using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Settings;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Site;

/// <summary>Publication-specific public experiences over the same reviewed editorial documents.</summary>
public sealed partial class PublicationSiteService(IBlogRepository repository, SiteService blog, ICatalogueFeed catalogue, IMediaStore media, PublicationSiteOptions? options = null)
{
    private sealed record Model(SiteSettings Settings, IReadOnlyList<ArticleDocument> Documents, IReadOnlyList<CatalogueProduct> Products,
        Func<string, string> Media, bool Preview, CatalogueEdition? Edition, string Locale, bool IsStaticExport = false, string? CatalogueAdminUrl = null, IReadOnlyList<string>? AvailableLocales = null);
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
    public static string Token(string text) => string.Concat(text.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
    private static string Join(IEnumerable<string> parts) => string.Concat(parts);
    private static string Title(string app) => app switch { "atlas" => "Ocharo Atlas", "wear" => "Ocharo Wear", _ => "Ocharo Catalogue" };
    private static bool Object(ArticleDocument d) => d.Kind is "textile" or "archive";
    private static string ProductPath(CatalogueProduct p) => "/catalogue/pieces/" + Uri.EscapeDataString(p.Slug ?? p.Id) + "/";
    private static string Read(string path) { using var stream = typeof(PublicationSiteService).Assembly.GetManifestResourceStream(path)!; using var reader = new StreamReader(stream); return reader.ReadToEnd(); }
    public static string Css => Read("OcharoPublications.Site.css");
    public static string Script => Read("OcharoPublications.Site.js");

    private async Task<Model> ModelAsync(CancellationToken ct, bool preview = false, Func<string, string>? mediaUrl = null, string locale = "en")
    {
        var documents = (await repository.GetArticlesAsync(ct)).Where(r => r.Status == ArticleStatus.Published || preview && r.Status is ArticleStatus.Draft or ArticleStatus.Scheduled)
            .Select(r => BlogJson.Read<ArticleDocument>(r.Json)).Where(d => d.Language == "en")
            .OrderByDescending(d => d.PublishedAt).ThenBy(d => d.Title, StringComparer.Ordinal).ToList();
        var settings = (await blog.ModelAsync(mediaUrl ?? LiveMedia, ct)).Settings;
        var snapshot = await catalogue.GetCatalogueAsync(ct, locale);
        return new Model(settings, documents, snapshot.Products, mediaUrl ?? LiveMedia, preview, snapshot.Edition, locale, CatalogueAdminUrl: SafeAdminUrl(options?.CatalogueAdminUrl), AvailableLocales: snapshot.AvailableLocales);
    }
    private static string? SafeAdminUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)) return null;
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.IsWellFormedOriginalString()
            && uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0
            && uri.Query.Length == 0 && uri.Fragment.Length == 0 ? uri.AbsoluteUri : null;
    }

    private static string PreviewLinks(string html) => new[] { "atlas", "wear", "catalogue" }.Aggregate(html,
        (page, app) => page.Replace($"href=\"/{app}/", $"href=\"/preview/apps/{app}/", StringComparison.Ordinal));

    private static string LiveMedia(string sha) => $"/media/{sha}/preview";

    public async Task<SitePage?> RenderAsync(string path, IReadOnlyDictionary<string, string>? query, CancellationToken ct, bool preview = false)
    {
        if (path.EndsWith("/site.css", StringComparison.Ordinal)) return new(Css, "text/css; charset=utf-8");
        if (path.EndsWith("/site.js", StringComparison.Ordinal)) return new(Script, "text/javascript; charset=utf-8");
        var arguments = query ?? new Dictionary<string, string>();
        var locale = CatalogueLanguages.Normalize(arguments.GetValueOrDefault("locale"));
        var model = await ModelAsync(ct, preview, locale: locale);
        var page = Render(model, path, arguments);
        return page is null ? null : new(preview ? PreviewLinks(page) : page, "text/html; charset=utf-8");
    }

    public async Task<string> PreviewAsync(Guid id, CancellationToken ct)
    {
        var record = await repository.FindArticleAsync(id, ct) ?? throw BlogException.NotFound("Editorial item");
        var d = BlogJson.Read<ArticleDocument>(record.Json);
        var model = await ModelAsync(ct, true);
        if (!model.Documents.Any(x => x.Slug == d.Slug)) model = model with { Documents = model.Documents.Append(d).ToList() };
        return PreviewLinks(Detail(model, d));
    }

    private static string? Render(Model m, string path, IReadOnlyDictionary<string, string> q)
    {
        var p = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = p switch
        {
            ["atlas"] => AtlasHome(m),
            ["atlas", "gallery"] => Gallery(m, q),
            ["atlas", "stories"] => StoryIndex(m, "atlas"),
            ["atlas", "about"] => About(m),
            ["atlas", "textiles", var slug] => m.Documents.FirstOrDefault(d => d.Publication == "atlas" && Object(d) && d.Slug == slug) is { } d ? Detail(m, d) : null,
            ["atlas", "stories", var slug] => m.Documents.FirstOrDefault(d => d.Publication == "atlas" && !Object(d) && d.Slug == slug) is { } d ? Detail(m, d) : null,
            ["atlas", var axis, var value] when axis is "places" or "techniques" or "motifs" => Gallery(m, new Dictionary<string, string> { [axis] = value }),
            ["wear"] => WearHome(m, q),
            ["wear", "guides"] => StoryIndex(m, "wear"),
            ["wear", "guides", var slug] => m.Documents.FirstOrDefault(d => d.Publication == "wear" && d.Slug == slug) is { } d ? Detail(m, d) : null,
            ["wear", "occasions", var occasion] => WearHome(m, new Dictionary<string, string> { ["occasions"] = occasion }),
            ["catalogue"] => Catalogue(m, q),
            ["catalogue", "editions", var editionId] when m.Edition?.Id == editionId => Edition(m, m.Edition),
            ["catalogue", "editions", var editionId, "pages", var pageId] when m.Edition?.Id == editionId =>
                m.Edition.Pages.FirstOrDefault(p => p.Id == pageId) is { } page ? EditionPage(m, m.Edition, page) : null,
            ["catalogue", "pieces", var slug] => m.Products.FirstOrDefault(p => (p.Slug ?? p.Id) == Uri.UnescapeDataString(slug)) is { } product ? Product(m, product) : null,
            _ => null,
        };
        return result is not null && p.FirstOrDefault() == "catalogue" ? LocalizeCatalogueLinks(result, m.Locale) : result;
    }

    private static string Start(Model m, string app, string title, string description, string path)
    {
        if (app == "catalogue") path = CatalogueLocaleUrl(path, m.Locale);
        var robots = m.Preview || m.Settings.NoIndex ? "<meta name=\"robots\" content=\"noindex,nofollow\">" : "";
        var canonical = !m.Preview && m.Settings.BaseUrl is { } origin ? $"<link rel=\"canonical\" href=\"{E(origin + path)}\">" : "";
        var nav = app switch
        {
            "atlas" => "<a href=\"/atlas/gallery/\">Textile gallery</a><a href=\"/atlas/stories/\">Stories</a><a href=\"/atlas/about/\">About the atlas</a>",
            "wear" => "<a href=\"/wear/guides/\">All guides</a><a href=\"/wear/#occasions\">Find your occasion</a>",
            _ => "<a href=\"/catalogue/#pieces\">The pieces</a>" + (m.Edition is {} edition ? $"<a href=\"{EditionPath(edition)}\">The catalogue</a>" : "") + "<a href=\"/wear/\">Ways to wear</a>",
        };
        return $"<!doctype html><html lang=\"en\" data-content-locale=\"{(app == "catalogue" ? m.Locale : "en")}\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{E(title)} — {Title(app)}</title><meta name=\"description\" content=\"{E(description)}\">{robots}{canonical}<meta property=\"og:title\" content=\"{E(title)}\"><meta property=\"og:description\" content=\"{E(description)}\"><link rel=\"stylesheet\" href=\"/{app}/site.css\"><script src=\"/{app}/site.js\" defer></script></head><body class=\"publication {app}\"><a class=\"skip\" href=\"#main\">Skip to content</a>" +
            (m.Preview ? "<div class=\"preview-notice\" role=\"status\">Editorial preview · includes unpublished drafts · not a public edition</div>" : "") +
            $"<header class=\"mast\"><a class=\"wordmark\" href=\"/{app}/\">{Title(app)}<span>by Ocharo</span>{BuildVersion.Badge}</a><nav aria-label=\"Main navigation\">{nav}</nav><a class=\"parent-link\" href=\"/\">Ocharo Blog ↗</a></header>" + (app == "catalogue" ? CatalogueLanguageNavigation(m) : "");
    }
    private static string End(Model m, string app)
    {
        var links = Join(m.Settings.Links.Where(l => Uri.TryCreate(l.Url, UriKind.Absolute, out var u) && u.Scheme == "https").Select(l => $"<a href=\"{E(l.Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(l.Label)} ↗</a>"));
        var admin = app == "catalogue" && !m.Preview && !m.IsStaticExport && m.CatalogueAdminUrl is {} url
            ? $"<a class=\"catalogue-admin\" href=\"{E(url)}\" rel=\"nofollow\">Catalogue admin</a>" : "";
        return $"<footer class=\"foot\"><div><strong>Ocharo</strong><p>Cloth, culture, and the lives we wear them in.</p></div><nav aria-label=\"Explore Ocharo\"><a href=\"/atlas/\">Atlas</a><a href=\"/wear/\">Wear</a><a href=\"/\">Blog</a><a href=\"/catalogue/\">Catalogue</a></nav><div class=\"channels\">{links}{admin}</div><small>{E(m.Settings.EditionNote)}</small></footer></body></html>";
    }
    private static string Empty(string title, string body) => $"<section class=\"empty\"><span class=\"eyebrow\">A considered collection</span><h2>{E(title)}</h2><p>{E(body)}</p><a href=\"/\">Explore Ocharo Blog →</a></section>";
    private static string Card(Model m, ArticleDocument d, string? reason = null)
    {
        var visual = d.Cover is { } cover ? $"<img src=\"{E(m.Media(cover.MediaSha256))}\" alt=\"{E(cover.Alt)}\" loading=\"lazy\">" : $"<div class=\"type-art\"><span>{E(d.Format ?? d.Kind)}</span><strong>{E(d.CardPrompt ?? d.Title)}</strong><span>Ocharo {E(d.Publication)}</span></div>";
        var tags = string.Join(" ", d.Places.Concat(d.Techniques).Concat(d.Motifs).Concat(d.Occasions));
        return $"<article class=\"editorial-card\" data-item data-search=\"{E(d.Title + " " + d.Excerpt + " " + tags)}\" data-places=\"{E(string.Join('|', d.Places.Select(Token)))}\" data-techniques=\"{E(string.Join('|', d.Techniques.Select(Token)))}\" data-motifs=\"{E(string.Join('|', d.Motifs.Select(Token)))}\" data-occasions=\"{E(string.Join('|', d.Occasions.Select(Token)))}\"><a class=\"card-visual\" href=\"{Publications.Path(d)}\">{visual}</a><div class=\"card-copy\"><p class=\"eyebrow\">{E(reason ?? (d.Kind == "archive" ? "Archive encounter" : d.Textile?.Place ?? d.Occasions.FirstOrDefault() ?? d.Format ?? "A closer look"))}</p><h3><a href=\"{Publications.Path(d)}\">{E(d.Title)}</a></h3><p>{E(d.Excerpt)}</p><span class=\"card-meta\">{(Object(d) ? E(d.Textile?.Period) : ArticleService.ReadMinutes(ArticleService.Words(d)) + " min read")} · {E(d.Status == ArticleStatus.Published ? "Explore" : "Draft preview")} →</span></div></article>";
    }
    private static string AtlasHome(Model m)
    {
        var docs = m.Documents.Where(d => d.Publication == "atlas").ToList(); var objects = docs.Where(Object).ToList(); var hero = objects.FirstOrDefault();
        var visual = hero?.Cover is { } c ? $"<a class=\"hero-object\" href=\"{Publications.Path(hero)}\"><img src=\"{E(m.Media(c.MediaSha256))}\" alt=\"{E(c.Alt)}\" fetchpriority=\"high\"><span>{E(hero.Textile?.Place)} · {E(hero.Textile?.Period)}<br>{E(c.Caption)}</span></a>" : "<div class=\"atlas-title-study\" aria-hidden=\"true\">A<br>closer<br>look.</div>";
        return Start(m, "atlas", "Uzbekistan, told through cloth", "A visual collection of Uzbekistan textile objects, places, techniques and the stories behind them.", "/atlas/") +
            $"<main id=\"main\"><section class=\"atlas-hero\"><div><p class=\"eyebrow\">A living atlas of Uzbekistan</p><h1>Every cloth.<br>A place.<br>A story.</h1><p class=\"deck\">Look closer at the colours, hands and histories woven into a textile. Begin with one object. Follow what catches your eye.</p><a class=\"button\" href=\"/atlas/gallery/\">Enter the textile gallery <span>↗</span></a></div>{visual}</section><section class=\"section\"><div class=\"section-heading\"><div><p class=\"eyebrow\">Start with looking</p><h2>Cloth, in context.</h2></div><a href=\"/atlas/gallery/\">Explore the collection →</a></div>" +
            (objects.Count == 0 ? Empty("The collection is being prepared", "Each object will arrive with its story, documented source and image credit.") : $"<div class=\"card-grid\">{Join(objects.Take(6).Select(d => Card(m,d)))}</div>") +
            $"</section><section class=\"statement\"><p class=\"eyebrow\">A small invitation</p><h2>Notice the edge.<br>Follow the thread.</h2><p>A whole cloth reveals something a pattern swatch cannot. Each object carries its own place, period and attribution.</p><a href=\"/atlas/about/\">How to read the atlas →</a></section><section class=\"section\"><div class=\"section-heading\"><h2>Stories behind the surface.</h2><a href=\"/atlas/stories/\">All stories →</a></div><div class=\"card-grid\">{Join(docs.Where(d=>!Object(d)).Take(3).Select(d=>Card(m,d)))}</div></section></main>" + End(m,"atlas");
    }
    private static string Filters(IEnumerable<ArticleDocument> docs, string action, params (string Key,string Label,Func<ArticleDocument,IReadOnlyList<string>> Values)[] axes)
    {
        return $"<form class=\"filters\" data-filter-form action=\"{action}\" method=\"get\"><label>Search<input name=\"q\" type=\"search\" placeholder=\"A place, pattern or question\" autocomplete=\"off\"></label>" +
            Join(axes.Select(a=>$"<label>{a.Label}<select name=\"{a.Key}\"><option value=\"\">All {a.Label.ToLowerInvariant()}</option>{Join(docs.SelectMany(a.Values).Distinct(StringComparer.OrdinalIgnoreCase).Order().Select(v=>$"<option value=\"{E(Token(v))}\">{E(v)}</option>"))}</select></label>")) + "<button class=\"button small\" type=\"submit\">Find</button><button class=\"text-button\" type=\"reset\">Reset</button></form>";
    }
    private static bool Matches(ArticleDocument d, IReadOnlyDictionary<string,string> q) =>
        (!q.TryGetValue("q",out var text) || (d.Title+" "+d.Excerpt+" "+string.Join(' ',d.Places.Concat(d.Techniques).Concat(d.Motifs).Concat(d.Occasions))).Contains(text,StringComparison.OrdinalIgnoreCase)) &&
        new[] { ("places", d.Places),("techniques",d.Techniques),("motifs",d.Motifs),("occasions",d.Occasions) }.All(a=>!q.TryGetValue(a.Item1,out var value)||value.Length==0||a.Item2.Any(v=>Token(v)==value));
    private static string Gallery(Model m, IReadOnlyDictionary<string,string> q)
    {
        var all=m.Documents.Where(d=>d.Publication=="atlas"&&Object(d)).ToList(); var found=all.Where(d=>Matches(d,q)).ToList();
        return Start(m,"atlas","The textile gallery","Explore documented textiles and archival views by place, technique and visible motif.","/atlas/gallery/")+ $"<main id=\"main\" class=\"section collection\"><p class=\"eyebrow\">Look first. Then follow the story.</p><h1>The textile gallery<span class=\"period\">.</span></h1><p class=\"deck\">Whole objects, close observations, and the context that makes them more than a pattern.</p>" + Filters(all,"/atlas/gallery/",("places","Places",d=>d.Places),("techniques","Techniques",d=>d.Techniques),("motifs","Motifs",d=>d.Motifs)) + $"<p class=\"result-count\" aria-live=\"polite\" data-count>{found.Count} of {all.Count} objects and archive views</p><div class=\"card-grid\" data-filter-grid>{Join(all.Select(d=>Card(m,d)))}</div><p class=\"empty filter-empty\" data-empty{(found.Count>0?" hidden":"")}>No objects match these choices. Try another place or reset the filters.</p><noscript><style>[data-filter-grid]{{display:none}}</style><div class=\"card-grid\">{Join(found.Select(d=>Card(m,d)))}</div></noscript></main>"+End(m,"atlas");
    }
    private static string WearHome(Model m,IReadOnlyDictionary<string,string> q)
    {
        var all=m.Documents.Where(d=>d.Publication=="wear").ToList();var found=all.Where(d=>Matches(d,q)).ToList();
        return Start(m,"wear","A wardrobe for the life you have","Practical dressing ideas for real occasions, with room for your own taste.","/wear/")+ $"<main id=\"main\"><section class=\"wear-hero\"><p class=\"eyebrow\">Good clothes. Real plans.</p><h1>What are<br>you dressing <em>for?</em></h1><p class=\"deck\">The invitation is a starting point. Find an idea that works when you sit down, walk out, and stay a little longer.</p><a class=\"button\" href=\"#occasions\">Find your moment ↓</a><span class=\"wear-note\">Wear it your way.</span></section><section id=\"occasions\" class=\"section\"><div class=\"section-heading\"><h2>Begin with the occasion.</h2><span>Useful ideas, no dress-code panic.</span></div>"+Filters(all,"/wear/",("occasions","Occasions",d=>d.Occasions))+$"<p class=\"result-count\" aria-live=\"polite\" data-count>{found.Count} guides</p><div class=\"card-grid\" data-filter-grid>{Join(all.Select(d=>Card(m,d)))}</div><p class=\"empty filter-empty\" data-empty{(found.Count>0?" hidden":"")}>Guides are being prepared for this occasion. Browse all guides or reset your choices.</p><noscript><style>[data-filter-grid]{{display:none}}</style><div class=\"card-grid\">{Join(found.Select(d=>Card(m,d)))}</div></noscript></section><section class=\"statement\"><p class=\"eyebrow\">Clothes are allowed a second life</p><h2>A special piece.<br>An ordinary Tuesday.</h2><p>Try the idea with something you already own. When you want to explore a new piece, the catalogue is nearby.</p><a href=\"/catalogue/\">Explore Ocharo pieces →</a></section></main>"+End(m,"wear");
    }
    private static string StoryIndex(Model m,string app)
    {
        var docs=m.Documents.Where(d=>d.Publication==app&&!Object(d)).ToList();
        return Start(m,app,app=="atlas"?"Stories behind the cloth":"All wearing guides","Stories and useful questions, chosen for a closer look.",$"/{app}/{(app=="atlas"?"stories":"guides")}/")+$"<main id=\"main\" class=\"section collection\"><p class=\"eyebrow\">{(app=="atlas"?"The reading room":"Ideas for real life")}</p><h1>{(app=="atlas"?"Stories behind the cloth.":"A little help getting dressed.")}</h1><div class=\"card-grid\">{Join(docs.Select(d=>Card(m,d)))}</div>"+(docs.Count==0?Empty("The next story is taking shape","Explore Ocharo Blog while our editors prepare this collection."):"")+"</main>"+End(m,app);
    }
    private static string Detail(Model m,ArticleDocument d)
    {
        var app=d.Publication; var image=d.Cover is { } c?$"<figure class=\"object-image\"><a href=\"{E(m.Media(c.MediaSha256))}\" target=\"_blank\" rel=\"noopener\" aria-label=\"Open full image\"><img src=\"{E(m.Media(c.MediaSha256))}\" alt=\"{E(c.Alt)}\" fetchpriority=\"high\"></a><figcaption>{E(c.Caption)} <span>Open image to look closer ↗</span></figcaption></figure>":"";
        var facts=d.Textile is { } t?$"<dl class=\"facts\">{Join(new[]{("Place",t.Place),("Period",t.Period),("Material",t.Materials),("Technique",t.Technique),("Dimensions",t.Dimensions),("Maker",t.Maker),("Collection",t.Collection),("Object record",t.ObjectId)}.Where(x=>!string.IsNullOrWhiteSpace(x.Item2)).Select(x=>$"<div><dt>{x.Item1}</dt><dd>{E(x.Item2)}</dd></div>"))}</dl><p class=\"attribution-note\">{E(t.AttributionNote)}</p>":"";
        var credit=d.ImageProvenance is { } evidence?$"<aside class=\"image-credit\"><strong>{(evidence.Kind=="creative-visualization"?"Creative visualization":"Image record")}</strong><p>{E(evidence.Credit)}</p><p>{E(evidence.Rights)}</p><a href=\"{E(evidence.SourceUrl)}\" target=\"_blank\" rel=\"noopener noreferrer\">Original collection record ↗</a></aside>":"";
        var body=Join(d.Sections.Select((s,i)=>$"<section id=\"section-{i+1}\"><h2>{E(s.Heading)}</h2>{ArticleFormatting.Render(s)}{(s.List is {Count:>0}?$"<ul>{Join(s.List.Select(v=>$"<li>{E(v)}</li>"))}</ul>":"")}{(s.SourceIndexes is {Count:>0}?$"<p class=\"inline-sources\">Sources: {Join(s.SourceIndexes.Where(x=>x>=0&&x<d.Sources.Count).Select(x=>$"<a href=\"#source-{x+1}\">{x+1}</a> "))}</p>":"")}</section>"));
        var linked=m.Documents.Where(x=>d.TextileSlugs.Contains(x.Slug)||Object(d)&&x.TextileSlugs.Contains(d.Slug)||d.RelatedSlugs.Contains(x.Slug)).Where(x=>x.Slug!=d.Slug).DistinctBy(x=>x.Slug).ToList();
        var tags=Join(new[]{("places",d.Places),("techniques",d.Techniques),("motifs",d.Motifs),("occasions",d.Occasions)}.SelectMany(x=>x.Item2.Select(v=>$"<a class=\"tag\" href=\"/{app}/{x.Item1}/{E(Token(v))}/\">{E(v)}</a>")));
        var pieces=m.Products.Where(p=>d.ProductIds.Contains(p.Id)).ToList();
        return Start(m,app,d.SeoTitle??d.Title,d.SeoDescription??d.Excerpt??"",Publications.Path(d))+$"<main id=\"main\"><article class=\"detail\"><a class=\"back\" href=\"/{app}/\">← {Title(app)}</a><header class=\"article-head\"><p class=\"eyebrow\">{E(d.Format??d.Kind)} · {ArticleService.ReadMinutes(ArticleService.Words(d))} min read</p><h1>{E(d.Title)}</h1><p class=\"deck\">{E(d.Excerpt)}</p><div class=\"tags\">{tags}</div></header><div class=\"{(Object(d)?"object-layout":"story-layout")}\"><div>{image}{credit}</div><div>{facts}<div class=\"prose\"><p class=\"lead\">{E(d.Intro)}</p>{body}{(d.Takeaway is{} take?$"<aside class=\"takeaway\"><span class=\"eyebrow\">Take this with you</span><p>{E(take)}</p></aside>":"")}{(d.ClosingQuestion is{} question?$"<section><h2>One question to keep</h2><p>{E(question)}</p></section>":"")}<section class=\"sources\"><h2>Sources &amp; credits</h2><ol>{Join(d.Sources.Select((s,i)=>$"<li id=\"source-{i+1}\"><a href=\"{E(s.Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(s.Label)} ↗</a><p>{E(s.Supports)}</p></li>"))}</ol></section></div></div></div></article>"+
            (linked.Count>0?$"<section class=\"section\"><div class=\"section-heading\"><h2>{(Object(d)?"Follow this object's story":"Textiles and stories connected to this page")}</h2></div><div class=\"card-grid\">{Join(linked.Select(x=>Card(m,x)))}</div></section>":"")+
            (pieces.Count>0?$"<section class=\"section\"><h2>Contemporary pieces to explore</h2><p>Ocharo products are shown separately from the historical objects above.</p><div class=\"product-grid\">{Join(pieces.Select(piece => ProductCard(m, piece)))}</div></section>":"")+"</main>"+End(m,app);
    }
    private static string About(Model m)=>Start(m,"atlas","A closer look, with context","How Ocharo Atlas connects an object, its documented facts and its story.","/atlas/about/")+"<main id=\"main\" class=\"section narrow\"><p class=\"eyebrow\">About the atlas</p><h1>A beautiful surface.<br>A specific history.</h1><div class=\"prose\"><p>Ocharo Atlas is a growing editorial collection focused on Uzbekistan's textile culture. Each entry begins with an identifiable object, an archival image, or a documented process.</p><h2>Look, read, follow.</h2><p>Start in the gallery. Open a whole object, explore its details, then follow a place, technique or story. Pattern names and visual descriptions are invitations to look; they are not universal symbol definitions.</p><h2>Keep the evidence close.</h2><p>Every published object includes the collection record, image credit, rights statement and the limits of its attribution. Unknown makers and uncertain places remain unknown or attributed. Historical records do not certify the materials of a contemporary garment.</p><h2>A living practice.</h2><p>The collection can grow through original reporting, named makers and carefully sourced objects. Ocharo's contemporary catalogue remains a separate experience, connected where it helps the reader.</p><a class=\"button\" href=\"/atlas/gallery/\">Return to the gallery →</a></div></main>"+End(m,"atlas");

    private static string ProductCard(Model m, CatalogueProduct p)=>$"<article class=\"product-card\" data-item data-search=\"{E(p.Name+" "+p.Fabric+" "+p.Category)}\"><a class=\"product-visual\" href=\"{ProductPath(p)}\">{(p.ImageUrl is{} url?$"<img src=\"{E(url)}\" alt=\"{E(p.ImageAlt??p.Name)}\" loading=\"lazy\">":"<span>Image being prepared</span>")}</a><div><p class=\"eyebrow\">{E(p.Category??p.GarmentName)}</p><h3 {ContentLanguage(m)}><a href=\"{ProductPath(p)}\">{E(p.Name)}</a></h3><p {ContentLanguage(m)}>{E(p.Fabric)}</p><p class=\"price\">{Price(p)}</p>{(p.ImageKind is "visualization" or "creative-visualization"?"<small>Creative visualization</small>":"")}</div></article>";
    private static string Price(CatalogueProduct p)=>p.PriceAmount is{} amount&&p.Currency is{} currency?E(amount.ToString("N0",CultureInfo.InvariantCulture)+" "+currency):p.RetailPrice is{} retail?E(retail.ToString("N0",CultureInfo.InvariantCulture)+" UZS"):"Price on enquiry";
    private static string Product(Model m,CatalogueProduct p)
    {
        var channels=Join(m.Settings.Links.Take(2).Select(l=>$"<a class=\"button\" href=\"{E(l.Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(l.Label)} ↗</a>"));
        var images=p.Images is {Count:>0}?p.Images:p.ImageUrl is{} image?[new CatalogueImage(image,p.ImageAlt??p.Name,p.ImageKind,p.ImageCredit,p.ImageRights)]:Array.Empty<CatalogueImage>();
        var stories=m.Documents.Where(d=>d.ProductIds.Contains(p.Id)&&d.Publication is "wear" or "blog").Take(3).ToList();
        return Start(m,"catalogue",p.Name,p.Description??p.Fabric??"Ocharo piece",ProductPath(p))+$"<main id=\"main\" class=\"section\"><a class=\"back\" href=\"/catalogue/\">← All pieces</a><article class=\"product-detail\"><div class=\"product-images\">{Join(images.Select(i=>$"<figure><img src=\"{E(i.Url)}\" alt=\"{E(i.Alt)}\"><figcaption>{(i.Kind is "visualization" or "creative-visualization"?"Creative visualization · ":"")}{E(i.Credit)}</figcaption></figure>"))}</div><div><p class=\"eyebrow\">{E(p.Category??p.GarmentName)}</p><h1 {ContentLanguage(m)}>{E(p.Name)}</h1><p class=\"price\">{Price(p)}</p><p class=\"deck\" {ContentLanguage(m)}>{E(p.Description)}</p><dl class=\"facts\"><div><dt>Material</dt><dd>{E(p.Fabric??"Ask Ocharo for the confirmed composition")}</dd></div><div><dt>Availability</dt><dd>{E(p.Availability??"Confirm with Ocharo")}</dd></div><div><dt>Sizes</dt><dd>{E(p.Sizes is{Count:>0}?string.Join(", ",p.Sizes):"Ask about measurements")}</dd></div></dl>{ProductEditionLinks(m,p)}<div class=\"enquiry\"><h2>Make it a conversation.</h2><p>Ask about this piece, its measurements and current availability. Mention <strong>{E(p.Name)}</strong>.</p>{channels}<form class=\"enquiry-form\" data-inquiry data-product=\"{E(p.Id)}\"><label>Your name<input name=\"name\" required maxlength=\"100\" autocomplete=\"name\"></label><label>Contact method<select name=\"contactMethod\"><option value=\"telegram\">Telegram</option><option value=\"email\">Email</option><option value=\"phone\">Phone</option></select></label><label>Contact<input name=\"contact\" required maxlength=\"180\" autocomplete=\"off\"></label><label>Your message<textarea name=\"message\" required maxlength=\"3000\">I would like to ask about {E(p.Name)}.</textarea></label><label class=\"consent\"><input type=\"checkbox\" name=\"consent\" required> I consent to Ocharo contacting me about this enquiry.</label><input type=\"text\" name=\"website\" tabindex=\"-1\" autocomplete=\"off\" class=\"honeypot\" aria-hidden=\"true\"><button class=\"button\" type=\"submit\">Send enquiry</button><p role=\"status\" data-inquiry-status></p></form><button class=\"text-button\" data-copy=\"{E($"Hello Ocharo, I would like to ask about {p.Name} (reference {p.Id}). Could you confirm availability and measurements?")}\">Copy enquiry text</button><span class=\"copy-status\" role=\"status\"></span></div></div></article>"+(stories.Count>0?$"<section><h2>Ideas for this piece</h2><div class=\"card-grid\">{Join(stories.Select(d=>Card(m,d)))}</div></section>":"")+"</main>"+End(m,"catalogue");
    }

    /// <summary>Each app exports a standalone tree from published content only.</summary>
    public async Task<byte[]> ExportAsync(string publication,CancellationToken ct,string locale="en")
    {
        if(publication is not ("atlas" or "wear" or "catalogue"))throw new BlogException("Unknown public app.");
        locale = CatalogueLanguages.Normalize(locale);
        var used=new HashSet<string>();string Media(string sha){used.Add(sha);return $"/media/{sha}.webp";}
        var model=(await ModelAsync(ct,mediaUrl:Media,locale:locale)) with { IsStaticExport = true };var paths=new HashSet<string>{$"/{publication}/"};
        if(publication=="atlas") {paths.UnionWith(["/atlas/gallery/","/atlas/stories/","/atlas/about/"]);foreach(var d in model.Documents.Where(d=>d.Publication==publication)){paths.Add(Publications.Path(d));foreach(var pair in new[]{("places",d.Places),("techniques",d.Techniques),("motifs",d.Motifs)})foreach(var tag in pair.Item2)paths.Add($"/atlas/{pair.Item1}/{Token(tag)}/");}}
        if(publication=="wear") {paths.Add("/wear/guides/");foreach(var d in model.Documents.Where(d=>d.Publication==publication)){paths.Add(Publications.Path(d));foreach(var tag in d.Occasions)paths.Add($"/wear/occasions/{Token(tag)}/");}}
        var editionFiles = new Dictionary<string, byte[]>();
        if(publication=="catalogue")
        {
            foreach(var p in model.Products)paths.Add(ProductPath(p));
            if (model.Edition is {} edition)
            {
                // Explicit exports freeze exact released files. Live pages always re-read the active public release.
                async Task<string> Copy(string url, string file)
                {
                    var name = $"catalogue/files/{edition.Id}/{file}";
                    editionFiles[name] = await catalogue.DownloadAsync(url, ct)
                        ?? throw new BlogException("The released catalogue file is unavailable. Retry after Platform is reachable.");
                    return "/" + name;
                }
                var pdf = await Copy(edition.PdfUrl, "catalogue.pdf");
                var pages = new List<CatalogueEditionPage>();
                foreach (var page in edition.Pages)
                {
                    var png = await Copy(page.PngUrl, page.Id + ".png");
                    var pagePdf = await Copy(page.PdfUrl, page.Id + ".pdf");
                    pages.Add(page with { ImageUrl = png, PngUrl = png, PdfUrl = pagePdf });
                }
                model = model with { Edition = edition with { PdfUrl = pdf, Pages = pages } };
                paths.Add(EditionPath(edition));
                foreach (var page in edition.Pages) paths.Add(EditionPagePath(edition, page));
            }
        }
        using var output=new MemoryStream();using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            void Text(string name,string value){using var writer=new StreamWriter(zip.CreateEntry(name,CompressionLevel.Optimal).Open(),new UTF8Encoding(false));writer.Write(value);}
            foreach(var path in paths){var html=Render(model,path,new Dictionary<string,string>());if(html is not null)Text(path.TrimStart('/')+"index.html",html);}
            Text("index.html",Render(model,$"/{publication}/",new Dictionary<string,string>())!);Text($"{publication}/site.css",Css);Text($"{publication}/site.js",Script);
            Text("robots.txt",model.Settings.NoIndex?"User-agent: *\nDisallow: /\n":"User-agent: *\nAllow: /\n");
            Text("publication.json",System.Text.Json.JsonSerializer.Serialize(new{schemaVersion=1,publication,locale=model.Locale,generatedAt=DateTimeOffset.UtcNow,pages=paths.Count,noIndex=model.Settings.NoIndex,catalogueEditionId=publication=="catalogue"?model.Edition?.Id:null}));
            foreach(var file in editionFiles){await using var entry=zip.CreateEntry(file.Key,CompressionLevel.NoCompression).Open();await entry.WriteAsync(file.Value,ct);}
            foreach(var sha in used){var opened=await media.OpenAsync(sha,true,ct);if(opened is null)continue;await using var input=opened.Value.Content;await using var entry=zip.CreateEntry($"media/{sha}.webp",CompressionLevel.NoCompression).Open();await input.CopyToAsync(entry,ct);}
        }
        return output.ToArray();
    }
}
