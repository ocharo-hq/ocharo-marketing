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

namespace OcharoBlog.Tests;

public sealed class WebCatalogueTests
{
    private const string Id = "384b3efa-1b92-42f2-9a87-1be3f285bd00";
    private const string Release = "96470c94-bab0-4be0-9352-a88b849cf8af";
    private const string Asset = "https://platform.example/api/public/v1/brands/ocharo/catalogue-editions/" + Id + "/";
    private static string Url(string file) => Asset + file + "?release=" + Release;
    private static readonly CatalogueProduct Product = new("nimcha", "Nimcha <silk>", "Vest", "Adras & silk", null,
        "https://platform.example/nimcha.webp", null, "nimcha", "A light layer with a woven pattern.", "Vest", "Available", ["S", "M"], [], 600000, "UZS",
        "Nimcha front", "photograph", "Ocharo", "Brand photograph",
        [new("https://platform.example/nimcha.webp", "Nimcha front", "photograph", "Ocharo", "Brand photograph", new string('a', 64)),
         new("https://platform.example/nimcha-back.webp", "Nimcha back", "photograph", "Ocharo", "Brand photograph", new string('b', 64))]);
    private static readonly CatalogueEdition Pdf = new(Id, "brand", "design", 2, "Print collection", "en", "classic", 1,
        DateTimeOffset.Parse("2026-10-01T10:00:00Z"), Url("catalogue.pdf"),
        [new("page-1", 1, "Print cover", 210, 297, [Product.Id], Url("page-1.png"), Url("page-1.pdf"), Url("page-1.png"))]);
    private static CatalogueCollection Collection(string slug = "woven-days", CatalogueEdition? pdf = null) => new(Id, slug,
        "Woven days", "For the everyday and the unexpected.", "en", DateTimeOffset.Parse("2026-10-01T10:00:00Z"), ["en", "ru"],
        new("editorial", [new("first-look", "First look", "spread", [Product.Id]), new("details", "The details", "collage", [Product.Id])]), pdf);

    [Fact]
    public async Task Feed_parses_named_web_only_version_without_requiring_pdf_pages()
    {
        var result = await Reader(Payload([Collection()])).GetCatalogueAsync(default);
        var collection = Assert.Single(result.Catalogues!);
        Assert.Null(collection.Pdf); Assert.Null(result.Edition);
        Assert.Equal("woven-days", collection.Slug);
        Assert.Equal(2, collection.Web!.Pages.Count);
        Assert.Equal(Product.Id, Assert.Single(collection.Web.Pages[0].ProductIds));
    }

    [Fact]
    public async Task Feed_drops_foreign_locale_unsafe_slug_and_unknown_product_pages()
    {
        var collection = Collection() with { Web = new("grid", [new("empty", "Private piece", "spread", ["unreleased"]), new("good", "Public", "grid", ["nimcha", "unreleased"])]) };
        var result = await Reader(Payload([collection, Collection("../../private"), Collection("russian") with { Locale = "ru" }])).GetCatalogueAsync(default);
        var named = Assert.Single(result.Catalogues!);
        Assert.Equal("good", Assert.Single(named.Web!.Pages).Id);
        Assert.Equal("nimcha", Assert.Single(named.Web.Pages[0].ProductIds));
    }

    [Fact]
    public async Task Feed_does_not_accept_unsafe_named_pdf_or_arbitrary_theme()
    {
        var collection = Collection() with { Web = new("javascript:alert(1)", [new("first", "First", "spread", ["nimcha"])]), Pdf = Pdf with { PdfUrl = "https://other.example/catalogue.pdf" } };
        var result = await Reader(Payload([collection])).GetCatalogueAsync(default);
        Assert.Empty(result.Catalogues!);
        Assert.Single(result.Products);
    }

    [Fact]
    public async Task Gallery_and_reader_use_product_text_and_media_without_pdf_dependency()
    {
        await using var harness = Harness(new Feed([Collection()]));
        var home = await harness.Client().GetStringAsync("/catalogue/?locale=en");
        Assert.Contains("/catalogue/collections/woven-days/?locale=en", home);
        Assert.Contains("Explore online", home);
        Assert.DoesNotContain("Download PDF", home);
        var page = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/?locale=en");
        Assert.Contains("web-theme-editorial", page);
        Assert.Contains("Nimcha &lt;silk&gt;", page);
        Assert.Contains("Adras &amp; silk", page);
        Assert.Contains("https://platform.example/nimcha.webp", page);
        Assert.Contains("/catalogue/pieces/nimcha/?locale=en", page);
        Assert.Contains("/catalogue/collections/woven-days/?locale=ru", page);
        Assert.DoesNotContain("catalogue-sheet", page);
        Assert.DoesNotContain("page-1.png", page);
        Assert.Contains("data-share-page", page);
        if (Environment.GetEnvironmentVariable("OCHARO_CATALOGUE_LAYOUT_EVIDENCE_DIR") is { Length: > 0 } evidence)
        {
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "reader.html"), page);
            await File.WriteAllTextAsync(Path.Combine(evidence, "gallery.html"), home);
            await File.WriteAllTextAsync(Path.Combine(evidence, "publications.css"), OcharoBlog.Application.Blog.Site.PublicationSiteService.Css);
        }
    }

    [Fact]
    public async Task Named_pdf_can_stand_alone_and_legacy_pinned_reader_stays_available()
    {
        var pdfOnly = Collection(pdf: Pdf) with { Web = null };
        await using var harness = Harness(new Feed([pdfOnly]));
        var home = await harness.Client().GetStringAsync("/catalogue/");
        Assert.Contains("Download PDF", home); Assert.DoesNotContain(">Explore online →</a>", home);
        var pdf = await harness.Client().GetAsync($"/catalogue/editions/{Id}/pages/page-1/");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await harness.Client().GetAsync("/catalogue/collections/woven-days/pages/first-look/")).StatusCode);
    }

    [Fact]
    public async Task Named_routes_are_exact_and_withdrawn_or_wrong_locale_versions_disappear()
    {
        var feed = new Feed([Collection()]); await using var harness = Harness(feed); var client = harness.Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/catalogue/collections/woven-days/pages/first-look/")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/catalogue/collections/woven-days-other/")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/catalogue/collections/woven-days/?locale=ru")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/catalogue/collections/woven-days/pages/hidden/")).StatusCode);
        feed.Catalogues = [];
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/catalogue/collections/woven-days/")).StatusCode);
    }

    [Fact]
    public async Task Empty_product_pages_are_defensively_excluded_from_reader_and_exports()
    {
        var collection = Collection() with { Web = new("grid", [new("empty", "Should disappear", "spread", ["private"]), new("public", "Public grid", "grid", [Product.Id])]) };
        await using var harness = Harness(new Feed([collection])); var client = harness.Client();
        var html = await client.GetStringAsync("/catalogue/collections/woven-days/");
        Assert.DoesNotContain("Should disappear", html);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/catalogue/collections/woven-days/pages/empty/")).StatusCode);
        using var zip = new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/catalogue.zip")));
        Assert.Null(zip.GetEntry("catalogue/collections/woven-days/pages/empty/index.html"));
        Assert.NotNull(zip.GetEntry("catalogue/collections/woven-days/pages/public/index.html"));
    }

    [Fact]
    public async Task Static_export_freezes_named_pdf_and_keeps_slug_and_locale_exact()
    {
        var collection = Collection(pdf: Pdf with { Locale = "fr" }) with { Locale = "fr", AvailableLocales = ["fr", "en"] };
        await using var harness = Harness(new Feed([collection]));
        using var zip = new ZipArchive(new MemoryStream(await harness.Client().GetByteArrayAsync("/api/blog/export/catalogue.zip?locale=fr")));
        using var reader = new StreamReader(zip.GetEntry("catalogue/collections/woven-days/index.html")!.Open());
        var html = await reader.ReadToEndAsync();
        Assert.Contains("/catalogue/collections/woven-days/pages/first-look/?locale=fr", html);
        Assert.Contains($"/catalogue/files/{Id}/catalogue.pdf", html);
        Assert.DoesNotContain("?locale=en", html);
        Assert.NotNull(zip.GetEntry("catalogue/collections/woven-days/pages/details/index.html"));
        Assert.NotNull(zip.GetEntry($"catalogue/files/{Id}/catalogue.pdf"));
        using var manifest = new StreamReader(zip.GetEntry("publication.json")!.Open());
        Assert.Contains("woven-days", await manifest.ReadToEndAsync());
    }

    [Fact]
    public async Task Frozen_web_settings_select_approved_hashes_and_confirmed_price_tiers()
    {
        var a = new string('a', 64); var b = new string('b', 64);
        var web = Collection().Web! with
        {
            MediaGroup = "model", PriceDisplay = new(true, false, true, false, true),
            Copy = new Dictionary<string, string> { ["header"] = "La collection", ["footer"] = "Merci", ["materials"] = "Tissus", ["pieces"] = "pièces" },
            ProductMedia = new Dictionary<string, IReadOnlyList<string>> { [Product.Id] = [a, b] }
        };
        var payload = JsonSerializer.SerializeToNode(Payload([Collection() with { Web = web }]), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        payload["products"]![0]!["images"] = JsonSerializer.SerializeToNode(new[]
        {
            new { url = "/model.webp", alt = "Model", sha256 = a, provenance = new { approvedForPublication = true, kind = "photograph" } },
            new { url = "/unapproved.webp", alt = "Unapproved", sha256 = b, provenance = new { approvedForPublication = false, kind = "photograph" } }
        });
        payload["products"]![0]!["pricing"] = JsonSerializer.SerializeToNode(new { currency = "UZS", tiers = new object[]
        {
            new { minimumQuantity = 1, maximumQuantity = (int?)4, unitPrice = 600000 },
            new { minimumQuantity = 5, maximumQuantity = (int?)null, unitPrice = 450000 }
        } });
        var result = await Reader(payload).GetCatalogueAsync(default);
        var product = Assert.Single(result.Products); var collection = Assert.Single(result.Catalogues!);
        Assert.Equal(a, Assert.Single(collection.Web!.ProductMedia![Product.Id]));
        Assert.Equal(2, product.Pricing!.Tiers.Count);
        Assert.False(collection.Web.PriceDisplay!.ShowRetail);
        Assert.Equal("Tissus", collection.Web.Copy!["materials"]);
    }

    [Fact]
    public async Task Reader_honors_empty_media_selection_hidden_prices_and_localized_copy()
    {
        var web = Collection().Web! with
        {
            MediaGroup = "fabric", PriceDisplay = new(false),
            Copy = new Dictionary<string, string> { ["header"] = "Atelier <Ocharo>", ["footer"] = "Merci", ["materials"] = "Tissus", ["sizes"] = "Tailles" },
            ProductMedia = new Dictionary<string, IReadOnlyList<string>> { [Product.Id] = [] }
        };
        await using var harness = Harness(new Feed([Collection() with { Web = web }]));
        var html = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/");
        Assert.DoesNotContain("600,000", html);
        Assert.DoesNotContain("nimcha.webp", html); Assert.DoesNotContain("nimcha-back.webp", html);
        Assert.Contains("Images are being prepared", html);
        Assert.Contains("Atelier &lt;Ocharo&gt;", html); Assert.Contains("Merci", html);
        Assert.Contains("Tissus", html); Assert.Contains("Tailles", html);
    }

    [Fact]
    public async Task Reader_filters_retail_and_wholesale_independently_and_preserves_selected_media_order()
    {
        var web = Collection().Web! with
        {
            PriceDisplay = new(true, false, true, false, true),
            Copy = new Dictionary<string, string> { ["pieces"] = "pièces" },
            ProductMedia = new Dictionary<string, IReadOnlyList<string>> { [Product.Id] = [new string('b',64), new string('a',64)] }
        };
        var feed = new Feed([Collection() with { Web = web }]) { Piece = Product with { Pricing = new("UZS", [new(1,4,600000), new(5,null,450000)]) } };
        await using var harness = Harness(feed);
        var html = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/");
        Assert.DoesNotContain("600,000", html); Assert.DoesNotContain("UZS", html);
        Assert.Contains("450,000", html); Assert.Contains("5+ pièces", WebUtility.HtmlDecode(html));
        Assert.True(html.IndexOf("nimcha-back.webp", StringComparison.Ordinal) < html.IndexOf("nimcha.webp", StringComparison.Ordinal));
        feed.Catalogues = [Collection() with { Web = web with { PriceDisplay = new(true, true, false, true, false) } }];
        html = await harness.Client().GetStringAsync("/catalogue/collections/woven-days/");
        Assert.Contains("600,000 UZS", html); Assert.DoesNotContain("450,000", html); Assert.DoesNotContain("pièces", WebUtility.HtmlDecode(html));
    }

    private static object Payload(IReadOnlyList<CatalogueCollection> catalogues) => new
    {
        schemaVersion = 1, release = new { id = Release }, availableLocales = new[] { "en" }, catalogues,
        products = new[] { new { id = Product.Id, title = Product.Name, images = Array.Empty<object>() } }
    };
    private static CatalogueFeed Reader(object payload) => new(new HttpClient(new Handler(payload)),
        Options.Create(new BlogOptions { PlatformBaseUrl = "https://platform.example", BrandSlug = "ocharo" }), NullLogger<CatalogueFeed>.Instance);
    private static BlogHarness Harness(Feed feed) => new(seed: false, services: services => services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(feed)));
    private sealed class Handler(object payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(payload) });
    }
    private sealed class Feed(IReadOnlyList<CatalogueCollection> catalogues) : ICatalogueFeed
    {
        public IReadOnlyList<CatalogueCollection> Catalogues { get; set; } = catalogues;
        public CatalogueProduct Piece { get; set; } = Product;
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<CatalogueProduct>>([Piece]);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") =>
            Task.FromResult(new CatalogueSnapshot([Piece], null, [locale], Catalogues));
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) => Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(url));
    }
}
