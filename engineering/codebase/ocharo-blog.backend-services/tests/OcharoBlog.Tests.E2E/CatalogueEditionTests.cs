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

namespace OcharoBlog.Tests.E2E;

public sealed class CatalogueEditionTests
{
    private const string Id = "394b3efa-1b92-42f2-9a87-1be3f285bd00";
    private const string ReleaseId = "96470c94-bab0-4be0-9352-a88b849cf8af";
    private const string Prefix = "https://platform.example/api/public/v1/brands/ocharo/catalogue-editions/" + Id + "/";
    private static string AssetUrl(string file) => Prefix + file + "?release=" + ReleaseId;
    private static readonly CatalogueProduct Piece =
        new("abaya-v01", "Abaya", "Abaya", "Cotton", null, null, null, "abaya");
    private static readonly CatalogueEdition Edition =
        new(Id, "brand", "source", 7, "Ocharo 2026", "en", "ocharo-journal-v22", 2,
        DateTimeOffset.Parse("2026-09-29T10:00:00Z"), AssetUrl("catalogue.pdf"),
        [new(
            "catalogue-01",
            1,
            "Cover",
            210,
            297,
            [],
            AssetUrl("catalogue-01.png"),
            AssetUrl("catalogue-01.pdf"),
            AssetUrl("catalogue-01.png")),
         new(
             "catalogue-02",
             2,
             "Abaya",
             210,
             297,
             ["abaya-v01"],
             AssetUrl("catalogue-02.png"),
             AssetUrl("catalogue-02.pdf"),
             AssetUrl("catalogue-02.png"))]);

    [Fact]
    public async Task Edition_ShouldPinItsAssetsToTheProductsRelease_WhenBothAreReleased()
    {
        var handler = new FeedHandler(Feed());
        var result = await Reader(handler).GetCatalogueAsync(default);
        result.Products.Should().ContainSingle();
        result.Edition!.Id.Should().Be(Id);
        result.Edition.Pages[0].Id.Should().Be("catalogue-01");
        result.Edition.PdfUrl.Should().Be(AssetUrl("catalogue.pdf"));
        handler.Requests.Should().Be(1);
    }

    [Theory]
    [InlineData("https://evil.example/catalogue.pdf")]
    [InlineData("https://platform.example/api/private/catalogue.pdf")]
    [InlineData(
        "https://platform.example/api/public/v1/brands/other/catalogue-editions/394b3efa-1b92-42f2-9a87-1be3f28"
            + "5bd00/catalogue.pdf")]
    [InlineData(Prefix + "catalogue.pdf?draft=true")]
    [InlineData(
        "https://user@platform.example/api/public/v1/brands/ocharo/catalogue-editions/394b3efa-1b92-42f2-9a87-1"
            + "be3f285bd00/catalogue.pdf")]
    [InlineData(Prefix + "catalogue.pdf")]
    [InlineData(Prefix + "catalogue.pdf?release=00000000-0000-0000-0000-000000000000")]
    [InlineData(Prefix + "catalogue.pdf?release=" + ReleaseId + "&release=" + ReleaseId)]
    public async Task Edition_ShouldBeRejectedWithProductsKept_WhenAnAssetIsUnsafe(string url)
    {
        var result = await Reader(new FeedHandler(Feed(pdf: url))).GetCatalogueAsync(default);
        result.Products.Should().ContainSingle();
        result.Edition.Should().BeNull();
    }

    [Fact]
    public async Task Edition_ShouldStayHidden_WhenItsPageOrderOrCountIsInvalid()
    {
        var result = await Reader(new FeedHandler(Feed(pageCount: 3))).GetCatalogueAsync(default);
        result.Products.Should().ContainSingle();
        result.Edition.Should().BeNull();
    }

    [Fact]
    public async Task Edition_ShouldStayWithdrawn_WhenAnEarlierFeedSucceeded()
    {
        var handler = new FeedHandler(Feed()); var reader = Reader(handler);
        ((await reader.GetCatalogueAsync(default)).Edition).Should().NotBeNull();
        handler.Status = HttpStatusCode.NotFound;
        var result = await reader.GetCatalogueAsync(default);
        result.Products.Should().BeEmpty(); result.Edition.Should().BeNull();
    }

    [Fact]
    public async Task Reader_ShouldOfferEveryDownloadAndPieceLink_WhenAnEditionIsReleased()
    {
        var feed = new FakeFeed(); await using var h = Harness(feed); var client = h.Client();
        var response = await client.GetAsync("/catalogue/");
        (response.Headers.CacheControl?.NoStore).Should().BeTrue();
        var home = await response.Content.ReadAsStringAsync();
        home.Should().Contain("Ocharo 2026"); home.Should().Contain("/catalogue/pieces/abaya/");
        home.Should().Contain(AssetUrl("catalogue.pdf") + "&amp;download=1");
        var gallery = await client.GetStringAsync($"/catalogue/editions/{Id}/");
        gallery.Should().Contain("catalogue-02.png?release=" + ReleaseId + "&amp;download=1");
        var page = await client.GetStringAsync($"/catalogue/editions/{Id}/pages/catalogue-02/");
        page.Should().Contain("Page 2 of 2");
        page.Should().Contain("catalogue-02.pdf?release=" + ReleaseId + "&amp;download=1");
        page.Should().Contain("catalogue-02.png?release=" + ReleaseId + "&amp;download=1");
        page.Should().Contain("Pieces on this page.");
        page.Should().Contain("rel=\"prev\""); page.Should().NotContain("rel=\"next\"");
        page.Should().Contain("data-share-page");
    }

    [Fact]
    public async Task ViewerUrls_ShouldDisappear_WhenTheirEditionIsWithdrawn()
    {
        var feed = new FakeFeed(); await using var h = Harness(feed); var client = h.Client();
        ((await client.GetAsync("/catalogue/editions/other/")).StatusCode).Should().Be(HttpStatusCode.NotFound);
        ((await client.GetAsync($"/catalogue/editions/{Id}/pages/private/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
        ((await client.GetAsync($"/catalogue/editions/{Id}/")).StatusCode).Should().Be(HttpStatusCode.OK);
        feed.Current = new([]);
        ((await client.GetAsync($"/catalogue/editions/{Id}/")).StatusCode).Should().Be(HttpStatusCode.NotFound);
        (await client.GetStringAsync("/catalogue/")).Should().NotContain(Id);
    }

    [Fact]
    public async Task CatalogueExport_ShouldKeepExactPageAndPdfBytes_WhenExported()
    {
        var feed = new FakeFeed(); await using var h = Harness(feed); var client = h.Client();
        var bytes = await client.GetByteArrayAsync("/api/blog/export/catalogue.zip");
        using var zip = new ZipArchive(new MemoryStream(bytes));
        foreach (var file in new[]
            {
                "catalogue.pdf",
                "catalogue-01.png",
                "catalogue-01.pdf",
                "catalogue-02.png",
                "catalogue-02.pdf"
            })
        {
            var entry = zip.GetEntry($"catalogue/files/{Id}/{file}"); entry.Should().NotBeNull();
            using var output = new MemoryStream(); await entry!.Open().CopyToAsync(output);
            output.ToArray().Should().Equal(FakeFeed.Bytes(AssetUrl(file)));
        }
        zip.GetEntry($"catalogue/editions/{Id}/pages/catalogue-02/index.html").Should().NotBeNull();
        using var pageReader =
            new StreamReader(zip.GetEntry($"catalogue/editions/{Id}/pages/catalogue-02/index.html")!.Open());
        var html = await pageReader.ReadToEndAsync();
        html.Should().Contain($"/catalogue/files/{Id}/catalogue-02.png");
        html.Should().NotContain("platform.example");
        using var manifestReader = new StreamReader(zip.GetEntry("publication.json")!.Open());
        (await manifestReader.ReadToEndAsync()).Should().Contain(Id);
    }

    [Fact]
    public async Task CatalogueExport_ShouldFail_WhenAnEditionFileIsUnavailable()
    {
        var feed = new FakeFeed { MissingFile = true }; await using var h = Harness(feed);
        var result = await h.Client().GetAsync("/api/blog/export/catalogue.zip");
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await result.Content.ReadAsStringAsync()).Should().Contain("released catalogue file is unavailable");
    }

    [Fact]
    public async Task Locale_ShouldReachTheFeedAndRequireItsEdition_WhenOneIsSelected()
    {
        var russian = new FeedHandler(Feed(locale: "ru"));
        var result = await Reader(russian).GetCatalogueAsync(default, "ru");
        russian.LastUri!.Query.Should().Be("?locale=ru");
        result.Edition!.Locale.Should().Be("ru");
        ((await Reader(new FeedHandler(Feed())).GetCatalogueAsync(default, "ru")).Edition).Should().BeNull();
        var invalid = new FeedHandler(Feed());
        ((await Reader(invalid).GetCatalogueAsync(default, "invalid")).Edition).Should().NotBeNull();
        invalid.LastUri!.Query.Should().Be("?locale=en");
    }

    [Theory]
    [InlineData("uz")][InlineData("ru")][InlineData("en")][InlineData("fr")][InlineData("ar")][InlineData("zh-Hans")]
    public async Task ReaderAndExport_ShouldKeepTheLocale_WhenOneIsSelected(string locale)
    {
        var feed = new FakeFeed();
        feed.Localized[locale] = new([Piece], Edition with { Locale = locale });
        await using var h = Harness(feed); var client = h.Client();
        var home = await client.GetStringAsync($"/catalogue/?locale={locale}");
        feed.LastLocale.Should().Be(locale);
        home.Should().Contain($"href=\"/catalogue/pieces/abaya/?locale={locale}\"");
        home.Should().Contain($"name=\"locale\" value=\"{locale}\"");
        home.Should().Contain($"href=\"/catalogue/?locale={locale}\" lang=\"{locale}\" aria-current=\"true\"");
        var page = await client.GetStringAsync($"/catalogue/editions/{Id}/pages/catalogue-02/?locale={locale}");
        page.Should().Contain($"/catalogue/editions/{Id}/pages/catalogue-01/?locale={locale}");
        page.Should().Contain("catalogue-02.pdf?release=" + ReleaseId + "&amp;download=1");
        using var zip =
            new ZipArchive(new MemoryStream(await client.GetByteArrayAsync(
                $"/api/blog/export/catalogue.zip?locale={locale}")));
        using var html = new StreamReader(zip.GetEntry("index.html")!.Open());
        var exported = await html.ReadToEndAsync();
        exported.Should().Contain($"/catalogue/pieces/abaya/?locale={locale}");
        foreach (var other in new[] { "uz", "ru", "en" }.Where(value => value != locale))
            exported.Should().NotContain("?locale=" + other);
        var fallback = await client.GetStringAsync("/catalogue/?locale=invalid");
        feed.LastLocale.Should().Be("en"); fallback.Should().Contain("lang=\"en\"");
    }

    [Theory]
    [InlineData("fr")][InlineData("ar")][InlineData("zh-Hans")]
    public async Task LocaleSelector_ShouldShowOnlyReleasedEditions_WhenLocalesAreRequested(string locale)
    {
        var handler = new FeedHandler(Feed(locale: locale));
        var result = await Reader(handler).GetCatalogueAsync(default, locale);
        handler.LastUri!.Query.Should().Be("?locale=" + locale);
        result.Edition!.Locale.Should().Be(locale);
        result.AvailableLocales.Should().Equal(new[] { locale });
        ((await Reader(new FeedHandler(Feed())).GetCatalogueAsync(default, locale)).Edition).Should().BeNull();
        var feed = new FakeFeed { Current = new([Piece], Edition with { Locale = locale }, [locale]) };
        await using var h = Harness(feed);
        var html = await h.Client().GetStringAsync("/catalogue/?locale=" + locale);
        html.Should().Contain($"href=\"/catalogue/?locale={locale}\" lang=\"{locale}\"");
        html.Should().NotContain("href=\"/catalogue/?locale=ru\" lang=\"ru\"");
        html.Should().NotContain("href=\"/catalogue/?locale=uz\" lang=\"uz\"");
        html.Should().Contain($"data-content-locale=\"{locale}\"");
        html.Should().Contain($"lang=\"{locale}\" dir=\"{(locale == "ar" ? "rtl" : "ltr")}\"");
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
    private static BlogHarness Harness(FakeFeed feed) =>
        new(seed: false, services: services => services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(feed)));
    private sealed class FeedHandler(object payload) : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public int Requests { get; private set; }
        public Uri? LastUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++;
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = JsonContent.Create(payload) });
        }
    }
    private sealed class FakeFeed : ICatalogueFeed
    {
        public CatalogueSnapshot Current { get; set; } = new([Piece], Edition);
        public bool MissingFile { get; init; }
        public string? LastLocale { get; private set; }
        public Dictionary<string, CatalogueSnapshot> Localized { get; } = [];
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) =>
            Task.FromResult(Current.Products);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en")
        {
            LastLocale = locale; return Task.FromResult(Localized.GetValueOrDefault(locale, Current));
        }
        public static byte[] Bytes(string url) => Encoding.UTF8.GetBytes("exact released bytes: " + url);
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) =>
            Task.FromResult<byte[]?>(MissingFile ? null : Bytes(url));
    }
}
