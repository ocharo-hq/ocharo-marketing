using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using DetailResult = WoW.Two.Sdk.Beta.Be.Foundation.Results.Result<OcharoBlog.Application.Blog.Models.ArticleDetail>;
using DocumentResult = WoW.Two.Sdk.Beta.Be.Foundation.Results.Result<OcharoBlog.Domain.Blog.ArticleDocument>;
using TagsResult = WoW.Two.Sdk.Beta.Be.Foundation.Results.Result<System.Collections.Generic.IReadOnlyList<string>>;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Story commands and queries: validation, revision-checked saves, publishing lifecycle and history. Each
/// refusal is the first check that fails, in the order the editor's input is read.</summary>
public sealed partial class ArticleService(IBlogRepository repository, TimeProvider time)
{
    public async Task<IReadOnlyList<ArticleSummary>> ListAsync(
        string? status,
        string? topic,
        string? query,
        CancellationToken ct,
        string? language = null,
        string? publication = null)
    {
        var q = query?.Trim();
        return (await repository.GetArticlesAsync(ct))
            .Where(r => string.IsNullOrWhiteSpace(status) || r.Status == status)
            .Where(r => string.IsNullOrWhiteSpace(topic) || r.Topic == topic)
            .Select(r => (Record: r, Document: BlogJson.Read<ArticleDocument>(r.Json)))
            .Where(x => string.IsNullOrWhiteSpace(language) || x.Document.Language == language)
            .Where(x => string.IsNullOrWhiteSpace(publication) || x.Document.Publication == publication)
            .Where(x => string.IsNullOrEmpty(q)
                || x.Document.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Document.Slug.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Document.Excerpt?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
            .OrderByDescending(x => x.Record.PublishedAt ?? x.Record.PublishAt ?? x.Record.UpdatedAt)
            .Select(x => Summarize(x.Record, x.Document))
            .ToList();
    }

    public async Task<DetailResult> GetAsync(Guid id, CancellationToken ct)
    {
        return await repository.FindArticleAsync(id, ct) is { } record
            ? DetailResult.Ok(Detail(record))
            : DetailResult.Fail(BlogErrors.NotFound("Story"));
    }

    public async Task<DetailResult> CreateAsync(ArticleInput input, CancellationToken ct)
    {
        var normalized = await NormalizeAsync(input, null, ArticleStatus.Draft, null, null, ct);
        if (normalized.IsFailure(out var error, out var document))
            return DetailResult.Fail(error);
        var now = time.GetUtcNow();
        var record = ToRecord(Guid.NewGuid(), document, 1, now, now);
        var inserted = await repository.InsertArticleAsync(record, Summary(input.ChangeSummary, "Created"), ct);
        return inserted.IsFailure(out error, out _) ? DetailResult.Fail(error) : DetailResult.Ok(Detail(record));
    }

    public async Task<DetailResult> UpdateAsync(Guid id, ArticleInput input, CancellationToken ct)
    {
        if (input.ExpectedRevision is not { } expected)
            return DetailResult.Fail(BlogErrors.Invalid("Send the revision you edited (expectedRevision)."));
        if (await repository.FindArticleAsync(id, ct) is not { } record)
            return DetailResult.Fail(BlogErrors.NotFound("Story"));
        var current = BlogJson.Read<ArticleDocument>(record.Json);
        var normalized = await NormalizeAsync(input, id, current.Status, current.PublishAt, current.PublishedAt, ct);
        if (normalized.IsFailure(out var error, out var document))
            return DetailResult.Fail(error);
        if (document.Status is ArticleStatus.Published or ArticleStatus.Scheduled
            && EnsurePublishable(document) is Result.Failure refused)
            return DetailResult.Fail(refused.Error);
        var summary = Summary(
            input.ChangeSummary,
            current.Status == ArticleStatus.Published ? "Edited live story" : "Edited");
        var saved = await repository.UpdateArticleAsync(
            ToRecord(id, document, expected + 1, record.CreatedAt, time.GetUtcNow()),
            expected,
            summary,
            ct);
        return saved.Match(DetailOk, DetailResult.Fail);
    }

    public Task<DetailResult> PublishAsync(Guid id, int expectedRevision, CancellationToken ct)
    {
        return TransitionAsync(id, expectedRevision, d =>
        {
            if (EnsurePublishable(d) is Result.Failure refused)
                return Change.Fail(refused.Error);
            var now = time.GetUtcNow();
            var next = d with
            {
                Status = ArticleStatus.Published,
                PublishAt = null,
                PublishedAt = d.PublishedAt ?? now
            };
            return Change.Ok((next, "Published"));
        }, ct);
    }

    public Task<DetailResult> ScheduleAsync(
        Guid id,
        int expectedRevision,
        DateTimeOffset publishAt,
        CancellationToken ct)
    {
        return TransitionAsync(id, expectedRevision, d =>
        {
            if (publishAt <= time.GetUtcNow().AddMinutes(1))
                return Change.Fail(BlogErrors.Invalid("Choose a publication time at least a minute from now."));
            if (EnsurePublishable(d) is Result.Failure refused)
                return Change.Fail(refused.Error);
            var at = publishAt.ToUniversalTime();
            var next = d with { Status = ArticleStatus.Scheduled, PublishAt = at };
            return Change.Ok((next, $"Scheduled for {at:yyyy-MM-dd HH:mm} UTC"));
        }, ct);
    }

    public Task<DetailResult> UnpublishAsync(Guid id, int expectedRevision, CancellationToken ct)
    {
        return TransitionAsync(
            id,
            expectedRevision,
            d => Change.Ok((d with { Status = ArticleStatus.Draft, PublishAt = null }, "Returned to draft")),
            ct);
    }

    public Task<DetailResult> ArchiveAsync(Guid id, int expectedRevision, CancellationToken ct)
    {
        return TransitionAsync(
            id,
            expectedRevision,
            d => Change.Ok((d with { Status = ArticleStatus.Archived, PublishAt = null }, "Archived")),
            ct);
    }

    /// <summary>Publishes every scheduled story whose time has come; returns the slugs published.</summary>
    /// <remarks>The periodic job reads failures as exceptions: a scheduled story that can no longer go live, or one
    /// deleted meanwhile, ends the pass as before; a story edited concurrently waits for the next pass, which re-reads
    /// the current revision.</remarks>
    public async Task<IReadOnlyList<string>> PublishDueAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var published = new List<string>();
        var due = (await repository.GetArticlesAsync(ct))
            .Where(r => r.Status == ArticleStatus.Scheduled && r.PublishAt <= now);
        foreach (var record in due)
        {
            var document = BlogJson.Read<ArticleDocument>(record.Json);
            EnsurePublishable(document).ThrowIfFailure();
            var next = document with
            {
                Status = ArticleStatus.Published,
                PublishAt = null,
                PublishedAt = document.PublishedAt ?? document.PublishAt ?? now,
            };
            var saved = await repository.UpdateArticleAsync(
                ToRecord(record.Id, next, record.Revision + 1, record.CreatedAt, now),
                record.Revision,
                "Published on schedule",
                ct);
            if (saved is Result<ArticleRecord>.Failure { Error.Type: AppErrorType.Conflict })
                continue;
            saved.ValueOrThrow();
            published.Add(document.Slug);
        }

        return published;
    }

    public async Task<Result<IReadOnlyList<RevisionSummary>>> RevisionsAsync(Guid id, CancellationToken ct)
    {
        if (await repository.FindArticleAsync(id, ct) is null)
            return Result<IReadOnlyList<RevisionSummary>>.Fail(BlogErrors.NotFound("Story"));
        var revisions = (await repository.GetRevisionsAsync(id, ct))
            .OrderByDescending(r => r.Revision)
            .Select(r => new RevisionSummary(r.Revision, r.SavedAt, r.Summary))
            .ToList();
        return Result<IReadOnlyList<RevisionSummary>>.Ok(revisions);
    }

    public async Task<DetailResult> RestoreAsync(
        Guid id,
        int revision,
        int expectedRevision,
        CancellationToken ct)
    {
        if (await repository.FindArticleAsync(id, ct) is not { } record)
            return DetailResult.Fail(BlogErrors.NotFound("Story"));
        if (await repository.FindRevisionAsync(id, revision, ct) is not { } snapshot)
            return DetailResult.Fail(BlogErrors.NotFound("Revision"));
        var current = BlogJson.Read<ArticleDocument>(record.Json);
        var restored = BlogJson.Read<ArticleDocument>(snapshot.Json) with
        {
            // Restoring content never silently changes what is live.
            Status = current.Status,
            PublishAt = current.PublishAt,
            PublishedAt = current.PublishedAt,
        };
        if (await SlugTakenAsync(restored.Slug, id, ct))
            return DetailResult.Fail(BlogErrors.Conflict($"Another story now uses '{restored.Slug}'."));
        if (restored.Status is ArticleStatus.Published or ArticleStatus.Scheduled
            && EnsurePublishable(restored) is Result.Failure refused)
            return DetailResult.Fail(refused.Error);
        var saved = await repository.UpdateArticleAsync(
            ToRecord(id, restored, expectedRevision + 1, record.CreatedAt, time.GetUtcNow()),
            expectedRevision,
            $"Restored revision {revision}",
            ct);
        return saved.Match(DetailOk, DetailResult.Fail);
    }

    /// <summary>
    /// Creates a draft edition in another language: same structure, sources, cover and pieces, linked by translation
    /// key; the text is copied for translating in place. Related stories are cleared (they belong to the source
    /// language). </summary>
    public async Task<DetailResult> TranslateAsync(Guid id, string language, CancellationToken ct)
    {
        if (language is not ("en" or "uz" or "ru"))
            return DetailResult.Fail(BlogErrors.Invalid("Language must be en, uz or ru."));
        if (await repository.FindArticleAsync(id, ct) is not { } record)
            return DetailResult.Fail(BlogErrors.NotFound("Story"));
        var source = BlogJson.Read<ArticleDocument>(record.Json);
        if (source.Language == language)
            return DetailResult.Fail(BlogErrors.Invalid("The story is already in that language."));
        var key = source.TranslationKey ?? source.Slug;
        var siblings = (await repository.GetArticlesAsync(ct))
            .Select(r => BlogJson.Read<ArticleDocument>(r.Json))
            .ToList();
        if (siblings.Any(s => (s.TranslationKey ?? s.Slug) == key
            && s.Language == language
            && s.Status != ArticleStatus.Archived))
            return DetailResult.Fail(BlogErrors.Conflict("An edition in that language already exists."));
        var slug = $"{source.Slug}-{language}";
        for (var n = 2; siblings.Any(s => s.Slug == slug); n++)
            slug = $"{source.Slug}-{language}-{n}";
        var now = time.GetUtcNow();
        if (source.TranslationKey is null)
        {
            var linked = await repository.UpdateArticleAsync(
                ToRecord(id, source with { TranslationKey = key }, record.Revision + 1, record.CreatedAt, now),
                record.Revision,
                $"Linked translation group '{key}'",
                ct);
            if (linked.IsFailure(out var error, out _))
                return DetailResult.Fail(error);
        }
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
        var inserted = await repository.InsertArticleAsync(
            created,
            $"Translation draft ({source.Language} → {language})",
            ct);
        return inserted.Match(_ => DetailResult.Ok(Detail(created)), DetailResult.Fail);
    }

    /// <summary>Tells why a story cannot go live, as one message; succeeds when it is publishable.</summary>
    public static Result EnsurePublishable(ArticleDocument d)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(d.Excerpt)) missing.Add("excerpt");
        if (string.IsNullOrWhiteSpace(d.Intro)) missing.Add("introduction");
        if (d.Sections.Count == 0 || d.Sections.Any(s => string.IsNullOrWhiteSpace(ArticleFormatting.Text(s))))
            missing.Add("at least one section with a paragraph");
        if (d.Cover is { } cover && (string.IsNullOrWhiteSpace(cover.Alt) || string.IsNullOrWhiteSpace(cover.Caption)))
            missing.Add("cover description and caption");
        if (d.Publication == "atlas")
        {
            if (d.Sources.Count == 0) missing.Add("a supporting source");
            if (d.Kind is "textile" or "archive")
            {
                if (d.Cover is null) missing.Add("an identified object image");
                if (!HasObjectIdentity(d.Textile))
                    missing.Add(
                        "object identity, collection, place, period, materials and technique (mark unknown "
                            + "explicitly)");
            }
            if (d.Cover is not null && d.ImageProvenance is null) missing.Add("image credit, rights and source");
        }
        if (d.Publication == "wear" && d.Occasions.Count == 0) missing.Add("an occasion");
        return missing.Count > 0
            ? Result.Fail(BlogErrors.Invalid($"Before publishing, add: {string.Join(", ", missing)}."))
            : Result.Ok();
    }

    public static int Words(ArticleDocument d)
    {
        return new[] { d.Intro ?? "", d.Takeaway ?? "", d.ClosingQuestion ?? "" }
            .Concat(d.Sections.SelectMany(s => new[] { ArticleFormatting.Text(s) }.Concat(s.List ?? [])))
            .Sum(text => text.Split((char[])[' ', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length);
    }

    public static int ReadMinutes(int words)
    {
        return Math.Max(1, (int)Math.Ceiling(words / 200.0));
    }

    private static bool HasObjectIdentity(TextileDetails? textile)
    {
        return textile is not null
            && !string.IsNullOrWhiteSpace(textile.ObjectId)
            && !string.IsNullOrWhiteSpace(textile.Collection)
            && !string.IsNullOrWhiteSpace(textile.Place)
            && !string.IsNullOrWhiteSpace(textile.Period)
            && !string.IsNullOrWhiteSpace(textile.Materials)
            && !string.IsNullOrWhiteSpace(textile.Technique);
    }

    private async Task<DetailResult> TransitionAsync(
        Guid id,
        int expectedRevision,
        Func<ArticleDocument, Result<(ArticleDocument Next, string Summary)>> change,
        CancellationToken ct)
    {
        if (await repository.FindArticleAsync(id, ct) is not { } record)
            return DetailResult.Fail(BlogErrors.NotFound("Story"));
        if (record.Revision != expectedRevision)
            return DetailResult.Fail(
                BlogErrors.Conflict($"The story changed (now revision {record.Revision}). Reload first."));
        if (change(BlogJson.Read<ArticleDocument>(record.Json)).IsFailure(out var error, out var changed))
            return DetailResult.Fail(error);
        var saved = await repository.UpdateArticleAsync(
            ToRecord(id, changed.Next, expectedRevision + 1, record.CreatedAt, time.GetUtcNow()),
            expectedRevision,
            changed.Summary,
            ct);
        return saved.Match(DetailOk, DetailResult.Fail);
    }

    private async Task<DocumentResult> NormalizeAsync(
        ArticleInput input,
        Guid? id,
        string status,
        DateTimeOffset? publishAt,
        DateTimeOffset? publishedAt,
        CancellationToken ct)
    {
        if (!Publications.All.Contains(input.Publication))
            return Invalid("Publication must be blog, atlas or wear.");
        if (!Publications.Kinds.Contains(input.Kind))
            return Invalid("Unknown editorial content kind.");
        if (input.Kind is "textile" or "archive" && input.Publication != "atlas")
            return Invalid("Object records belong to Atlas.");
        if (input.Publication == "wear" && input.Kind != "guide")
            return Invalid("Wear content must be a guide.");
        var slug = input.Slug.Trim();
        if (!Slug().IsMatch(slug) || slug.Length > 90)
            return Invalid("The address must be lowercase words joined by single hyphens (max 90).");
        if (await SlugTakenAsync(slug, id, ct))
            return DocumentResult.Fail(BlogErrors.Conflict($"Another story already uses the address '{slug}'."));
        var topics = (await repository.GetTopicsAsync(ct)).Select(t => t.Key).ToHashSet();
        if (!topics.Contains(input.Topic))
            return Invalid($"Unknown topic '{input.Topic}'.");
        if (input.Language is not ("en" or "uz" or "ru"))
            return Invalid("Language must be en, uz or ru.");

        var sources = new List<ArticleSource>();
        foreach (var s in input.Sources ?? [])
        {
            if (TooLong(s.Label, 300, out var label, out var error)) return DocumentResult.Fail(error);
            if (label is null) return Invalid("Every source needs a label.");
            if (!Uri.TryCreate(s.Url?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return Invalid($"Source '{label}' needs an https:// address.");
            if (TooLong(s.Supports, 600, out var supports, out error)) return DocumentResult.Fail(error);
            sources.Add(new ArticleSource(label, uri.ToString(), supports));
        }

        var sections = new List<ArticleSection>();
        foreach (var s in input.Sections ?? [])
        {
            var section = Section(s, sources.Count);
            if (section.IsFailure(out var error, out var normalized)) return DocumentResult.Fail(error);
            sections.Add(normalized);
        }

        var related = (input.RelatedSlugs ?? [])
            .Select(r => r.Trim())
            .Where(r => r.Length > 0 && r != slug)
            .Distinct()
            .Take(3)
            .ToList();
        foreach (var r in related)
        {
            if (await repository.FindArticleBySlugAsync(r, ct) is null)
                return Invalid($"Related story '{r}' does not exist.");
        }

        ArticleCover? cover = null;
        if (input.Cover is { } c)
        {
            if (await repository.FindMediaAsync(c.MediaSha256, ct) is null)
                return Invalid("Upload the cover image before using it.");
            if (TooLong(c.Alt, 400, out var alt, out var error)
                || TooLong(c.Caption, 400, out var caption, out error)
                || TooLong(c.CardClass, 40, out var cardClass, out error))
                return DocumentResult.Fail(error);
            cover = new ArticleCover(c.MediaSha256, alt ?? "", caption ?? "", cardClass);
        }

        ImageProvenance? provenance = null;
        if (input.ImageProvenance is { } evidence)
        {
            var provenanceResult = Provenance(evidence);
            if (provenanceResult.IsFailure(out var error, out var checkedProvenance))
                return DocumentResult.Fail(error);
            provenance = checkedProvenance;
        }

        TextileDetails? textile = null;
        if (input.Textile is { } t)
        {
            var textileResult = Textile(t);
            if (textileResult.IsFailure(out var error, out var checkedTextile)) return DocumentResult.Fail(error);
            textile = checkedTextile;
        }

        var textiles = (input.TextileSlugs ?? [])
            .Select(x => x.Trim())
            .Where(x => x.Length > 0 && x != slug)
            .Distinct()
            .Take(12)
            .ToList();
        foreach (var linked in textiles)
        {
            var record = await repository.FindArticleBySlugAsync(linked, ct);
            if (record is null) return Invalid($"Linked textile '{linked}' does not exist.");
            var doc = BlogJson.Read<ArticleDocument>(record.Json);
            if (doc.Publication != "atlas" || doc.Kind is not ("textile" or "archive"))
                return Invalid($"'{linked}' is not an Atlas object.");
        }

        return Document(input, slug, status, publishAt, publishedAt, sections, sources, related, cover, provenance,
            textile, textiles);
    }

    /// <summary>Reads the document's own text fields in the order they are stored, and assembles it.</summary>
    private static DocumentResult Document(
        ArticleInput input,
        string slug,
        string status,
        DateTimeOffset? publishAt,
        DateTimeOffset? publishedAt,
        IReadOnlyList<ArticleSection> sections,
        IReadOnlyList<ArticleSource> sources,
        IReadOnlyList<string> related,
        ArticleCover? cover,
        ImageProvenance? provenance,
        TextileDetails? textile,
        IReadOnlyList<string> textiles)
    {
        if (Tags(input.Places).IsFailure(out var error, out var places)
            || Tags(input.Techniques).IsFailure(out error, out var techniques)
            || Tags(input.Motifs).IsFailure(out error, out var motifs)
            || Tags(input.Occasions).IsFailure(out error, out var occasions))
            return DocumentResult.Fail(error);
        if (TooLong(input.Title, 200, out var title, out error)) return DocumentResult.Fail(error);
        if (title is null) return Invalid("A story needs a title.");
        if (TooLong(input.Excerpt, 400, out var excerpt, out error)
            || TooLong(input.Intro, 4000, out var intro, out error)
            || TooLong(input.Format, 80, out var format, out error)
            || TooLong(input.ReaderPromise, 400, out var readerPromise, out error)
            || TooLong(input.Takeaway, 600, out var takeaway, out error)
            || TooLong(input.ClosingQuestion, 400, out var closingQuestion, out error)
            || TooLong(input.CardPrompt, 120, out var cardPrompt, out error)
            || TooLong(input.SeoTitle, 120, out var seoTitle, out error)
            || TooLong(input.SeoDescription, 300, out var seoDescription, out error)
            || TooLong(input.TranslationKey, 90, out var translationKey, out error))
            return DocumentResult.Fail(error);

        return DocumentResult.Ok(new ArticleDocument
        {
            Publication = input.Publication,
            Kind = input.Kind,
            Places = places,
            Techniques = techniques,
            Motifs = motifs,
            Occasions = occasions,
            TextileSlugs = textiles,
            Textile = textile,
            ImageProvenance = provenance,
            Slug = slug,
            Topic = input.Topic,
            Status = status,
            Language = input.Language,
            Title = title,
            Excerpt = excerpt,
            Intro = intro,
            Format = format,
            ReaderPromise = readerPromise,
            Sections = sections,
            Takeaway = takeaway,
            ClosingQuestion = closingQuestion,
            Sources = sources,
            RelatedSlugs = related,
            CardPrompt = cardPrompt,
            Cover = cover,
            ProductIds = (input.ProductIds ?? [])
                .Select(p => p.Trim())
                .Where(p => p.Length is > 0 and <= 64)
                .Distinct()
                .Take(6)
                .ToList(),
            PublishAt = publishAt,
            PublishedAt = publishedAt,
            SeoTitle = seoTitle,
            SeoDescription = seoDescription,
            TranslationKey = translationKey,
        });
    }

    /// <summary>Checks one section: its heading, paragraphs in their format, list items and cited sources.</summary>
    private static Result<ArticleSection> Section(ArticleSection s, int sourceCount)
    {
        if (TooLong(s.Heading, 200, out var heading, out var error)) return Result<ArticleSection>.Fail(error);
        if (heading is null) return Result<ArticleSection>.Fail(BlogErrors.Invalid("Every section needs a heading."));
        var paragraphs = new List<string>();
        foreach (var p in s.Paragraphs ?? [])
        {
            if (s.Format == "markdown")
            {
                if (!string.IsNullOrWhiteSpace(p)) paragraphs.Add(p.Replace("\r\n", "\n"));
                continue;
            }
            if (TooLong(p, 4000, out var paragraph, out error)) return Result<ArticleSection>.Fail(error);
            if (paragraph is not null) paragraphs.Add(paragraph);
        }
        if (ArticleFormatting.Validate(s.Format, paragraphs) is Result.Failure refused)
            return Result<ArticleSection>.Fail(refused.Error);
        var list = new List<string>();
        foreach (var item in s.List ?? [])
        {
            if (TooLong(item, 600, out var text, out error)) return Result<ArticleSection>.Fail(error);
            if (text is not null) list.Add(text);
        }
        var indexes = (s.SourceIndexes ?? []).Distinct().ToList();
        if (indexes.Any(i => i < 0 || i >= sourceCount))
            return Result<ArticleSection>.Fail(BlogErrors.Invalid($"Section '{heading}' cites a missing source."));
        return Result<ArticleSection>.Ok(new ArticleSection(
            heading,
            paragraphs,
            list.Count == 0 ? null : list,
            indexes.Count == 0 ? null : indexes,
            s.Format));
    }

    /// <summary>Checks an image's provenance: its kind, an HTTPS source, its credit and its rights statement.</summary>
    private static Result<ImageProvenance> Provenance(ImageProvenance evidence)
    {
        if (evidence.Kind is not ("archival" or "photograph" or "creative-visualization"))
            return Result<ImageProvenance>.Fail(
                BlogErrors.Invalid("Choose image provenance: archival, photograph or creative-visualization."));
        if (!Uri.TryCreate(evidence.SourceUrl, UriKind.Absolute, out var sourceUri) || sourceUri.Scheme != "https")
            return Result<ImageProvenance>.Fail(BlogErrors.Invalid("Image provenance needs an https source URL."));
        if (TooLong(evidence.Credit, 800, out var credit, out var error)) return Result<ImageProvenance>.Fail(error);
        if (credit is null) return Result<ImageProvenance>.Fail(BlogErrors.Invalid("Add the image credit."));
        if (TooLong(evidence.Rights, 800, out var rights, out error)) return Result<ImageProvenance>.Fail(error);
        if (rights is null)
            return Result<ImageProvenance>.Fail(BlogErrors.Invalid("Add the actual image rights statement."));
        return Result<ImageProvenance>.Ok(evidence with
        {
            Credit = credit,
            Rights = rights,
            SourceUrl = sourceUri.ToString(),
        });
    }

    /// <summary>Checks an object record's fields against their lengths, in the order they are stored.</summary>
    private static Result<TextileDetails> Textile(TextileDetails t)
    {
        if (TooLong(t.ObjectId, 120, out var objectId, out var error)
            || TooLong(t.Collection, 300, out var collection, out error)
            || TooLong(t.Place, 160, out var place, out error)
            || TooLong(t.Period, 160, out var period, out error)
            || TooLong(t.Materials, 300, out var materials, out error)
            || TooLong(t.Technique, 300, out var technique, out error)
            || TooLong(t.Dimensions, 160, out var dimensions, out error)
            || TooLong(t.Maker, 200, out var maker, out error)
            || TooLong(t.AttributionNote, 800, out var attributionNote, out error))
            return Result<TextileDetails>.Fail(error);
        return Result<TextileDetails>.Ok(new TextileDetails(
            objectId ?? "",
            collection ?? "",
            place ?? "",
            period ?? "",
            materials ?? "",
            technique ?? "",
            dimensions,
            maker,
            attributionNote));
    }

    private async Task<bool> SlugTakenAsync(string slug, Guid? id, CancellationToken ct)
    {
        return await repository.FindArticleBySlugAsync(slug, ct) is { } existing && existing.Id != id;
    }

    public static ArticleRecord ToRecord(
        Guid id,
        ArticleDocument d,
        int revision,
        DateTimeOffset created,
        DateTimeOffset updated)
    {
        return new()
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
    }

    private static ArticleSummary Summarize(ArticleRecord record, ArticleDocument document)
    {
        var words = Words(document);
        return new ArticleSummary(
            record.Id,
            document.Slug,
            document.Topic,
            document.Status,
            document.Title,
            document.Excerpt,
            ReadMinutes(words),
            words,
            document.Cover is not null,
            document.ProductIds.Count,
            document.PublishAt,
            document.PublishedAt,
            record.Revision,
            record.UpdatedAt,
            document.Language,
            document.TranslationKey,
            document.Publication,
            document.Kind);
    }

    private static ArticleDetail Detail(ArticleRecord r)
    {
        var document = BlogJson.Read<ArticleDocument>(r.Json);
        var words = Words(document);
        return new ArticleDetail(r.Id, document, r.Revision, r.CreatedAt, r.UpdatedAt, ReadMinutes(words), words);
    }

    private static DetailResult DetailOk(ArticleRecord record)
    {
        return DetailResult.Ok(Detail(record));
    }

    private static DocumentResult Invalid(string message)
    {
        return DocumentResult.Fail(BlogErrors.Invalid(message));
    }

    /// <summary>Trims <paramref name="value"/> into <paramref name="text"/>, none when blank; tells whether it is
    /// longer than <paramref name="max"/>, with the refusal in <paramref name="error"/>.</summary>
    private static bool TooLong(string? value, int max, out string? text, [NotNullWhen(true)] out AppError? error)
    {
        var trimmed = value?.Trim();
        text = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        error = text?.Length > max ? BlogErrors.Invalid($"Text is longer than {max} characters.") : null;
        return error is not null;
    }

    /// <summary>Reads up to 12 distinct tags, ignoring case; a tag past the twelfth is never read.</summary>
    private static TagsResult Tags(IReadOnlyList<string>? values)
    {
        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values ?? [])
        {
            if (tags.Count == 12) break;
            if (TooLong(value, 80, out var tag, out var error)) return TagsResult.Fail(error);
            if (tag is not null && seen.Add(tag)) tags.Add(tag);
        }
        return TagsResult.Ok(tags);
    }

    private static string Summary(string? requested, string fallback)
    {
        return string.IsNullOrWhiteSpace(requested)
            ? fallback
            : requested.Trim()[..Math.Min(200, requested.Trim().Length)];
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Slug();

    /// <summary>Builds the outcome of a lifecycle change: the next document and its history summary.</summary>
    private static class Change
    {
        public static Result<(ArticleDocument Next, string Summary)> Ok((ArticleDocument Next, string Summary) change)
        {
            return Result<(ArticleDocument Next, string Summary)>.Ok(change);
        }

        public static Result<(ArticleDocument Next, string Summary)> Fail(AppError error)
        {
            return Result<(ArticleDocument Next, string Summary)>.Fail(error);
        }
    }
}
