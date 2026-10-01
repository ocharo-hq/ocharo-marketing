using System.Globalization;
using System.Text;
using System.Text.Json;
using OcharoBlog.Application.Blog.Models;

namespace OcharoBlog.Application.Blog.Site;

public sealed partial class PublicationSiteService
{
    private sealed record CatalogueDiscoveryState(string Search, string Category, string Fabric, string Size, string Sort);

    private static string DiscoveryValue(IReadOnlyDictionary<string, string> query, string key)
    {
        if (!query.TryGetValue(key, out var value)) return "";
        var trimmed = value.Trim();
        var length = Math.Min(trimmed.Length, 160);
        if (length > 0 && char.IsHighSurrogate(trimmed[length - 1])) length--;
        return trimmed[..length];
    }

    private static string SearchText(string value) => value.Normalize(NormalizationForm.FormKC).ToLowerInvariant()
        .Replace('’', '\'').Replace('‘', '\'').Replace('ʻ', '\'').Replace('ʼ', '\'');

    private static string ProductSearch(CatalogueProduct product) => string.Join(" ",
        product.Id, product.Name, product.GarmentName, product.Fabric, product.Category,
        string.Join(" ", product.Sizes ?? []));

    private static decimal? SortPrice(CatalogueProduct product) =>
        product.PriceAmount is not null && !string.IsNullOrWhiteSpace(product.Currency) ? product.PriceAmount : product.RetailPrice;

    private static string? SortCurrency(CatalogueProduct product) => SortPrice(product) is null ? null
        : product.PriceAmount is not null && !string.IsNullOrWhiteSpace(product.Currency) ? product.Currency : "UZS";

    private static string? DiscoveryCurrency(Model model)
    {
        var currencies = model.Products.Select(SortCurrency).Where(value => value is not null).Distinct(StringComparer.Ordinal).ToList();
        return currencies.Count == 1 ? currencies[0] : null;
    }

    private static CatalogueDiscoveryState DiscoveryState(Model model, IReadOnlyDictionary<string, string> query)
    {
        var sort = DiscoveryValue(query, "sort");
        if (sort is not ("name" or "price-asc" or "price-desc") || sort != "name" && DiscoveryCurrency(model) is null) sort = "";
        return new(DiscoveryValue(query, "q"), DiscoveryValue(query, "category"), DiscoveryValue(query, "fabric"), DiscoveryValue(query, "size"), sort);
    }

    private static bool IsDiscoveryMatch(CatalogueProduct product, CatalogueDiscoveryState state)
    {
        var search = SearchText(ProductSearch(product));
        return SearchText(state.Search).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).All(search.Contains)
            && (state.Category.Length == 0 || product.Category == state.Category)
            && (state.Fabric.Length == 0 || product.Fabric == state.Fabric)
            && (state.Size.Length == 0 || product.Sizes?.Contains(state.Size) == true);
    }

    private static string DiscoveryOptions(IEnumerable<string?> values, string selected, string all)
    {
        var options = values.Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal).ToList();
        if (selected.Length > 0 && !options.Contains(selected)) options.Add(selected);
        return $"<option value=\"\">{E(all)}</option>" + Join(options.Select(value =>
            $"<option value=\"{E(value)}\"{(value == selected ? " selected" : "")}>{E(value)}</option>"));
    }

    private static string CatalogueDiscovery(Model model, IReadOnlyDictionary<string, string> query)
    {
        var state = DiscoveryState(model, query);
        var currency = DiscoveryCurrency(model);
        var matches = model.Products.Count(product => IsDiscoveryMatch(product, state));
        var products = model.Products.Select((product, index) => (Product: product, Index: index));
        products = state.Sort switch
        {
            "name" => products.OrderBy(item => item.Product.Name, StringComparer.Create(CultureInfo.GetCultureInfo(model.Locale), true)).ThenBy(item => item.Index),
            "price-asc" => products.OrderBy(item => SortPrice(item.Product) is null).ThenBy(item => SortPrice(item.Product)).ThenBy(item => item.Index),
            "price-desc" => products.OrderBy(item => SortPrice(item.Product) is null).ThenByDescending(item => SortPrice(item.Product)).ThenBy(item => item.Index),
            _ => products,
        };
        string SortOption(string value, string label) => $"<option value=\"{value}\"{(state.Sort == value ? " selected" : "")}>{E(label)}</option>";
        var sorts = SortOption("", "Catalogue order") + SortOption("name", "Name, A–Z")
            + (currency is null ? "" : SortOption("price-asc", $"Price, low to high ({currency})") + SortOption("price-desc", $"Price, high to low ({currency})"));
        var cards = Join(products.Select(item => DiscoveryCard(item.Product, item.Index, !IsDiscoveryMatch(item.Product, state))));
        return $$"""
        <form class="filters catalogue-filters" data-catalogue-discovery method="get" action="/catalogue/#pieces" role="search" aria-label="Find catalogue pieces" data-price-currency="{{E(currency)}}">
        <input type="hidden" name="locale" value="{{model.Locale}}">
        <label>Find a piece<input type="search" name="q" value="{{E(state.Search)}}" maxlength="160" placeholder="Name, reference or material" aria-controls="catalogue-results"></label>
        <label>Category<select name="category" aria-controls="catalogue-results">{{DiscoveryOptions(model.Products.Select(product => product.Category), state.Category, "All categories")}}</select></label>
        <label>Material<select name="fabric" aria-controls="catalogue-results">{{DiscoveryOptions(model.Products.Select(product => product.Fabric), state.Fabric, "All materials")}}</select></label>
        <label>Size<select name="size" aria-controls="catalogue-results">{{DiscoveryOptions(model.Products.SelectMany(product => product.Sizes ?? []), state.Size, "All sizes")}}</select></label>
        <label>Sort<select name="sort" aria-controls="catalogue-results">{{sorts}}</select></label>
        <button type="submit" class="button small">Find pieces</button><a class="text-button" data-catalogue-reset href="/catalogue/#pieces">Clear filters</a></form>
        <p class="catalogue-filter-note">Sizes describe the collection. Confirm measurements and availability with Ocharo.{{(currency is null ? "" : " Prices on enquiry appear last when sorting by price.")}}</p>
        <p class="result-count" data-catalogue-count role="status" aria-live="polite" aria-atomic="true">{{matches}} of {{model.Products.Count}} pieces</p>
        <div id="catalogue-results" class="product-grid" data-catalogue-grid>{{cards}}</div>
        {{(model.Products.Count == 0 ? Empty("The collection is being prepared", "Explore the stories or contact Ocharo through the channels below.") : $"<section class=\"empty filter-empty\" data-catalogue-empty{(matches > 0 ? " hidden" : "")}><h3>No pieces match these filters.</h3><p>Try fewer filters, another spelling or a product reference.</p><a href=\"/catalogue/#pieces\" data-catalogue-reset>Show all pieces →</a></section>")}}
        """;
    }

    private static string DiscoveryCard(CatalogueProduct product, int index, bool hidden)
    {
        var attributes = $" data-catalogue-item data-order=\"{index}\" data-name=\"{E(product.Name)}\" data-category=\"{E(product.Category)}\" data-fabric=\"{E(product.Fabric)}\" data-sizes=\"{E(JsonSerializer.Serialize(product.Sizes ?? []))}\" data-price=\"{SortPrice(product)?.ToString(CultureInfo.InvariantCulture)}\"{(hidden ? " hidden" : "")}";
        return ProductCard(product).Replace("<article ", "<article" + attributes + " ", StringComparison.Ordinal)
            .Replace($"data-search=\"{E(product.Name + " " + product.Fabric + " " + product.Category)}\"", $"data-search=\"{E(ProductSearch(product))}\"", StringComparison.Ordinal);
    }

    private static string ProductEditionLinks(Model model, CatalogueProduct product)
    {
        if (model.Edition is not { } edition) return "";
        var pages = edition.Pages.Where(page => page.ProductIds.Contains(product.Id)).ToList();
        return pages.Count == 0 ? "" : "<aside class=\"product-edition-links\"><h2>In the catalogue.</h2><p>See this piece in the original page layout.</p><ul>"
            + Join(pages.Select(page => $"<li><a href=\"{EditionPagePath(edition, page)}\">Page {page.Number} · {E(page.Title)} →</a></li>")) + "</ul></aside>";
    }
}
