using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Tests;

public sealed class CatalogueDiscoveryTests
{
    private static readonly CatalogueProduct First = new("nimcha-v01", "Nimcha", "Vest", "Cotton", 300000, null, null,
        Slug: "nimcha", Category: "Vests", Sizes: ["S", "M", "XXXL"]);
    private static readonly CatalogueProduct Second = new("abaya-v01", "Abaya", "Abaya", "Silk", null, null, null,
        Slug: "abaya", Category: "Dresses", Sizes: ["M", "L"], PriceAmount: 900000, Currency: "UZS");
    private static readonly CatalogueProduct Unknown = new("coat-v01", "Coat", "Coat", null, null, null, null,
        Slug: "coat", Category: "Coats", Sizes: []);
    private static readonly CatalogueEdition Edition = new("released-edition", "brand", "design", 7, "Ocharo", "en", "template", 2,
        DateTimeOffset.Parse("2026-09-29T10:00:00Z"), "/released/catalogue.pdf?release=known",
        [new("page-01", 1, "Nimcha and Abaya", 210, 297, [First.Id, Second.Id], "/released/1.png", "/released/1.pdf?release=known", "/released/1.png"),
         new("page-02", 2, "Nimcha in detail", 210, 297, [First.Id], "/released/2.png", "/released/2.pdf?release=known", "/released/2.png")]);

    [Fact]
    public async Task Discovery_uses_released_facts_and_keeps_source_order_by_default()
    {
        await using var harness = Harness(); var html = await harness.Client().GetStringAsync("/catalogue/");
        Assert.Equal(["Nimcha", "Abaya", "Coat"], VisibleNames(html));
        Assert.Contains("name=\"category\"", html); Assert.Contains("name=\"fabric\"", html); Assert.Contains("name=\"size\"", html);
        Assert.Contains("<option value=\"XXXL\">XXXL</option>", html);
        Assert.DoesNotContain("<option value=\"XXL\">", html);
        Assert.Contains("Confirm measurements and availability", html);
        Assert.Contains("data-price=\"\"", html);
    }

    [Fact]
    public async Task Server_filters_intersect_reference_category_material_and_size_without_javascript()
    {
        await using var harness = Harness(); var html = await harness.Client().GetStringAsync("/catalogue/?q=nimcha%20v01&category=Vests&fabric=Cotton&size=M&locale=uz");
        Assert.Equal(["Nimcha"], VisibleNames(html)); Assert.Contains("1 of 3 pieces", html);
        Assert.Contains("name=\"q\" value=\"nimcha v01\"", html);
        Assert.Contains("<option value=\"M\" selected>", html);
        Assert.Contains("href=\"/catalogue/?locale=uz#pieces\"", html);
    }

    [Fact]
    public async Task Unknown_filter_has_empty_state_and_remains_visible_for_clear_action()
    {
        await using var harness = Harness(); var html = await harness.Client().GetStringAsync("/catalogue/?size=XXL");
        Assert.Empty(VisibleNames(html)); Assert.Contains("0 of 3 pieces", html);
        Assert.Contains("<option value=\"XXL\" selected>XXL</option>", html);
        Assert.Contains("data-catalogue-empty><h3>No pieces match", html);
        Assert.Contains("Show all pieces", html);
    }

    [Theory]
    [InlineData("price-asc", "Nimcha", "Abaya", "Coat")]
    [InlineData("price-desc", "Abaya", "Nimcha", "Coat")]
    [InlineData("name", "Abaya", "Coat", "Nimcha")]
    [InlineData("invalid", "Nimcha", "Abaya", "Coat")]
    public async Task Sorting_preserves_unknown_prices_as_unknown(string sort, string one, string two, string three)
    {
        await using var harness = Harness(); var html = await harness.Client().GetStringAsync("/catalogue/?sort=" + sort);
        Assert.Equal([one, two, three], VisibleNames(html)); Assert.Contains("Price on enquiry", html);
    }

    [Fact]
    public async Task Different_currencies_are_not_compared_as_if_they_were_exchange_rates()
    {
        await using var harness = Harness([First, Second with { Currency = "USD" }, Unknown]);
        var html = await harness.Client().GetStringAsync("/catalogue/?sort=price-asc");
        Assert.Equal(["Nimcha", "Abaya", "Coat"], VisibleNames(html));
        Assert.DoesNotContain("<option value=\"price-asc\"", html);
    }

    [Fact]
    public async Task Hostile_search_and_filter_are_encoded_in_attributes_and_option_text()
    {
        await using var harness = Harness();
        var value = Uri.EscapeDataString("\"><script>alert(1)</script>");
        var html = await harness.Client().GetStringAsync($"/catalogue/?q={value}&fabric={value}");
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.Empty(VisibleNames(html));
    }

    [Fact]
    public async Task Search_length_limit_does_not_split_a_unicode_character()
    {
        await using var harness = Harness();
        var value = new string('a', 159) + "🧵";
        var response = await harness.Client().GetAsync("/catalogue/?q=" + Uri.EscapeDataString(value));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("0 of 3 pieces", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Product_links_to_every_exact_page_without_inventing_matches()
    {
        await using var harness = Harness(); var client = harness.Client();
        var html = await client.GetStringAsync("/catalogue/pieces/nimcha/?locale=ru");
        Assert.Contains("/catalogue/editions/released-edition/pages/page-01/?locale=ru", html);
        Assert.Contains("/catalogue/editions/released-edition/pages/page-02/?locale=ru", html);
        Assert.DoesNotContain("In the catalogue.", await client.GetStringAsync("/catalogue/pieces/coat/"));
    }

    [Fact]
    public async Task Empty_feed_preserves_preparation_state_instead_of_suggesting_existing_stock()
    {
        await using var harness = Harness([]); var html = await harness.Client().GetStringAsync("/catalogue/");
        Assert.Contains("The collection is being prepared", html);
        Assert.DoesNotContain("data-catalogue-empty", html);
    }

    [Fact]
    public async Task Static_export_contains_all_discovery_data_script_and_local_page_backlinks()
    {
        await using var harness = Harness();
        using var zip = new ZipArchive(new MemoryStream(await harness.Client().GetByteArrayAsync("/api/blog/export/catalogue.zip?locale=uz")));
        using var homeReader = new StreamReader(zip.GetEntry("index.html")!.Open()); var home = await homeReader.ReadToEndAsync();
        Assert.Equal(3, VisibleNames(home).Length); Assert.Contains("data-catalogue-discovery", home);
        Assert.Contains("data-order=\"0\"", home); Assert.Contains("data-sizes=\"", home);
        using var productReader = new StreamReader(zip.GetEntry("catalogue/pieces/nimcha/index.html")!.Open());
        Assert.Contains("/catalogue/editions/released-edition/pages/page-01/?locale=uz", await productReader.ReadToEndAsync());
        using var scriptReader = new StreamReader(zip.GetEntry("catalogue/site.js")!.Open());
        Assert.Contains("data-catalogue-discovery", await scriptReader.ReadToEndAsync());
    }

    private static string[] VisibleNames(string html) => Regex.Matches(html, "<article(?<attributes>[^>]*data-catalogue-item[^>]*)>")
        .Where(match => !Regex.IsMatch(match.Groups["attributes"].Value, @"(?:^|\s)hidden(?:\s|$)"))
        .Select(match => WebUtility.HtmlDecode(Regex.Match(match.Groups["attributes"].Value, "data-name=\"([^\"]*)\"").Groups[1].Value)).ToArray();

    private static BlogHarness Harness(IReadOnlyList<CatalogueProduct>? products = null) => new(seed: false, services: services =>
        services.Replace(ServiceDescriptor.Singleton<ICatalogueFeed>(new Feed(products ?? [First, Second, Unknown]))));

    private sealed class Feed(IReadOnlyList<CatalogueProduct> products) : ICatalogueFeed
    {
        public Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) => Task.FromResult(products);
        public Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en") => Task.FromResult(new CatalogueSnapshot(products, products.Count == 0 ? null : Edition with { Locale = locale }));
        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct) => Task.FromResult<byte[]?>(Encoding.UTF8.GetBytes(url));
    }
}
