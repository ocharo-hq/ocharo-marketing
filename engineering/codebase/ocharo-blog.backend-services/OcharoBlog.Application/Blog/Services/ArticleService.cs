using System.Text.RegularExpressions;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Story commands and queries: validation, revision-checked saves, publishing lifecycle and history.</summary>
public sealed partial class ArticleService(IBlogRepository repository, IClock clock)
{
    public async Task<IReadOnlyList<ArticleSummary>> ListAsync(string? status, string? topic, string? query, CancellationToken ct, string? language = null, string? publication = null)
    {
        var q = query?.Trim();
        return (await repository.GetArticlesAsync(ct))
            .Where(r => string.IsNullOrWhiteSpace(status) || r.Status == status)
            .Where(r => string.IsNullOrWhiteSpace(topic) || r.Topic == topic)
            .Select(r => (Record: r, Document: BlogJson.Read<ArticleDocument>(r.Json)))
            .Where(x => string.IsNullOrWhiteSpace(language) || x.Document.Language == language)
            .Where(x => string.IsNullOrWhiteSpace(publication) || x.Document.Publication == publication)
            .Where(x => string.IsNullOrEmpty(q) || x.Document.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Document.Slug.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Document.Excerpt?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
            .OrderByDescending(x => x.Record.PublishedAt ?? x.Record.PublishAt ?? x.Record.UpdatedAt)
            .Select(x =>
            {
                var words = Words(x.Document);
                return new ArticleSummary(x.Record.Id, x.Document.Slug, x.Document.Topic, x.Document.Status, x.Document.Title, x.Document.Excerpt,
                    ReadMinutes(words), words, x.Document.Cover is not null, x.Document.ProductIds.Count, x.Document.PublishAt,
                    x.Document.PublishedAt, x.Record.Revision, x.Record.UpdatedAt, x.Document.Language, x.Document.TranslationKey, x.Document.Publication, x.Document.Kind);
            })
            .ToList();
    }

    public async Task<ArticleDetail> GetAsync(Guid id, CancellationToken ct) =>
        Detail(await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story"));

    public async Task<ArticleDetail> CreateAsync(ArticleInput input, CancellationToken ct)
    {
        var document = await NormalizeAsync(input, null, ArticleStatus.Draft, null, null, ct);
        var now = clock.UtcNow;
        var record = ToRecord(Guid.NewGuid(), document, 1, now, now);
        await repository.InsertArticleAsync(record, Summary(input.ChangeSummary, "Created"), ct);
        return Detail(record);
    }

    public async Task<ArticleDetail> UpdateAsync(Guid id, ArticleInput input, CancellationToken ct)
    {
        if (input.ExpectedRevision is not { } expected) throw new BlogException("Send the revision you edited (expectedRevision).");
        var record = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story");
        var current = BlogJson.Read<ArticleDocument>(record.Json);
        var document = await NormalizeAsync(input, id, current.Status, current.PublishAt, current.PublishedAt, ct);
        if (document.Status is ArticleStatus.Published or ArticleStatus.Scheduled) EnsurePublishable(document);
        var saved = await repository.UpdateArticleAsync(ToRecord(id, document, expected + 1, record.CreatedAt, clock.UtcNow), expected,
            Summary(input.ChangeSummary, current.Status == ArticleStatus.Published ? "Edited live story" : "Edited"), ct);
        return Detail(saved);
    }

    public Task<ArticleDetail> PublishAsync(Guid id, int expectedRevision, CancellationToken ct) =>
        TransitionAsync(id, expectedRevision, d =>
        {
            EnsurePublishable(d);
            var now = clock.UtcNow;
            return (d with { Status = ArticleStatus.Published, PublishAt = null, PublishedAt = d.PublishedAt ?? now }, "Published");
        }, ct);

    public Task<ArticleDetail> ScheduleAsync(Guid id, int expectedRevision, DateTimeOffset publishAt, CancellationToken ct) =>
        TransitionAsync(id, expectedRevision, d =>
        {
            if (publishAt <= clock.UtcNow.AddMinutes(1)) throw new BlogException("Choose a publication time at least a minute from now.");
            EnsurePublishable(d);
            return (d with { Status = ArticleStatus.Scheduled, PublishAt = publishAt.ToUniversalTime() }, $"Scheduled for {publishAt.ToUniversalTime():yyyy-MM-dd HH:mm} UTC");
        }, ct);

    public Task<ArticleDetail> UnpublishAsync(Guid id, int expectedRevision, CancellationToken ct) =>
        TransitionAsync(id, expectedRevision, d => (d with { Status = ArticleStatus.Draft, PublishAt = null }, "Returned to draft"), ct);

    public Task<ArticleDetail> ArchiveAsync(Guid id, int expectedRevision, CancellationToken ct) =>
        TransitionAsync(id, expectedRevision, d => (d with { Status = ArticleStatus.Archived, PublishAt = null }, "Archived"), ct);

    /// <summary>Publishes every scheduled story whose time has come; returns the slugs published.</summary>
    public async Task<IReadOnlyList<string>> PublishDueAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var published = new List<string>();
        foreach (var record in (await repository.GetArticlesAsync(ct)).Where(r => r.Status == ArticleStatus.Scheduled && r.PublishAt <= now))
        {
            var document = BlogJson.Read<ArticleDocument>(record.Json);
            EnsurePublishable(document);
            var next = document with { Status = ArticleStatus.Published, PublishAt = null, PublishedAt = document.PublishedAt ?? document.PublishAt ?? now };
            try
            {
                await repository.UpdateArticleAsync(ToRecord(record.Id, next, record.Revision + 1, record.CreatedAt, now), record.Revision,
                    "Published on schedule", ct);
                published.Add(document.Slug);
            }
            catch (BlogException exception) when (exception.StatusCode == 409)
            {
                // Edited concurrently; the next tick re-reads the current revision.
            }
        }

        return published;
    }

    public async Task<IReadOnlyList<RevisionSummary>> RevisionsAsync(Guid id, CancellationToken ct)
    {
        _ = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story");
        return (await repository.GetRevisionsAsync(id, ct)).OrderByDescending(r => r.Revision)
            .Select(r => new RevisionSummary(r.Revision, r.SavedAt, r.Summary)).ToList();
    }

    public async Task<ArticleDetail> RestoreAsync(Guid id, int revision, int expectedRevision, CancellationToken ct)
    {
        var record = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story");
        var snapshot = await repository.FindRevisionAsync(id, revision, ct) ?? throw BlogException.NotFound("Revision");
        var current = BlogJson.Read<ArticleDocument>(record.Json);
        var restored = BlogJson.Read<ArticleDocument>(snapshot.Json) with
        {
            // Restoring content never silently changes what is live.
            Status = current.Status,
            PublishAt = current.PublishAt,
            PublishedAt = current.PublishedAt,
        };
        if (await SlugTakenAsync(restored.Slug, id, ct)) throw BlogException.Conflict($"Another story now uses '{restored.Slug}'.");
        if (restored.Status is ArticleStatus.Published or ArticleStatus.Scheduled) EnsurePublishable(restored);
        var saved = await repository.UpdateArticleAsync(ToRecord(id, restored, expectedRevision + 1, record.CreatedAt, clock.UtcNow),
            expectedRevision, $"Restored revision {revision}", ct);
        return Detail(saved);
    }

    /// <summary>
    /// Creates a draft edition in another language: same structure, sources, cover and pieces, linked by translation
    /// key; the text is copied for translating in place. Related stories are cleared (they belong to the source language).
    /// </summary>
    public async Task<ArticleDetail> TranslateAsync(Guid id, string language, CancellationToken ct)
    {
        if (language is not ("en" or "uz" or "ru")) throw new BlogException("Language must be en, uz or ru.");
        var record = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story");
        var source = BlogJson.Read<ArticleDocument>(record.Json);
        if (source.Language == language) throw new BlogException("The story is already in that language.");
        var key = source.TranslationKey ?? source.Slug;
        var siblings = (await repository.GetArticlesAsync(ct)).Select(r => BlogJson.Read<ArticleDocument>(r.Json)).ToList();
        if (siblings.Any(s => (s.TranslationKey ?? s.Slug) == key && s.Language == language && s.Status != ArticleStatus.Archived))
            throw BlogException.Conflict("An edition in that language already exists.");
        var slug = $"{source.Slug}-{language}";
        for (var n = 2; siblings.Any(s => s.Slug == slug); n++) slug = $"{source.Slug}-{language}-{n}";
        var now = clock.UtcNow;
        if (source.TranslationKey is null)
            await repository.UpdateArticleAsync(ToRecord(id, source with { TranslationKey = key }, record.Revision + 1, record.CreatedAt, now), record.Revision,
                $"Linked translation group '{key}'", ct);
        var edition = source with
        {
            Slug = slug,
            Language = language,
            Status = ArticleStatus.Draft,
            PublishAt = null,
            PublishedAt = null,
            RelatedSlugs = [],
            TranslationKey = key,
        };
        var created = ToRecord(Guid.NewGuid(), edition, 1, now, now);
        await repository.InsertArticleAsync(created, $"Translation draft ({source.Language} → {language})", ct);
        return Detail(created);
    }

    /// <summary>Why a story cannot go live, as one message; throws when not publishable.</summary>
    public static void EnsurePublishable(ArticleDocument d)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(d.Excerpt)) missing.Add("excerpt");
        if (string.IsNullOrWhiteSpace(d.Intro)) missing.Add("introduction");
        if (d.Sections.Count == 0 || d.Sections.Any(s => string.IsNullOrWhiteSpace(ArticleFormatting.Text(s)))) missing.Add("at least one section with a paragraph");
        if (d.Cover is { } cover && (string.IsNullOrWhiteSpace(cover.Alt) || string.IsNullOrWhiteSpace(cover.Caption))) missing.Add("cover description and caption");
        if (d.Publication == "atlas")
        {
            if (d.Sources.Count == 0) missing.Add("a supporting source");
            if (d.Kind is "textile" or "archive")
            {
                if (d.Cover is null) missing.Add("an identified object image");
                if (d.Textile is null || string.IsNullOrWhiteSpace(d.Textile.ObjectId) || string.IsNullOrWhiteSpace(d.Textile.Collection)
                    || string.IsNullOrWhiteSpace(d.Textile.Place) || string.IsNullOrWhiteSpace(d.Textile.Period)
                    || string.IsNullOrWhiteSpace(d.Textile.Materials) || string.IsNullOrWhiteSpace(d.Textile.Technique))
                    missing.Add("object identity, collection, place, period, materials and technique (mark unknown explicitly)");
            }
            if (d.Cover is not null && d.ImageProvenance is null) missing.Add("image credit, rights and source");
        }
        if (d.Publication == "wear" && d.Occasions.Count == 0) missing.Add("an occasion");
        if (missing.Count > 0) throw new BlogException($"Before publishing, add: {string.Join(", ", missing)}.");
    }

    public static int Words(ArticleDocument d) =>
        new[] { d.Intro ?? "", d.Takeaway ?? "", d.ClosingQuestion ?? "" }
            .Concat(d.Sections.SelectMany(s => new[] { ArticleFormatting.Text(s) }.Concat(s.List ?? [])))
            .Sum(text => text.Split((char[])[' ', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length);

    public static int ReadMinutes(int words) => Math.Max(1, (int)Math.Ceiling(words / 200.0));

    private async Task<ArticleDetail> TransitionAsync(Guid id, int expectedRevision, Func<ArticleDocument, (ArticleDocument, string)> change, CancellationToken ct)
    {
        var record = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story");
        if (record.Revision != expectedRevision) throw BlogException.Conflict($"The story changed (now revision {record.Revision}). Reload first.");
        var (next, summary) = change(BlogJson.Read<ArticleDocument>(record.Json));
        var saved = await repository.UpdateArticleAsync(ToRecord(id, next, expectedRevision + 1, record.CreatedAt, clock.UtcNow), expectedRevision, summary, ct);
        return Detail(saved);
    }

    private async Task<ArticleDocument> NormalizeAsync(ArticleInput input, Guid? id, string status, DateTimeOffset? publishAt,
        DateTimeOffset? publishedAt, CancellationToken ct)
    {
        if (!Publications.All.Contains(input.Publication)) throw new BlogException("Publication must be blog, atlas or wear.");
        if (!Publications.Kinds.Contains(input.Kind)) throw new BlogException("Unknown editorial content kind.");
        if (input.Kind is "textile" or "archive" && input.Publication != "atlas") throw new BlogException("Object records belong to Atlas.");
        if (input.Publication == "wear" && input.Kind != "guide") throw new BlogException("Wear content must be a guide.");
        var slug = input.Slug.Trim();
        if (!Slug().IsMatch(slug) || slug.Length > 90) throw new BlogException("The address must be lowercase words joined by single hyphens (max 90).");
        if (await SlugTakenAsync(slug, id, ct)) throw BlogException.Conflict($"Another story already uses the address '{slug}'.");
        var topics = (await repository.GetTopicsAsync(ct)).Select(t => t.Key).ToHashSet();
        if (!topics.Contains(input.Topic)) throw new BlogException($"Unknown topic '{input.Topic}'.");
        if (input.Language is not ("en" or "uz" or "ru")) throw new BlogException("Language must be en, uz or ru.");

        var sources = (input.Sources ?? []).Select(s =>
        {
            var label = Text(s.Label, 300) ?? throw new BlogException("Every source needs a label.");
            if (!Uri.TryCreate(s.Url?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new BlogException($"Source '{label}' needs an https:// address.");
            return new ArticleSource(label, uri.ToString(), Text(s.Supports, 600));
        }).ToList();
        var sections = (input.Sections ?? []).Select(s =>
        {
            var heading = Text(s.Heading, 200) ?? throw new BlogException("Every section needs a heading.");
            var paragraphs = s.Format == "markdown"
                ? (s.Paragraphs ?? []).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Replace("\r\n", "\n")).ToList()
                : (s.Paragraphs ?? []).Select(p => Text(p, 4000)).OfType<string>().ToList();
            ArticleFormatting.Validate(s.Format, paragraphs);
            var list = (s.List ?? []).Select(p => Text(p, 600)).OfType<string>().ToList();
            var indexes = (s.SourceIndexes ?? []).Distinct().ToList();
            if (indexes.Any(i => i < 0 || i >= sources.Count)) throw new BlogException($"Section '{heading}' cites a missing source.");
            return new ArticleSection(heading, paragraphs, list.Count == 0 ? null : list, indexes.Count == 0 ? null : indexes, s.Format);
        }).ToList();

        var related = (input.RelatedSlugs ?? []).Select(r => r.Trim()).Where(r => r.Length > 0 && r != slug).Distinct().Take(3).ToList();
        foreach (var r in related)
            if (await repository.FindArticleBySlugAsync(r, ct) is null) throw new BlogException($"Related story '{r}' does not exist.");

        ArticleCover? cover = null;
        if (input.Cover is { } c)
        {
            if (await repository.FindMediaAsync(c.MediaSha256, ct) is null) throw new BlogException("Upload the cover image before using it.");
            cover = new ArticleCover(c.MediaSha256, Text(c.Alt, 400) ?? "", Text(c.Caption, 400) ?? "", Text(c.CardClass, 40));
        }

        ImageProvenance? provenance = null;
        if (input.ImageProvenance is { } evidence)
        {
            if (evidence.Kind is not ("archival" or "photograph" or "creative-visualization")) throw new BlogException("Choose image provenance: archival, photograph or creative-visualization.");
            if (!Uri.TryCreate(evidence.SourceUrl, UriKind.Absolute, out var sourceUri) || sourceUri.Scheme != "https") throw new BlogException("Image provenance needs an https source URL.");
            provenance = evidence with
            {
                Credit = Text(evidence.Credit, 800) ?? throw new BlogException("Add the image credit."),
                Rights = Text(evidence.Rights, 800) ?? throw new BlogException("Add the actual image rights statement."),
                SourceUrl = sourceUri.ToString(),
            };
        }
        TextileDetails? textile = input.Textile is { } t ? new TextileDetails(
            Text(t.ObjectId, 120) ?? "", Text(t.Collection, 300) ?? "", Text(t.Place, 160) ?? "",
            Text(t.Period, 160) ?? "", Text(t.Materials, 300) ?? "", Text(t.Technique, 300) ?? "",
            Text(t.Dimensions, 160), Text(t.Maker, 200), Text(t.AttributionNote, 800)) : null;
        var textiles = (input.TextileSlugs ?? []).Select(x => x.Trim()).Where(x => x.Length > 0 && x != slug).Distinct().Take(12).ToList();
        foreach (var linked in textiles)
        {
            var record = await repository.FindArticleBySlugAsync(linked, ct);
            if (record is null) throw new BlogException($"Linked textile '{linked}' does not exist.");
            var doc = BlogJson.Read<ArticleDocument>(record.Json);
            if (doc.Publication != "atlas" || doc.Kind is not ("textile" or "archive")) throw new BlogException($"'{linked}' is not an Atlas object.");
        }

        return new ArticleDocument
        {
            Publication = input.Publication,
            Kind = input.Kind,
            Places = Tags(input.Places), Techniques = Tags(input.Techniques), Motifs = Tags(input.Motifs), Occasions = Tags(input.Occasions),
            TextileSlugs = textiles, Textile = textile, ImageProvenance = provenance,
            Slug = slug,
            Topic = input.Topic,
            Status = status,
            Language = input.Language,
            Title = Text(input.Title, 200) ?? throw new BlogException("A story needs a title."),
            Excerpt = Text(input.Excerpt, 400),
            Intro = Text(input.Intro, 4000),
            Format = Text(input.Format, 80),
            ReaderPromise = Text(input.ReaderPromise, 400),
            Sections = sections,
            Takeaway = Text(input.Takeaway, 600),
            ClosingQuestion = Text(input.ClosingQuestion, 400),
            Sources = sources,
            RelatedSlugs = related,
            CardPrompt = Text(input.CardPrompt, 120),
            Cover = cover,
            ProductIds = (input.ProductIds ?? []).Select(p => p.Trim()).Where(p => p.Length is > 0 and <= 64).Distinct().Take(6).ToList(),
            PublishAt = publishAt,
            PublishedAt = publishedAt,
            SeoTitle = Text(input.SeoTitle, 120),
            SeoDescription = Text(input.SeoDescription, 300),
            TranslationKey = Text(input.TranslationKey, 90),
        };
    }

    private async Task<bool> SlugTakenAsync(string slug, Guid? id, CancellationToken ct) =>
        await repository.FindArticleBySlugAsync(slug, ct) is { } existing && existing.Id != id;

    public static ArticleRecord ToRecord(Guid id, ArticleDocument d, int revision, DateTimeOffset created, DateTimeOffset updated) => new()
    {
        Id = id,
        Slug = d.Slug,
        Topic = d.Topic,
        Status = d.Status,
        PublishAt = d.PublishAt,
        PublishedAt = d.PublishedAt,
        Revision = revision,
        CreatedAt = created,
        UpdatedAt = updated,
        Json = BlogJson.Write(d),
    };

    private static ArticleDetail Detail(ArticleRecord r)
    {
        var document = BlogJson.Read<ArticleDocument>(r.Json);
        var words = Words(document);
        return new ArticleDetail(r.Id, document, r.Revision, r.CreatedAt, r.UpdatedAt, ReadMinutes(words), words);
    }

    private static string? Text(string? value, int max)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > max) throw new BlogException($"Text is longer than {max} characters.");
        return trimmed;
    }

    private static IReadOnlyList<string> Tags(IReadOnlyList<string>? values) =>
        (values ?? []).Select(x => Text(x, 80)).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();

    private static string Summary(string? requested, string fallback) =>
        string.IsNullOrWhiteSpace(requested) ? fallback : requested.Trim()[..Math.Min(200, requested.Trim().Length)];

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Slug();
}
