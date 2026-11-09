using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog;

namespace OcharoBlog.Tests.E2E;

public sealed class WebCatalogueTests
{
    private const string Id = "384b3efa-1b92-42f2-9a87-1be3f285bd00";
    private const string Release = "96470c94-bab0-4be0-9352-a88b849cf8af";
    private const string Asset = "https://platform.example/api/public/v1/brands/ocharo/catalogue-editions/" + Id + "/";
    private static string Url(string file) => Asset + file + "?release=" + Release;
    private static readonly CatalogueProduct Product = new("nimcha", "Nimcha <silk>", "Vest", "Adras & silk", null,
        "https://platform.example/nimcha.webp", null, "nimcha", "A light layer with a woven pattern.", "Vest",
            "Available", [
            "S",
            "M"], [], 600000, "UZS",
        "Nimcha front", "photograph", "Ocharo", "Brand photograph",
        [new(
            "https://platform.example/nimcha.webp",
            "Nimcha front",
            "photograph",
            "Ocharo",
            "Brand photograph",
            new string('a', 64)),
         new(
             "https://platform.example/nimcha-back.webp",
             "Nimcha back",
             "photograph",
             "Ocharo",
             "Brand photograph",
             new string('b', 64))]);
    private static readonly CatalogueEdition Pdf = new(Id, "brand", "design", 2, "Print collection", "en", "classic", 1,
        DateTimeOffset.Parse("2026-10-01T10:00:00Z"), Url("catalogue.pdf"),
        [new(
            "page-1",
            1,
            "Print cover",
            210,
            297,
            [Product.Id],
            Url("page-1.png"),
            Url("page-1.pdf"),
            Url("page-1.png"))]);
    private static CatalogueCollection Collection(string slug = "woven-days", CatalogueEdition? pdf = null) =>
        new(Id, slug,
        "Woven days", "For the everyday and the unexpected.", "en", DateTimeOffset.Parse(
            "2026-10-01T10:00:00Z"), ["en", "ru"],
        new(
            "editorial",
            [
                new("first-look", "First look", "spread", [Product.Id]),
                new("details", "The details", "collage", [Product.Id])]), pdf);

    [Fact]
    public async Task Feed_ShouldParseAWebOnlyVersion_WhenItHasNoPdfPages()
    {
        var result = await Reader(Payload([Collection()])).GetCatalogueAsync(default);
        var collection = result.Catalogues!.Should().ContainSingle().Subject;
        collection.Pdf.Should().BeNull(); result.Edition.Should().BeNull();
        collection.Slug.Should().Be("woven-days");
        collection.Web!.Pages.Count.Should().Be(2);
        collection.Web.Pages[0].ProductIds.Should().ContainSingle().Subject.Should().Be(Product.Id);
    }

    [Fact]
    public async Task Feed_ShouldDropPages_WhenTheirLocaleSlugOrProductIsWrong()
    {
        var collection = Collection() with
        {
            Web =
                new(
                    "grid",
                    [
                        new("empty", "Private piece", "spread", ["unreleased"]),
                        new("good", "Public", "grid", ["nimcha", "unreleased"])])
        };
        var result =
            await Reader(Payload(
                [collection, Collection("../../private"), Collection("russian") with { Locale = "ru" }]))
                .GetCatalogueAsync(default);
        var named = result.Catalogues!.Should().ContainSingle().Subject;
        named.Web!.Pages.Should().ContainSingle().Subject.Id.Should().Be("good");
        named.Web.Pages[0].ProductIds.Should().ContainSingle().Subject.Should().Be("nimcha");
    }

    [Fact]
    public async Task Feed_ShouldRefuseTheVersion_WhenItsPdfOrThemeIsUnsafe()
    {
        var collection = Collection() with
        {
            Web = new("javascript:alert(1)", [new("first", "First", "spread", ["nimcha"])]),
            Pdf = Pdf with { PdfUrl = "https://other.example/catalogue.pdf" }
        };
        var result = await Reader(Payload([collection])).GetCatalogueAsync(default);
        result.Catalogues!.Should().BeEmpty();
        result.Products.Should().ContainSingle();
    }

    [Fact]
    public async Task GalleryAndReader_ShouldUseProductTextAndMedia_WhenNoPdfExists()
    {
        await using var harness = Harness(new Feed([Collection()]));
        var home = await harness.Client().GetStringAsync("/catalogue/?locale=en");
        home.Should().Contain("/catalogue/collections/woven-days/?locale=en");
        home.Should().Contain("Explore online");
        home.Should().NotContain("Download PDF");
        var page = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/?locale=en");
        page.Should().Contain("web-theme-editorial");
        page.Should().Contain("Nimcha &lt;silk&gt;");
        page.Should().Contain("Adras &amp; silk");
        page.Should().Contain("https://platform.example/nimcha.webp");
        page.Should().Contain("/catalogue/pieces/nimcha/?locale=en");
        page.Should().Contain("/catalogue/collections/woven-days/?locale=ru");
        page.Should().NotContain("catalogue-sheet");
        page.Should().NotContain("page-1.png");
        page.Should().Contain("data-share-page");
        if (Environment.GetEnvironmentVariable("OCHARO_CATALOGUE_LAYOUT_EVIDENCE_DIR") is { Length: > 0 } evidence)
        {
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "reader.html"), page);
            await File.WriteAllTextAsync(Path.Combine(evidence, "gallery.html"), home);
            await File.WriteAllTextAsync(
                Path.Combine(evidence, "publications.css"),
                OcharoBlog.Application.Blog.Site.PublicationSiteService.Css);
        }
    }

    [Fact]
    public async Task NamedPdf_ShouldStandAloneBesideTheLegacyReader_WhenBothExist()
    {
        var pdfOnly = Collection(pdf: Pdf) with { Web = null };
        await using var harness = Harness(new Feed([pdfOnly]));
        var home = await harness.Client().GetStringAsync("/catalogue/");
        home.Should().Contain("Download PDF"); home.Should().NotContain(">Explore online →</a>");
        var pdf = await harness.Client().GetAsync($"/catalogue/editions/{Id}/pages/page-1/");
        pdf.StatusCode.Should().Be(HttpStatusCode.OK);
        ((await harness.Client().GetAsync("/catalogue/collections/woven-days/pages/first-look/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NamedRoutes_ShouldDisappear_WhenWithdrawnOrInAnotherLocale()
    {
        var feed = new Feed([Collection()]); await using var harness = Harness(feed); var client = harness.Client();
        ((await client.GetAsync("/catalogue/collections/woven-days/pages/first-look/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.OK);
        ((await client.GetAsync("/catalogue/collections/woven-days-other/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
        ((await client.GetAsync("/catalogue/collections/woven-days/?locale=ru")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
        ((await client.GetAsync("/catalogue/collections/woven-days/pages/hidden/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
        feed.Catalogues = [];
        ((await client.GetAsync("/catalogue/collections/woven-days/")).StatusCode).Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReaderAndExport_ShouldExcludePages_WhenTheyHoldNoProducts()
    {
        var collection = Collection() with
        {
            Web =
                new(
                    "grid",
                    [
                        new("empty", "Should disappear", "spread", ["private"]),
                        new("public", "Public grid", "grid", [Product.Id])])
        };
        await using var harness = Harness(new Feed([collection])); var client = harness.Client();
        var html = await client.GetStringAsync("/catalogue/collections/woven-days/");
        html.Should().NotContain("Should disappear");
        ((await client.GetAsync("/catalogue/collections/woven-days/pages/empty/")).StatusCode)
            .Should()
            .Be(HttpStatusCode.NotFound);
        using var zip =
            new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/catalogue.zip")));
        zip.GetEntry("catalogue/collections/woven-days/pages/empty/index.html").Should().BeNull();
        zip.GetEntry("catalogue/collections/woven-days/pages/public/index.html").Should().NotBeNull();
    }

    [Fact]
    public async Task CatalogueExport_ShouldFreezeTheNamedPdf_WhenExported()
    {
        var collection = Collection(pdf: Pdf with { Locale = "fr" }) with
        {
            Locale = "fr",
            AvailableLocales = ["fr", "en"]
        };
        await using var harness = Harness(new Feed([collection]));
        using var zip =
            new ZipArchive(new MemoryStream(await harness.Client()
                .GetByteArrayAsync("/api/blog/export/catalogue.zip?locale=fr")));
        using var reader = new StreamReader(zip.GetEntry("catalogue/collections/woven-days/index.html")!.Open());
        var html = await reader.ReadToEndAsync();
        html.Should().Contain("/catalogue/collections/woven-days/pages/first-look/?locale=fr");
        html.Should().Contain($"/catalogue/files/{Id}/catalogue.pdf");
        html.Should().NotContain("?locale=en");
        zip.GetEntry("catalogue/collections/woven-days/pages/details/index.html").Should().NotBeNull();
        zip.GetEntry($"catalogue/files/{Id}/catalogue.pdf").Should().NotBeNull();
        using var manifest = new StreamReader(zip.GetEntry("publication.json")!.Open());
        (await manifest.ReadToEndAsync()).Should().Contain("woven-days");
    }

    [Fact]
    public async Task WebSettings_ShouldSelectApprovedHashesAndConfirmedTiers_WhenFrozen()
    {
        var a = new string('a', 64); var b = new string('b', 64);
        var web = Collection().Web! with
        {
            MediaGroup = "model", PriceDisplay = new(true, false, true, false, true),
            Copy = new Dictionary<string, string>
            {
                ["header"] = "La collection",
                ["footer"] = "Merci",
                ["materials"] = "Tissus",
                ["pieces"] = "pièces"
            },
            ProductMedia = new Dictionary<string, IReadOnlyList<string>> { [Product.Id] = [a, b] }
        };
        var payload =
            JsonSerializer.SerializeToNode(
                Payload([Collection() with { Web = web }]),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        payload["products"]![0]!["images"] = JsonSerializer.SerializeToNode(new[]
        {
            new
            {
                url = "/model.webp",
                alt = "Model",
                sha256 = a,
                provenance = new { approvedForPublication = true, kind = "photograph" }
            },
            new
            {
                url = "/unapproved.webp",
                alt = "Unapproved",
                sha256 = b,
                provenance = new { approvedForPublication = false, kind = "photograph" }
            }
        });
        payload["products"]![0]!["pricing"] =
            JsonSerializer.SerializeToNode(new { currency = "UZS", tiers = new object[]
        {
            new { minimumQuantity = 1, maximumQuantity = (int?)4, unitPrice = 600000 },
            new { minimumQuantity = 5, maximumQuantity = (int?)null, unitPrice = 450000 }
        } });
        var result = await Reader(payload).GetCatalogueAsync(default);
        var product = result.Products.Should().ContainSingle().Subject;
        var collection = result.Catalogues!.Should().ContainSingle().Subject;
        (collection.Web!.ProductMedia![Product.Id]).Should().ContainSingle().Subject.Should().Be(a);
        product.Pricing!.Tiers.Count.Should().Be(2);
        collection.Web.PriceDisplay!.ShowRetail.Should().BeFalse();
        (collection.Web.Copy!["materials"]).Should().Be("Tissus");
    }

    [Fact]
    public async Task Reader_ShouldHonorTheVersionsChoices_WhenMediaAndPricesAreHidden()
    {
        var web = Collection().Web! with
        {
            MediaGroup = "fabric", PriceDisplay = new(false),
            Copy = new Dictionary<string, string>
            {
                ["header"] = "Atelier <Ocharo>",
                ["footer"] = "Merci",
                ["materials"] = "Tissus",
                ["sizes"] = "Tailles"
            },
            ProductMedia = new Dictionary<string, IReadOnlyList<string>> { [Product.Id] = [] }
        };
        await using var harness = Harness(new Feed([Collection() with { Web = web }]));
        var html = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/");
        html.Should().NotContain("600,000");
        html.Should().NotContain("nimcha.webp"); html.Should().NotContain("nimcha-back.webp");
        html.Should().Contain("Images are being prepared");
        html.Should().Contain("Atelier &lt;Ocharo&gt;"); html.Should().Contain("Merci");
        html.Should().Contain("Tissus"); html.Should().Contain("Tailles");
    }

    [Fact]
    public async Task Reader_ShouldFilterRetailAndWholesaleApart_WhenBothAreOffered()
    {
        var web = Collection().Web! with
        {
            PriceDisplay = new(true, false, true, false, true),
            Copy = new Dictionary<string, string> { ["pieces"] = "pièces" },
            ProductMedia = new Dictionary<string, IReadOnlyList<string>>
            {
                [Product.Id] = [new string('b',64), new string('a',64)]
            }
        };
        var feed = new Feed([Collection() with { Web = web }])
        {
            Piece = Product with { Pricing = new("UZS", [new(1,4,600000), new(5,null,450000)]) }
        };
        await using var harness = Harness(feed);
        var html = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/");
        html.Should().NotContain("600,000"); html.Should().NotContain("UZS");
        html.Should().Contain("450,000"); WebUtility.HtmlDecode(html).Should().Contain("5+ pièces");
        (html.IndexOf(
            "nimcha-back.webp",
            StringComparison.Ordinal) < html.IndexOf("nimcha.webp", StringComparison.Ordinal))
            .Should()
            .BeTrue();
        feed.Catalogues = [Collection() with { Web = web with { PriceDisplay = new(true, true, false, true, false) } }];
        html = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/");
        html.Should().Contain("600,000 UZS");
        html.Should().NotContain("450,000");
        WebUtility.HtmlDecode(html).Should().NotContain("pièces");
    }

    private static object Payload(IReadOnlyList<CatalogueCollection> catalogues) => new
    {
        schemaVersion = 1, release = new { id = Release }, availableLocales = new[] { "en" }, catalogues,
        products = new[] { new { id = Product.Id, title = Product.Name, images = Array.Empty<object>() } }
    };
    private static CatalogueFeed Reader(object payload) => new(new HttpClient(new Handler(payload)),
        Options.Create(
            new BlogOptions
            {
                PlatformBaseUrl = "https://platform.example",
                BrandSlug = "ocharo"
            }), NullLogger<CatalogueFeed>.Instance);
    private static BlogHarness Harness(Feed feed) =>
        new(seed: false, services: services => services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(feed)));
    private sealed class Handler(object payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(payload) });
    }
    private sealed class Feed(IReadOnlyList<CatalogueCollection> catalogues) : ICatalogueFeed
    {
        public IReadOnlyList<CatalogueCollection> Catalogues { get; set; } = catalogues;
        public CatalogueProduct Piece { get; set; } = Product;
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<CatalogueProduct>>([Piece]);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") =>
            Task.FromResult(new CatalogueSnapshot([Piece], null, [locale], Catalogues));
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) =>
            Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(url));
    }
}
