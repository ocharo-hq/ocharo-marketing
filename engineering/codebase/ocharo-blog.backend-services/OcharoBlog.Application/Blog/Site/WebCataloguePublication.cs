using System.Globalization;
using OcharoBlog.Application.Blog.Models;

namespace OcharoBlog.Application.Blog.Site;

public sealed partial class PublicationSiteService
{
    private static string CollectionPath(CatalogueCollection collection) => $"/catalogue/collections/{Uri.EscapeDataString(collection.Slug)}/";
    private static string CollectionPagePath(CatalogueCollection collection, CatalogueWebPage page) => CollectionPath(collection) + $"pages/{Uri.EscapeDataString(page.Id)}/";
    private static IEnumerable<CatalogueEdition> Editions(Model model) =>
        (model.Edition is { } legacy ? new[] { legacy } : []).Concat((model.Catalogues ?? []).Where(item => item.Pdf is not null).Select(item => item.Pdf!)).DistinctBy(edition => edition.Id);
    private static CatalogueEdition? FindEdition(Model model, string id) => Editions(model).FirstOrDefault(edition => edition.Id == id);

    private static IReadOnlyList<CatalogueCollection> VisibleCatalogues(CatalogueSnapshot snapshot, string locale)
    {
        var ids = snapshot.Products.Select(product => product.Id).ToHashSet(StringComparer.Ordinal);
        return (snapshot.Catalogues ?? []).Where(item => item.Locale == locale).Select(item =>
        {
            var pages = (item.Web?.Pages ?? []).Select(page => page with
                { ProductIds = page.ProductIds.Where(ids.Contains).Distinct(StringComparer.Ordinal).ToList() })
                .Where(page => page.ProductIds.Count > 0).ToList();
            return item with { Web = pages.Count > 0 ? item.Web! with { Pages = pages } : null,
                Pdf = item.Pdf?.Locale == locale ? item.Pdf : null };
        }).Where(item => item.Web is not null || item.Pdf is not null).ToList();
    }

    private static string CatalogueGallery(Model m, IReadOnlyDictionary<string, string> query) =>
        Start(m, "catalogue", "The Ocharo catalogues", "Explore Ocharo collections online or keep an available PDF edition.", "/catalogue/") + $$"""
        <main id="main"><header class="catalogue-hero"><p class="eyebrow">Ocharo collections</p><h1>Find your next piece.</h1><p class="deck">Browse a collection at your own pace. Explore online or keep a PDF where available.</p></header>
        <section id="catalogues" class="section catalogue-library" aria-label="Catalogues"><div class="catalogue-library-grid">{{Join((m.Catalogues ?? []).Select(collection => CollectionCard(m, collection)))}}</div></section>
        <section id="pieces" class="section catalogue-pieces"><div class="section-heading"><h2>All pieces.</h2></div>{{CatalogueDiscovery(m, query)}}</section></main>
        """ + End(m, "catalogue");

    private static string CollectionCard(Model m, CatalogueCollection collection)
    {
        var firstProduct = collection.Web?.Pages.SelectMany(page => page.ProductIds).Select(id => m.Products.FirstOrDefault(product => product.Id == id)).FirstOrDefault(product => product is not null && SelectedImages(product, collection.Web).Count > 0);
        var image = firstProduct is null ? collection.Pdf?.Pages.FirstOrDefault()?.ImageUrl : SelectedImages(firstProduct, collection.Web!)[0].Url;
        var cover = image is null ? $"<div class=\"collection-title-cover\" {ContentLanguage(m)}>{E(collection.Title)}</div>"
            : $"<img src=\"{E(image)}\" alt=\"{E(firstProduct?.ImageAlt ?? collection.Title)}\" loading=\"lazy\">";
        return $$"""
        <article class="collection-card"><a class="collection-cover" href="{{CollectionPath(collection)}}">{{cover}}</a><div class="collection-copy"><h2 {{ContentLanguage(m)}}><a href="{{CollectionPath(collection)}}">{{E(collection.Title)}}</a></h2><p {{ContentLanguage(m)}}>{{E(collection.Description)}}</p><div class="catalogue-actions">{{(collection.Web is null ? "" : $"<a class=\"button small\" href=\"{CollectionPath(collection)}\">Explore online →</a>")}}{{(collection.Pdf is null ? "" : $"<a class=\"catalogue-download\" href=\"{E(Download(collection.Pdf.PdfUrl))}\" download=\"ocharo-{E(collection.Slug)}.pdf\">Download PDF ↓</a>")}}</div></div></article>
        """;
    }

    private static string WebCatalogue(Model model, CatalogueCollection collection, CatalogueWebPage? selected = null)
    {
        var m = model with { AvailableLocales = collection.AvailableLocales };
        var pages = selected is null ? collection.Web?.Pages ?? [] : new[] { selected };
        var path = selected is null ? CollectionPath(collection) : CollectionPagePath(collection, selected);
        var title = selected?.Title ?? collection.Title;
        var navigation = collection.Web is { Pages.Count: > 1 } web
            ? "<nav class=\"web-page-nav\" aria-label=\"Collection sections\">" + Join(web.Pages.Select(page => $"<a href=\"{CollectionPagePath(collection, page)}\"{(selected?.Id == page.Id ? " aria-current=\"page\"" : "")}>{E(page.Title)}</a>")) + "</nav>" : "";
        return Start(m, "catalogue", title, collection.Description ?? "Explore the Ocharo collection.", path) + $$"""
        <main id="main" class="web-catalogue web-theme-{{collection.Web?.Theme ?? "editorial"}}"><header class="section web-catalogue-heading"><a class="back" href="{{(selected is null ? "/catalogue/#catalogues" : CollectionPath(collection))}}">← {{(selected is null ? "All catalogues" : "Complete collection")}}</a><p class="eyebrow" {{ContentLanguage(m)}}>{{E(selected is null ? Copy(collection.Web, "header", "Ocharo") : collection.Title)}} · {{E(Copy(collection.Web, "collection", "Collection"))}}</p><h1 {{ContentLanguage(m)}}>{{E(title)}}</h1><p class="deck" {{ContentLanguage(m)}}>{{E(collection.Description)}}</p><p class="web-cover-note" {{ContentLanguage(m)}}>{{E(Copy(collection.Web, "coverNote", ""))}}</p><div class="catalogue-actions">{{(collection.Pdf is null ? "" : $"<a class=\"button small\" href=\"{E(Download(collection.Pdf.PdfUrl))}\" download=\"ocharo-{E(collection.Slug)}.pdf\">Download PDF ↓</a><a class=\"catalogue-download\" href=\"{EditionPath(collection.Pdf)}\">View PDF pages →</a>")}}<button class="text-button" data-share-page>Copy collection link ↗</button><span class="copy-status" role="status"></span></div>{{navigation}}</header>
        {{Join(pages.Select(page => WebPage(m, collection, page)))}}
        <footer class="section web-catalogue-footer" {{ContentLanguage(m)}}>{{E(Copy(collection.Web, "footer", ""))}}</footer>
        </main>
        """ + End(m, "catalogue");
    }

    private static string WebPage(Model m, CatalogueCollection collection, CatalogueWebPage page)
    {
        var products = page.ProductIds.Select(id => m.Products.FirstOrDefault(product => product.Id == id)).Where(product => product is not null).Cast<CatalogueProduct>().ToList();
        if (products.Count == 0) return "";
        var layout = page.Layout is "grid" or "collage" ? page.Layout : "spread";
        var content = Join(products.Select(product => WebProduct(m, product, collection.Web!, layout)));
        var body = layout == "grid" ? "<div class=\"product-grid\">" + content + "</div>" : content;
        return $"<section class=\"section web-section web-layout-{layout}\" id=\"section-{E(page.Id)}\"><div class=\"section-heading\"><h2 {ContentLanguage(m)}>{E(page.Title)}</h2><a href=\"{CollectionPagePath(collection, page)}\">Open section ↗</a></div>{body}</section>";
    }

    private static string Copy(CatalogueWeb? web, string key, string fallback) => web?.Copy?.GetValueOrDefault(key) ?? fallback;
    private static IReadOnlyList<CatalogueImage> SelectedImages(CatalogueProduct product, CatalogueWeb web)
    {
        var images = product.Images is { Count: > 0 } ? product.Images : product.ImageUrl is { } image
            ? new[] { new CatalogueImage(image, product.ImageAlt ?? product.Name, product.ImageKind, product.ImageCredit, product.ImageRights) } : [];
        // Explicit empty selections stay empty; no accidental fallback into another media group.
        if (web.ProductMedia is { } selected)
            return (selected.GetValueOrDefault(product.Id) ?? []).Select(hash => images.FirstOrDefault(image => image.Sha256 == hash))
                .Where(image => image is not null).Cast<CatalogueImage>().ToList();
        return web.MediaGroup == "all" ? images : [];
    }
    private static string WebPrices(Model model, CatalogueProduct product, CatalogueWeb web)
    {
        var display = web.PriceDisplay ?? new();
        if (!display.Show) return "";
        var pricing = product.Pricing;
        var tiers = pricing?.Tiers ?? (product.PriceAmount is { } amount ? new[] { new CataloguePriceTier(1, null, amount) }
            : product.RetailPrice is { } retail ? new[] { new CataloguePriceTier(1, null, retail) } : []);
        if (tiers.Count == 0) return $"<p class=\"price\" {ContentLanguage(model)}>{E(Copy(web, "priceOnRequest", "Price on enquiry"))}</p>";
        var culture = CultureInfo.GetCultureInfo(model.Locale);
        return "<div class=\"web-prices\">" + Join(tiers.Where(tier => tier.MinimumQuantity == 1 ? display.ShowRetail : display.ShowWholesale).Select(tier =>
        {
            var price = tier.UnitPrice.ToString("N0", culture) + (display.ShowCurrency ? " " + (pricing?.Currency ?? product.Currency ?? "UZS") : "");
            var quantity = tier.MinimumQuantity.ToString(culture) + (tier.MaximumQuantity is null ? "+" : tier.MaximumQuantity == tier.MinimumQuantity ? "" : "–" + tier.MaximumQuantity.Value.ToString(culture));
            return $"<p class=\"price\"><b>{E(price)}</b>{(display.ShowQuantity ? $"<span {ContentLanguage(model)}> · {E(quantity)} {E(Copy(web, "pieces", "pieces"))}</span>" : "")}</p>";
        })) + "</div>";
    }
    private static string WebProduct(Model m, CatalogueProduct product, CatalogueWeb web, string layout)
    {
        var images = SelectedImages(product, web);
        var media = images.Count == 0 ? "<div class=\"web-image-empty\">Images are being prepared.</div>"
            : Join(images.Take(layout == "grid" ? 1 : layout == "collage" ? 6 : 3).Select(item => $"<figure class=\"web-product-photo\"><a href=\"{ProductPath(product)}\"><img src=\"{E(item.Url)}\" alt=\"{E(item.Alt)}\" loading=\"lazy\"></a>{(item.Kind is "visualization" or "creative-visualization" || !string.IsNullOrWhiteSpace(item.Credit) ? $"<figcaption>{(item.Kind is "visualization" or "creative-visualization" ? "Creative visualization · " : "")}{E(item.Credit)}</figcaption>" : "")}</figure>"));
        return $$"""
        <article class="web-product"><div class="web-product-media{{(layout == "collage" ? " web-collage" : "")}}">{{media}}</div><div class="web-product-copy"><h3 {{ContentLanguage(m)}}>{{E(product.Name)}}</h3>{{(layout == "grid" ? "" : $"<p class=\"deck\" {ContentLanguage(m)}>{E(product.Description)}</p>")}}<dl class="facts"><div><dt {{ContentLanguage(m)}}>{{E(Copy(web, "materials", "Material"))}}</dt><dd {{ContentLanguage(m)}}>{{E(product.Fabric)}}</dd></div>{{(product.Sizes is { Count: > 0 } ? $"<div><dt {ContentLanguage(m)}>{E(Copy(web, "sizes", "Sizes"))}</dt><dd>{E(string.Join(", ", product.Sizes))}</dd></div>" : "")}}</dl>{{WebPrices(m, product, web)}}<a class="button" href="{{ProductPath(product)}}">Explore this piece →</a></div></article>
        """;
    }
}
