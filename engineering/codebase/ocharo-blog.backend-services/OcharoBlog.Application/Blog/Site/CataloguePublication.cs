using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using OcharoBlog.Application.Blog.Models;

namespace OcharoBlog.Application.Blog.Site;

public sealed partial class PublicationSiteService
{
    private static string EditionPath(CatalogueEdition edition) => $"/catalogue/editions/{Uri.EscapeDataString(edition.Id)}/";
    private static string EditionPagePath(CatalogueEdition edition, CatalogueEditionPage page) => EditionPath(edition) + $"pages/{Uri.EscapeDataString(page.Id)}/";
    private static string Download(string url) => url.StartsWith("/catalogue/files/", StringComparison.Ordinal) ? url : url + (url.Contains('?') ? "&" : "?") + "download=1";
    private static string Language(string locale) => locale switch { "uz" => "O‘zbekcha", "ru" => "Русский", "en" => "English", _ => locale };
    private static string EditionMeta(CatalogueEdition edition) => $"{edition.PageCount} pages · {E(Language(edition.Locale))}";

    private static string CatalogueLocaleUrl(string path, string locale)
    {
        if (path.Contains("locale=", StringComparison.Ordinal)) return path;
        var parts = path.Split('#', 2);
        return parts[0] + (parts[0].Contains('?') ? "&" : "?") + "locale=" + locale
            + (parts.Length == 2 ? "#" + parts[1] : "");
    }
    private static string LocalizeCatalogueLinks(string html, string locale) => Regex.Replace(html,
        "(?<attribute>href|action)=\"(?<url>/catalogue/[^\"<>]*)\"", match =>
            match.Groups["attribute"].Value + "=\"" + E(CatalogueLocaleUrl(WebUtility.HtmlDecode(match.Groups["url"].Value), locale)) + "\"");
    private static string CatalogueLanguages(Model m) => m.IsStaticExport
        ? $"<div class=\"catalogue-languages\"><span>Catalogue language</span><span lang=\"{m.Locale}\">{E(Language(m.Locale))}</span></div>"
        : "<nav class=\"catalogue-languages\" aria-label=\"Catalogue language\"><span>Catalogue language</span>"
        + Join(new[] { "uz", "ru", "en" }.Select(locale => $"<a href=\"/catalogue/?locale={locale}\" lang=\"{locale}\"{(locale == m.Locale ? " aria-current=\"true\"" : "")}>{E(Language(locale))}</a>")) + "</nav>";

    private static string Catalogue(Model m, IReadOnlyDictionary<string, string> query)
    {
        var edition = m.Edition;
        var hero = edition is null
            ? "<section class=\"catalogue-hero\"><p class=\"eyebrow\">The Ocharo collection</p><h1>The pieces.</h1><p class=\"deck\">Explore the shapes, colours and details of Ocharo.</p></section>"
            : $$"""
              <section class="catalogue-opening"><div><p class="eyebrow">Ocharo · The collection</p><h1>{{E(edition.Title)}}</h1>
              <p class="deck">Explore every piece. Keep the catalogue close.</p><p class="edition-meta">{{EditionMeta(edition)}}</p>
              <div class="catalogue-actions"><a class="button" href="#pieces">Explore the pieces ↓</a><a class="text-button" href="{{EditionPath(edition)}}">Read the catalogue →</a></div>
              <a class="catalogue-download" href="{{E(Download(edition.PdfUrl))}}" download="ocharo-catalogue.pdf">Download full catalogue · PDF ↓</a></div>
              <a class="catalogue-cover" href="{{EditionPath(edition)}}" aria-label="Open {{E(edition.Title)}}"><img src="{{E(edition.Pages[0].ImageUrl)}}" alt="{{E(edition.Pages[0].Title)}} — catalogue cover" fetchpriority="high"></a></section>
              """;
        return Start(m, "catalogue", edition?.Title ?? "The Ocharo collection", "Explore Ocharo pieces and the complete catalogue, page by page.", "/catalogue/")
            + "<main id=\"main\">" + hero + $$"""
              <section id="pieces" class="section catalogue-pieces"><div class="section-heading"><div><p class="eyebrow">The range, at a glance</p><h2>The pieces.</h2></div>{{(edition is null ? "" : $"<a href=\"{EditionPath(edition)}\">View catalogue pages →</a>")}}</div>
              {{CatalogueDiscovery(m, query)}}
              </section></main>
              """ + End(m, "catalogue");
    }

    private static string Edition(Model m, CatalogueEdition edition) =>
        Start(m, "catalogue", edition.Title, "The complete Ocharo catalogue. Open any page, or download the full edition.", EditionPath(edition)) + $$"""
        <main id="main" class="section edition-index"><a class="back" href="/catalogue/">← The pieces</a>
        <header class="edition-heading"><div><p class="eyebrow">The catalogue</p><h1>{{E(edition.Title)}}</h1><p class="edition-meta">{{EditionMeta(edition)}}</p></div>
        <a class="button" href="{{E(Download(edition.PdfUrl))}}" download="ocharo-catalogue.pdf">Download full catalogue · PDF ↓</a></header>
        <p class="edition-intro">A page for every detail. Open a page to explore it, save it, or share it.</p>
        <div class="edition-grid">{{Join(edition.Pages.Select(page => $$"""
        <article class="edition-card"><a href="{{EditionPagePath(edition, page)}}"><img src="{{E(page.ImageUrl)}}" alt="{{E(page.Title)}} — page {{page.Number}}" loading="lazy" style="aspect-ratio:{{page.WidthMm.ToString(CultureInfo.InvariantCulture)}}/{{page.HeightMm.ToString(CultureInfo.InvariantCulture)}}"><span><small>{{page.Number.ToString("00", CultureInfo.InvariantCulture)}}</small><strong>{{E(page.Title)}}</strong><span aria-hidden="true">↗</span></span></a>
        <a class="page-download" href="{{E(Download(page.PngUrl))}}" download="ocharo-page-{{page.Number}}.png">Save page · PNG ↓</a></article>
        """))}}</div></main>
        """ + End(m, "catalogue");

    private static string EditionPage(Model m, CatalogueEdition edition, CatalogueEditionPage page)
    {
        var index = edition.Pages.ToList().FindIndex(p => p.Id == page.Id);
        var previous = index > 0 ? edition.Pages[index - 1] : null;
        var next = index < edition.Pages.Count - 1 ? edition.Pages[index + 1] : null;
        var products = m.Products.Where(p => page.ProductIds.Contains(p.Id)).ToList();
        return Start(m, "catalogue", page.Title, $"Page {page.Number} of {edition.Title}. View and download this original catalogue page.", EditionPagePath(edition, page)) + $$"""
        <main id="main" class="section edition-reader"><a class="back" href="{{EditionPath(edition)}}">← All catalogue pages</a>
        <header class="reader-heading"><div><p class="eyebrow">{{E(edition.Title)}} · {{E(Language(edition.Locale))}}</p><h1>{{E(page.Title)}}</h1><p class="edition-meta">Page {{page.Number}} of {{edition.PageCount}}</p></div>
        <div class="reader-downloads"><a class="button small" href="{{E(Download(page.PdfUrl))}}" download="ocharo-page-{{page.Number}}.pdf">Download page · PDF ↓</a><a class="text-button" href="{{E(Download(page.PngUrl))}}" download="ocharo-page-{{page.Number}}.png">Save image · PNG ↓</a><button class="text-button" data-share-page>Copy page link ↗</button><span class="copy-status" role="status"></span></div></header>
        <figure class="catalogue-sheet"><img src="{{E(page.ImageUrl)}}" alt="{{E(page.Title)}} — catalogue page {{page.Number}}" style="aspect-ratio:{{page.WidthMm.ToString(CultureInfo.InvariantCulture)}}/{{page.HeightMm.ToString(CultureInfo.InvariantCulture)}}" fetchpriority="high"></figure>
        <nav class="page-navigation" aria-label="Catalogue pages">{{(previous is null ? "<span></span>" : $"<a rel=\"prev\" href=\"{EditionPagePath(edition, previous)}\">← {E(previous.Title)}</a>")}}<span>{{page.Number}} / {{edition.PageCount}}</span>{{(next is null ? "<span></span>" : $"<a rel=\"next\" href=\"{EditionPagePath(edition, next)}\">{E(next.Title)} →</a>")}}</nav>
        {{(products.Count == 0 ? "" : "<section class=\"page-pieces\"><div class=\"section-heading\"><h2>Pieces on this page.</h2></div><div class=\"product-grid\">" + Join(products.Select(ProductCard)) + "</div></section>")}}
        <div class="edition-footer"><a class="catalogue-download" href="{{E(Download(edition.PdfUrl))}}" download="ocharo-catalogue.pdf">Keep the complete catalogue · Download PDF ↓</a></div></main>
        """ + End(m, "catalogue");
    }
}
