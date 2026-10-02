using System.IO.Compression;
using System.Text;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Site;

/// <summary>A rendered public resource.</summary>
public sealed record SitePage(string Content, string ContentType);

/// <summary>Serves the public site from live data, renders previews, and exports a static copy.</summary>
public sealed class SiteService(IBlogRepository repository, TopicService topics, SiteSettingsService settings, ICatalogueFeed catalogue,
    IBrandKit brand, IMediaStore media, IClock clock)
{
    private static readonly Lazy<string> Theme = new(() =>
    {
        using var stream = typeof(SiteService).Assembly.GetManifestResourceStream("OcharoBlog.Site.css")
            ?? throw new InvalidOperationException("Embedded site theme is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    /// <summary>The public stylesheet (approved journal theme plus platform additions).</summary>
    public static string Css => Theme.Value;

    /// <summary>Everything a render needs; <paramref name="live"/> false (static export) leaves out time-bounded event badges.</summary>
    public async Task<SiteModel> ModelAsync(Func<string, string> mediaUrl, CancellationToken ct, string language = "en", bool live = true)
    {
        var editions = (await repository.GetArticlesAsync(ct))
            .Where(r => r.Status == ArticleStatus.Published)
            .OrderByDescending(r => r.PublishedAt).ThenBy(r => r.Slug, StringComparer.Ordinal)
            .Select(r => BlogJson.Read<ArticleDocument>(r.Json))
            .Where(d => d.Publication == "blog")
            .ToList();
        var stories = editions.Where(s => s.Language == language).ToList();
        var available = SiteLanguages.All.Where(l => l == "en" || editions.Any(s => s.Language == l)).ToList();
        var products = stories.Any(s => s.ProductIds.Count > 0)
            ? (await catalogue.GetProductsAsync(ct)).ToDictionary(p => p.Id)
            : new Dictionary<string, CatalogueProduct>();
        var kit = await brand.GetAsync(ct);
        var badge = live && kit?.Event is { } ev && ev.EndsAt > clock.UtcNow ? ev : null;
        return new SiteModel(await BrandedAsync((await settings.GetAsync(ct)).Settings, kit, ct), await topics.ListAsync(ct), stories, products,
            mediaUrl, clock.UtcNow, language, editions, available, badge);
    }

    /// <summary>
    /// The published brand kit wins over the blog's own logo and links. Its logo is copied into the blog's media store
    /// (same SHA-256), so live pages and the static export share one path; an unreachable Brand service keeps the settings.
    /// </summary>
    private async Task<SiteSettings> BrandedAsync(SiteSettings site, BrandSnapshot? snapshot, CancellationToken ct)
    {
        if (snapshot is not { } kit) return site;
        var logo = site.LogoSha256;
        if (kit.LogoSha256 is { } sha && kit.LogoUrl is { } url)
        {
            if (await repository.FindMediaAsync(sha, ct) is not null) logo = sha;
            else if (await brand.DownloadAsync(url, ct) is { } bytes) logo = (await media.StoreAsync(bytes, $"ocharo-logo-r{kit.Release}.png", ct)).Sha256;
        }
        return site with { LogoSha256 = logo, Links = kit.Links.Count > 0 ? kit.Links : site.Links };
    }

    /// <summary>Resolves a public path to a page, or null for 404.</summary>
    public async Task<SitePage?> RenderAsync(string path, CancellationToken ct)
    {
        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        var language = segments.Count > 0 && segments[0] is "uz" or "ru" ? segments[0] : "en";
        if (language != "en") segments.RemoveAt(0);
        var model = await ModelAsync(LiveMedia, ct, language);
        if (language != "en" && !model.AvailableLanguages!.Contains(language)) return null;
        var renderer = new SiteRenderer(model);
        return segments.ToArray() switch
        {
            [] => Html(renderer.Home()),
            ["journal"] => Html(renderer.Archive()),
            ["journal", var slug] => model.Stories.FirstOrDefault(s => s.Slug == slug) is { } story ? Html(renderer.Article(story)) : null,
            ["topics", var key] => renderer.Topic(key) is { } topic ? Html(topic) : null,
            ["feed.xml"] => new SitePage(renderer.Feed(), "application/rss+xml; charset=utf-8"),
            ["sitemap.xml"] when language == "en" => new SitePage(renderer.Sitemap(model.AvailableLanguages!), "application/xml; charset=utf-8"),
            ["robots.txt"] when language == "en" => new SitePage(renderer.Robots(), "text/plain; charset=utf-8"),
            ["site.css"] when language == "en" => new SitePage(Css, "text/css; charset=utf-8"),
            ["boot-splash.css"] when language == "en" => new SitePage(PublicDocumentPaint.Css, "text/css; charset=utf-8"),
            _ => null,
        };
    }

    /// <summary>Renders any story (draft, scheduled, archived) as a non-indexed preview.</summary>
    public async Task<string> PreviewAsync(Guid id, CancellationToken ct)
    {
        var record = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Story");
        var story = BlogJson.Read<ArticleDocument>(record.Json);
        var model = await ModelAsync(LiveMedia, ct, story.Language);
        if (story.ProductIds.Count > 0 && model.Products.Count == 0)
            model = model with { Products = (await catalogue.GetProductsAsync(ct)).ToDictionary(p => p.Id) };
        return new SiteRenderer(model).Article(story, preview: record.Status != ArticleStatus.Published);
    }

    /// <summary>A static copy of the published site (HTML, CSS, feed, sitemap, robots and every referenced image).</summary>
    public async Task<byte[]> ExportAsync(CancellationToken ct)
    {
        var used = new Dictionary<string, string>();
        string ExportMedia(string sha)
        {
            var path = $"/media/{sha}.webp";
            used[sha] = path;
            return path;
        }

        var model = await ModelAsync(ExportMedia, ct, live: false);
        var productImages = new Dictionary<string, string>();
        SiteModel Localize(SiteModel source) => source with
        {
            Products = source.Products.ToDictionary(p => p.Key, p =>
            {
                if (p.Value.PreviewUrl is not { } url) return p.Value;
                var local = $"/media/products/{p.Key}.webp";
                productImages[local] = url;
                return p.Value with { PreviewUrl = local, ImageUrl = local };
            }),
        };
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Text(string entry, string content)
            {
                using var writer = new StreamWriter(zip.CreateEntry(entry, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
                writer.Write(content);
            }

            foreach (var language in model.AvailableLanguages!)
            {
                var localized = Localize(language == "en" ? model : await ModelAsync(ExportMedia, ct, language, live: false));
                var renderer = new SiteRenderer(localized);
                var folder = language == "en" ? "" : language + "/";
                Text($"{folder}index.html", renderer.Home());
                Text($"{folder}journal/index.html", renderer.Archive());
                foreach (var topic in localized.Topics) Text($"{folder}topics/{topic.Key}/index.html", renderer.Topic(topic.Key)!);
                foreach (var story in localized.Stories) Text($"{folder}journal/{story.Slug}/index.html", renderer.Article(story));
                Text($"{folder}feed.xml", renderer.Feed());
            }

            var root = new SiteRenderer(model);
            Text("sitemap.xml", root.Sitemap(model.AvailableLanguages!));
            Text("robots.txt", root.Robots());
            Text("boot-splash.css", PublicDocumentPaint.Css);
            Text("site.css", Css);
            foreach (var (sha, path) in used)
            {
                var opened = await media.OpenAsync(sha, preview: true, ct);
                if (opened is null) continue;
                await using var content = opened.Value.Content;
                await using var entry = zip.CreateEntry(path.TrimStart('/'), CompressionLevel.NoCompression).Open();
                await content.CopyToAsync(entry, ct);
            }

            foreach (var (path, url) in productImages)
            {
                var bytes = await catalogue.DownloadAsync(url, ct);
                if (bytes is null) continue;
                await using var entry = zip.CreateEntry(path.TrimStart('/'), CompressionLevel.NoCompression).Open();
                await entry.WriteAsync(bytes, ct);
            }
        }

        return output.ToArray();
    }

    private static string LiveMedia(string sha) => $"/media/{sha}/preview";

    private static SitePage Html(string html) => new(html, "text/html; charset=utf-8");
}
