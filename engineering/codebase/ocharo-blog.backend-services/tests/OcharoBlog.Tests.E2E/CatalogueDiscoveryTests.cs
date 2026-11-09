using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Tests.E2E;

public sealed class CatalogueDiscoveryTests
{
    private static readonly CatalogueProduct First = new("nimcha-v01", "Nimcha", "Vest", "Cotton", 300000, null, null,
        Slug: "nimcha", Category: "Vests", Sizes: ["S", "M", "XXXL"]);
    private static readonly CatalogueProduct Second = new("abaya-v01", "Abaya", "Abaya", "Silk", null, null, null,
        Slug: "abaya", Category: "Dresses", Sizes: ["M", "L"], PriceAmount: 900000, Currency: "UZS");
    private static readonly CatalogueProduct Unknown = new("coat-v01", "Coat", "Coat", null, null, null, null,
        Slug: "coat", Category: "Coats", Sizes: []);
    private static readonly CatalogueEdition Edition =
        new("released-edition", "brand", "design", 7, "Ocharo", "en", "template", 2,
        DateTimeOffset.Parse("2026-09-29T10:00:00Z"), "/released/catalogue.pdf?release=known",
        [new(
            "page-01",
            1,
            "Nimcha and Abaya",
            210,
            297,
            [First.Id, Second.Id],
            "/released/1.png",
            "/released/1.pdf?release=known",
            "/released/1.png"),
         new(
             "page-02",
             2,
             "Nimcha in detail",
             210,
             297,
             [First.Id],
             "/released/2.png",
             "/released/2.pdf?release=known",
             "/released/2.png")]);

    [Fact]
    public async Task Discovery_ShouldUseReleasedFactsInSourceOrder_WhenNoSortIsChosen()
    {
        await using var harness = Harness(); var html = await harness.Client().GetStringAsync("/catalogue/");
        VisibleNames(html).Should().Equal(["Nimcha", "Abaya", "Coat"]);
        html.Should().Contain("name=\"category\"");
        html.Should().Contain("name=\"fabric\"");
        html.Should().Contain("name=\"size\"");
        html.Should().Contain("<option value=\"XXXL\">XXXL</option>");
        html.Should().NotContain("<option value=\"XXL\">");
        html.Should().Contain("Confirm measurements and availability");
        html.Should().Contain("data-price=\"\"");
    }

    [Fact]
    public async Task Discovery_ShouldIntersectEveryFilterOnTheServer_WhenSeveralAreChosen()
    {
        await using var harness = Harness();
        var html =
            await harness.Client()
                .GetStringAsync("/catalogue/?q=nimcha%20v01&category=Vests&fabric=Cotton&size=M&locale=uz");
        VisibleNames(html).Should().Equal(["Nimcha"]); html.Should().Contain("1 of 3 pieces");
        html.Should().Contain("name=\"q\" value=\"nimcha v01\"");
        html.Should().Contain("<option value=\"M\" selected>");
        html.Should().Contain("href=\"/catalogue/?locale=uz#pieces\"");
    }

    [Fact]
    public async Task Discovery_ShouldShowAnEmptyStateWithTheFilter_WhenTheFilterIsUnknown()
    {
        await using var harness = Harness(); var html = await harness.Client().GetStringAsync("/catalogue/?size=XXL");
        VisibleNames(html).Should().BeEmpty(); html.Should().Contain("0 of 3 pieces");
        html.Should().Contain("<option value=\"XXL\" selected>XXL</option>");
        html.Should().Contain("data-catalogue-empty><h3>No pieces match");
        html.Should().Contain("Show all pieces");
    }

    [Theory]
    [InlineData("price-asc", "Nimcha", "Abaya", "Coat")]
    [InlineData("price-desc", "Abaya", "Nimcha", "Coat")]
    [InlineData("name", "Abaya", "Coat", "Nimcha")]
    [InlineData("invalid", "Nimcha", "Abaya", "Coat")]
    public async Task Sorting_ShouldKeepUnknownPricesUnknown_WhenSortingByPrice(
        string sort,
        string one,
        string two,
        string three)
    {
        await using var harness = Harness();
        var html = await harness.Client().GetStringAsync("/catalogue/?sort=" + sort);
        VisibleNames(html).Should().Equal([one, two, three]); html.Should().Contain("Price on enquiry");
    }

    [Fact]
    public async Task Sorting_ShouldNotCompareAcrossCurrencies_WhenPricesDifferInCurrency()
    {
        await using var harness = Harness([First, Second with { Currency = "USD" }, Unknown]);
        var html = await harness.Client().GetStringAsync("/catalogue/?sort=price-asc");
        VisibleNames(html).Should().Equal(["Nimcha", "Abaya", "Coat"]);
        html.Should().NotContain("<option value=\"price-asc\"");
    }

    [Fact]
    public async Task Discovery_ShouldEncodeSearchAndFilters_WhenTheyCarryMarkup()
    {
        await using var harness = Harness();
        var value = Uri.EscapeDataString("\"><script>alert(1)</script>");
        var html = await harness.Client().GetStringAsync($"/catalogue/?q={value}&fabric={value}");
        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
        VisibleNames(html).Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ShouldKeepWholeCharacters_WhenTheQueryIsCutToItsLimit()
    {
        await using var harness = Harness();
        var value = new string('a', 159) + "🧵";
        var response = await harness.Client().GetAsync("/catalogue/?q=" + Uri.EscapeDataString(value));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("0 of 3 pieces");
    }

    [Fact]
    public async Task Product_ShouldLinkOnlyItsExactPages_WhenEditionsNameIt()
    {
        await using var harness = Harness(); var client = harness.Client();
        var html = await client.GetStringAsync("/catalogue/pieces/nimcha/?locale=ru");
        html.Should().Contain("/catalogue/editions/released-edition/pages/page-01/?locale=ru");
        html.Should().Contain("/catalogue/editions/released-edition/pages/page-02/?locale=ru");
        (await client.GetStringAsync("/catalogue/pieces/coat/")).Should().NotContain("In the catalogue.");
    }

    [Fact]
    public async Task Discovery_ShouldShowThePreparationState_WhenTheFeedIsEmpty()
    {
        await using var harness = Harness([]); var html = await harness.Client().GetStringAsync("/catalogue/");
        html.Should().Contain("The collection is being prepared");
        html.Should().NotContain("data-catalogue-empty");
    }

    [Fact]
    public async Task CatalogueExport_ShouldHoldDiscoveryDataAndLocalBacklinks_WhenExported()
    {
        await using var harness = Harness();
        using var zip =
            new ZipArchive(new MemoryStream(await harness.Client()
                .GetByteArrayAsync("/api/blog/export/catalogue.zip?locale=uz")));
        using var homeReader = new StreamReader(zip.GetEntry("index.html")!.Open());
        var home = await homeReader.ReadToEndAsync();
        VisibleNames(home).Length.Should().Be(3); home.Should().Contain("data-catalogue-discovery");
        home.Should().Contain("data-order=\"0\""); home.Should().Contain("data-sizes=\"");
        using var productReader = new StreamReader(zip.GetEntry("catalogue/pieces/nimcha/index.html")!.Open());
        (await productReader.ReadToEndAsync())
            .Should()
            .Contain("/catalogue/editions/released-edition/pages/page-01/?locale=uz");
        using var scriptReader = new StreamReader(zip.GetEntry("catalogue/site.js")!.Open());
        (await scriptReader.ReadToEndAsync()).Should().Contain("data-catalogue-discovery");
    }

    private static string[] VisibleNames(string html) =>
        Regex.Matches(html, "<article(?<attributes>[^>]*data-catalogue-item[^>]*)>")
        .Where(match => !Regex.IsMatch(match.Groups["attributes"].Value, @"(?:^|\s)hidden(?:\s|$)"))
        .Select(match =>
            WebUtility.HtmlDecode(Regex.Match(match.Groups["attributes"].Value, "data-name=\"([^\"]*)\"")
                .Groups[1]
                .Value))
            .ToArray();

    private static BlogHarness Harness(IReadOnlyList<CatalogueProduct>? products = null) =>
        new(seed: false, services: services =>
        services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(new Feed(products ?? [First, Second, Unknown]))));

    private sealed class Feed(IReadOnlyList<CatalogueProduct> products) : ICatalogueFeed
    {
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) =>
            Task.FromResult(products);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") =>
            Task.FromResult(new CatalogueSnapshot(
                products,
                products.Count == 0 ? null : Edition with { Locale = locale }));
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) =>
            Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(url));
    }
}
