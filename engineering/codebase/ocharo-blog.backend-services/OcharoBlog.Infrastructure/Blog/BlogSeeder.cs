using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>
/// Seeds an empty blog from the approved standalone journal (topics, thirty stories, images). Stories arrive as
/// published — matching the existing preview — while the site-wide noindex stays on until the owner launches.
/// </summary>
public sealed class BlogSeeder(IBlogRepository repository, IMediaStore media, SiteSettingsService settings, IOptions<BlogOptions> options,
    IClock clock, ILogger<BlogSeeder> logger)
{
    public async Task<int> SeedIfEmptyAsync(string contentRoot, CancellationToken ct)
    {
        if ((await repository.GetTopicsAsync(ct)).Count > 0 || (await repository.GetArticlesAsync(ct)).Count > 0) return 0;
        var file = options.Value.SeedFile is { } seedFile ? Path.IsPathRooted(seedFile) ? seedFile : Path.Combine(contentRoot, seedFile) : null;
        if (file is null || !File.Exists(file)) return 0;
        var seed = JsonSerializer.Deserialize<JournalSeed>(await File.ReadAllBytesAsync(file, ct), SeedJson) ?? throw new InvalidOperationException("Empty seed.");

        for (var i = 0; i < seed.Topics.Count; i++)
        {
            var t = seed.Topics[i];
            var topic = new TopicDocument(t.Key, t.Name, t.Short, t.Description, i);
            await repository.SaveTopicAsync(new TopicRecord { Key = t.Key, SortOrder = i, Json = BlogJson.Write(topic) }, ct);
        }

        var images = new Dictionary<string, string>();
        async Task<string?> ImageAsync(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || options.Value.SeedAssetRoot is not { } root) return null;
            if (images.TryGetValue(name, out var known)) return known;
            var path = Path.Combine(Path.GetFullPath(root, contentRoot), Path.GetFileName(name));
            if (!File.Exists(path)) { logger.LogWarning("Seed image {Name} not found in {Root}", name, root); return null; }
            var stored = await media.StoreAsync(await File.ReadAllBytesAsync(path, ct), name, ct);
            return images[name] = stored.Sha256;
        }

        var now = clock.UtcNow;
        for (var i = 0; i < seed.Stories.Count; i++)
        {
            var s = seed.Stories[i];
            var coverSha = await ImageAsync(s.Image);
            var document = new ArticleDocument
            {
                Slug = s.Slug,
                Topic = s.Topic,
                Status = ArticleStatus.Published,
                Title = s.Title,
                Excerpt = s.Excerpt,
                Intro = s.Intro,
                Format = s.Format,
                ReaderPromise = s.ReaderPromise,
                Sections = s.Sections.Select(x => new ArticleSection(x.Heading, x.Paragraphs, x.List, x.SourceIndexes)).ToList(),
                Takeaway = s.Takeaway,
                ClosingQuestion = s.ClosingQuestion,
                Sources = (s.Sources ?? []).Select(x => new ArticleSource(x.Label, x.Url, x.Supports)).ToList(),
                RelatedSlugs = s.RelatedSlugs ?? [],
                CardPrompt = s.CardPrompt,
                Cover = coverSha is null ? null : new ArticleCover(coverSha, s.Alt ?? "", s.Caption ?? "", s.CardClass),
                // Preserve the journal's reading order: the first story is the newest.
                PublishedAt = now.AddMinutes(-i),
            };
            var record = ArticleService.ToRecord(Guid.NewGuid(), document, 1, now, now);
            await repository.InsertArticleAsync(record, "Imported from the standalone journal", ct);
        }

        var current = await settings.GetAsync(ct);
        await settings.SaveAsync(current.Settings with
        {
            LogoSha256 = await ImageAsync(seed.Assets?.Logo),
            HeroSha256 = await ImageAsync(seed.Assets?.Hero),
            BandSha256 = await ImageAsync(seed.Assets?.Band),
            FeaturedSlug = seed.Stories.FirstOrDefault()?.Slug,
        }, current.Revision, ct);
        logger.LogInformation("Seeded blog: {Topics} topics, {Stories} stories, {Images} images", seed.Topics.Count, seed.Stories.Count, images.Count);
        return seed.Stories.Count;
    }

    private static readonly JsonSerializerOptions SeedJson = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };

    private sealed record JournalSeed(IReadOnlyList<SeedTopic> Topics, IReadOnlyList<SeedStory> Stories, SeedAssets? Assets);
    private sealed record SeedTopic(string Key, string Name, string Short, string Description);
    private sealed record SeedAssets(string? Logo, string? Hero, string? Band);
    private sealed record SeedSource(string Label, string Url, string? Supports);
    private sealed record SeedSection(string Heading, IReadOnlyList<string> Paragraphs, IReadOnlyList<string>? List, IReadOnlyList<int>? SourceIndexes);
    private sealed record SeedStory(string Slug, string Topic, string Title, string? Excerpt, string? Intro, string? Format, string? ReaderPromise,
        IReadOnlyList<SeedSection> Sections, string? Takeaway, string? ClosingQuestion, IReadOnlyList<SeedSource>? Sources, IReadOnlyList<string>? RelatedSlugs,
        string? CardPrompt, string? Image, string? Alt, string? Caption, string? CardClass);
}
