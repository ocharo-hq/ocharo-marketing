using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog;

namespace OcharoBlog.Tests;

public sealed class CatalogueEditionTests
{
    private const string Id = "394b3efa-1b92-42f2-9a87-1be3f285bd00";
    private const string ReleaseId = "96470c94-bab0-4be0-9352-a88b849cf8af";
    private const string Prefix = "https://platform.example/api/public/v1/brands/ocharo/catalogue-editions/" + Id + "/";
    private static string AssetUrl(string file) => Prefix + file + "?release=" + ReleaseId;
    private static readonly CatalogueProduct Piece = new("abaya-v01", "Abaya", "Abaya", "Cotton", null, null, null, "abaya");
    private static readonly CatalogueEdition Edition = new(Id, "brand", "source", 7, "Ocharo 2026", "en", "ocharo-journal-v22", 2,
        DateTimeOffset.Parse("2026-09-29T10:00:00Z"), AssetUrl("catalogue.pdf"),
        [new("catalogue-01", 1, "Cover", 210, 297, [], AssetUrl("catalogue-01.png"), AssetUrl("catalogue-01.pdf"), AssetUrl("catalogue-01.png")),
         new("catalogue-02", 2, "Abaya", 210, 297, ["abaya-v01"], AssetUrl("catalogue-02.png"), AssetUrl("catalogue-02.pdf"), AssetUrl("catalogue-02.png"))]);

    [Fact]
    public async Task Same_release_supplies_products_and_strictly_pinned_edition_assets()
    {
        var handler = new FeedHandler(Feed());
        var result = await Reader(handler).GetCatalogueAsync(default);
        Assert.Single(result.Products);
        Assert.Equal(Id, result.Edition!.Id);
        Assert.Equal("catalogue-01", result.Edition.Pages[0].Id);
        Assert.Equal(AssetUrl("catalogue.pdf"), result.Edition.PdfUrl);
        Assert.Equal(1, handler.Requests);
    }

    [Theory]
    [InlineData("https://evil.example/catalogue.pdf")]
    [InlineData("https://platform.example/api/private/catalogue.pdf")]
    [InlineData("https://platform.example/api/public/v1/brands/other/catalogue-editions/394b3efa-1b92-42f2-9a87-1be3f285bd00/catalogue.pdf")]
    [InlineData(Prefix + "catalogue.pdf?draft=true")]
    [InlineData("https://user@platform.example/api/public/v1/brands/ocharo/catalogue-editions/394b3efa-1b92-42f2-9a87-1be3f285bd00/catalogue.pdf")]
    [InlineData(Prefix + "catalogue.pdf")]
    [InlineData(Prefix + "catalogue.pdf?release=00000000-0000-0000-0000-000000000000")]
    [InlineData(Prefix + "catalogue.pdf?release=" + ReleaseId + "&release=" + ReleaseId)]
    public async Task Unsafe_edition_asset_is_rejected_without_hiding_valid_products(string url)
    {
        var result = await Reader(new FeedHandler(Feed(pdf: url))).GetCatalogueAsync(default);
        Assert.Single(result.Products);
        Assert.Null(result.Edition);
    }

    [Fact]
    public async Task Invalid_page_order_or_count_never_exposes_partial_edition()
    {
        var result = await Reader(new FeedHandler(Feed(pageCount: 3))).GetCatalogueAsync(default);
        Assert.Single(result.Products);
        Assert.Null(result.Edition);
    }

    [Fact]
    public async Task Withdrawal_does_not_resurrect_the_previous_successful_feed()
    {
        var handler = new FeedHandler(Feed()); var reader = Reader(handler);
        Assert.NotNull((await reader.GetCatalogueAsync(default)).Edition);
        handler.Status = HttpStatusCode.NotFound;
        var result = await reader.GetCatalogueAsync(default);
        Assert.Empty(result.Products); Assert.Null(result.Edition);
    }

    [Fact]
    public async Task Buyer_view_and_original_page_reader_have_all_downloads_and_piece_links()
    {
        var feed = new FakeFeed(); await using var h = Harness(feed); var client = h.Client();
        var response = await client.GetAsync("/catalogue/");
        Assert.True(response.Headers.CacheControl?.NoStore);
        var home = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ocharo 2026", home); Assert.Contains("/catalogue/pieces/abaya/", home);
        Assert.Contains(AssetUrl("catalogue.pdf") + "&amp;download=1", home);
        var gallery = await client.GetStringAsync($"/catalogue/editions/{Id}/");
        Assert.Contains("catalogue-02.png?release=" + ReleaseId + "&amp;download=1", gallery);
        var page = await client.GetStringAsync($"/catalogue/editions/{Id}/pages/catalogue-02/");
        Assert.Contains("Page 2 of 2", page); Assert.Contains("catalogue-02.pdf?release=" + ReleaseId + "&amp;download=1", page);
        Assert.Contains("catalogue-02.png?release=" + ReleaseId + "&amp;download=1", page); Assert.Contains("Pieces on this page.", page);
        Assert.Contains("rel=\"prev\"", page); Assert.DoesNotContain("rel=\"next\"", page);
        Assert.Contains("data-share-page", page);
    }

    [Fact]
    public async Task Viewer_urls_remain_edition_pinned_and_disappear_after_withdrawal()
    {
        var feed = new FakeFeed(); await using var h = Harness(feed); var client = h.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/catalogue/editions/other/")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/catalogue/editions/{Id}/pages/private/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/catalogue/editions/{Id}/")).StatusCode);
        feed.Current = new([]);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/catalogue/editions/{Id}/")).StatusCode);
        Assert.DoesNotContain(Id, await client.GetStringAsync("/catalogue/"));
    }

    [Fact]
    public async Task Explicit_static_export_preserves_exact_page_and_pdf_bytes_with_release_identity()
    {
        var feed = new FakeFeed(); await using var h = Harness(feed); var client = h.Client();
        var bytes = await client.GetByteArrayAsync("/api/blog/export/catalogue.zip");
        using var zip = new ZipArchive(new MemoryStream(bytes));
        foreach (var file in new[] { "catalogue.pdf", "catalogue-01.png", "catalogue-01.pdf", "catalogue-02.png", "catalogue-02.pdf" })
        {
            var entry = zip.GetEntry($"catalogue/files/{Id}/{file}"); Assert.NotNull(entry);
            using var output = new MemoryStream(); await entry!.Open().CopyToAsync(output);
            Assert.Equal(FakeFeed.Bytes(AssetUrl(file)), output.ToArray());
        }
        Assert.NotNull(zip.GetEntry($"catalogue/editions/{Id}/pages/catalogue-02/index.html"));
        using var pageReader = new StreamReader(zip.GetEntry($"catalogue/editions/{Id}/pages/catalogue-02/index.html")!.Open());
        var html = await pageReader.ReadToEndAsync();
        Assert.Contains($"/catalogue/files/{Id}/catalogue-02.png", html);
        Assert.DoesNotContain("platform.example", html);
        using var manifestReader = new StreamReader(zip.GetEntry("publication.json")!.Open());
        Assert.Contains(Id, await manifestReader.ReadToEndAsync());
    }

    [Fact]
    public async Task Unavailable_edition_file_fails_export_instead_of_shipping_broken_downloads()
    {
        var feed = new FakeFeed { MissingFile = true }; await using var h = Harness(feed);
        var result = await h.Client().GetAsync("/api/blog/export/catalogue.zip");
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Contains("released catalogue file is unavailable", await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Selected_catalogue_locale_reaches_feed_and_requires_matching_edition()
    {
        var russian = new FeedHandler(Feed(locale: "ru"));
        var result = await Reader(russian).GetCatalogueAsync(default, "ru");
        Assert.Equal("?locale=ru", russian.LastUri!.Query);
        Assert.Equal("ru", result.Edition!.Locale);
        Assert.Null((await Reader(new FeedHandler(Feed())).GetCatalogueAsync(default, "ru")).Edition);
        var invalid = new FeedHandler(Feed());
        Assert.NotNull((await Reader(invalid).GetCatalogueAsync(default, "invalid")).Edition);
        Assert.Equal("?locale=en", invalid.LastUri!.Query);
    }

    [Theory]
    [InlineData("uz")][InlineData("ru")][InlineData("en")]
    public async Task Buyer_reader_and_exports_preserve_selected_locale(string locale)
    {
        var feed = new FakeFeed();
        feed.Localized[locale] = new([Piece], Edition with { Locale = locale });
        await using var h = Harness(feed); var client = h.Client();
        var home = await client.GetStringAsync($"/catalogue/?locale={locale}");
        Assert.Equal(locale, feed.LastLocale);
        Assert.Contains($"href=\"/catalogue/pieces/abaya/?locale={locale}\"", home);
        Assert.Contains($"name=\"locale\" value=\"{locale}\"", home);
        Assert.Contains($"href=\"/catalogue/?locale={locale}\" lang=\"{locale}\" aria-current=\"true\"", home);
        var page = await client.GetStringAsync($"/catalogue/editions/{Id}/pages/catalogue-02/?locale={locale}");
        Assert.Contains($"/catalogue/editions/{Id}/pages/catalogue-01/?locale={locale}", page);
        Assert.Contains("catalogue-02.pdf?release=" + ReleaseId + "&amp;download=1", page);
        using var zip = new ZipArchive(new MemoryStream(await client.GetByteArrayAsync($"/api/blog/export/catalogue.zip?locale={locale}")));
        using var html = new StreamReader(zip.GetEntry("index.html")!.Open());
        var exported = await html.ReadToEndAsync();
        Assert.Contains($"/catalogue/pieces/abaya/?locale={locale}", exported);
        foreach (var other in new[] { "uz", "ru", "en" }.Where(value => value != locale))
            Assert.DoesNotContain("?locale=" + other, exported);
        var fallback = await client.GetStringAsync("/catalogue/?locale=invalid");
        Assert.Equal("en", feed.LastLocale); Assert.Contains("lang=\"en\"", fallback);
    }

    private static object Feed(string? pdf = null, int pageCount = 2, string locale = "en") => new
    {
        schemaVersion = 1, release = new { id = ReleaseId },
        products = new[] { new { id = "abaya-v01", title = "Abaya", images = Array.Empty<object>() } },
        edition = new { Edition.Id, Edition.BrandId, Edition.SourceDesignId, Edition.SourceRevision, Edition.Title,
            locale, Edition.TemplateId, pageCount, Edition.ReleasedAt, pdfUrl = pdf ?? Edition.PdfUrl, Edition.Pages },
    };
    private static CatalogueFeed Reader(FeedHandler handler) => new(new HttpClient(handler),
        Options.Create(new BlogOptions { PlatformBaseUrl = "https://platform.example", BrandSlug = "ocharo" }),
        NullLogger<CatalogueFeed>.Instance);
    private static BlogHarness Harness(FakeFeed feed) => new(seed: false, services: services => services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(feed)));
    private sealed class FeedHandler(object payload) : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public int Requests { get; private set; }
        public Uri? LastUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { Requests++; LastUri = request.RequestUri; return Task.FromResult(new HttpResponseMessage(Status) { Content = JsonContent.Create(payload) }); }
    }
    private sealed class FakeFeed : ICatalogueFeed
    {
        public CatalogueSnapshot Current { get; set; } = new([Piece], Edition);
        public bool MissingFile { get; init; }
        public string? LastLocale { get; private set; }
        public Dictionary<string, CatalogueSnapshot> Localized { get; } = [];
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) => Task.FromResult(Current.Products);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") { LastLocale = locale; return Task.FromResult(Localized.GetValueOrDefault(locale, Current)); }
        public static byte[] Bytes(string url) => Encoding.UTF8.GetBytes("exact released bytes: " + url);
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) => Task.FromResult<byte[]?>(MissingFile ? null : Bytes(url));
    }
}
