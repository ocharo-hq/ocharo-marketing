using System.Globalization;
using System.Net;
using System.Text;
using System.Xml.Linq;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Site;

/// <summary>
/// Everything one render pass needs. <see cref="Stories"/> are the published stories of <see cref="Language"/>;
/// <see cref="Editions"/> are every published story, used for language alternates.
/// </summary>
public sealed record SiteModel(
    SiteSettings Settings,
    IReadOnlyList<TopicDocument> Topics,
    IReadOnlyList<ArticleDocument> Stories,
    IReadOnlyDictionary<string, CatalogueProduct> Products,
    Func<string, string> MediaUrl,
    DateTimeOffset GeneratedAt,
    string Language = "en",
    IReadOnlyList<ArticleDocument>? Editions = null,
    IReadOnlyList<string>? AvailableLanguages = null,
    BrandEvent? Event = null);

/// <summary>
/// Server-side renderer for the public blog, ported from the approved static journal layout (same markup and CSS
/// classes) with English at the root and Uzbek/Russian editions under <c>/uz/</c> and <c>/ru/</c>.
/// Plain text is encoded; explicitly formatted sections use the fixed safe Markdown policy.
/// </summary>
public sealed class SiteRenderer(SiteModel site)
{
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    private static string Icon(string path) =>
        $"<svg class=\"link-icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">{path}</svg>";

    private static readonly string Arrow = Icon("<path d=\"M5 12h14M13 6l6 6-6 6\"/>");
    private static readonly string ExternalArrow = Icon("<path d=\"M6 18 18 6M6 6h12v12\"/>");
    private static readonly string BackArrow = Icon("<path d=\"M19 12H5m6-6-6 6 6 6\"/>");

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Strings = new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        ["en"] = new Dictionary<string, string>
        {
            ["all"] = "All stories", ["min"] = "min read", ["read"] = "Read the story", ["readLabel"] = "Read", ["featured"] = "Featured story",
            ["think"] = "Something to think about", ["browse"] = "Browse all {0} stories", ["home"] = "Blog home", ["room"] = "The reading room",
            ["stories"] = "stories", ["allIntro"] = "A wardrobe dilemma, an experiment to try, a detail you might have missed. Start with a question that feels familiar.",
            ["keep"] = "Keep reading", ["take"] = "Take this with you", ["question"] = "One question to keep", ["sources"] = "Sources & further reading",
            ["further"] = "Further reading", ["pieces"] = "Pieces in this story", ["ask"] = "Ask about a piece", ["essay"] = "Essay",
            ["skip"] = "Skip to content", ["menu"] = "Main navigation", ["preview"] = "Preview — this page is not public", ["pattern"] = "A pattern is<br>only the beginning.",
            ["thread"] = "Follow the thread", ["visual"] = "Collection visualisation", ["languages"] = "Languages",
        },
        ["uz"] = new Dictionary<string, string>
        {
            ["all"] = "Barcha hikoyalar", ["min"] = "daqiqa o‘qish", ["read"] = "Hikoyani o‘qish", ["readLabel"] = "O‘qish", ["featured"] = "Tanlangan hikoya",
            ["think"] = "O‘ylab ko‘rish uchun", ["browse"] = "Barcha {0} ta hikoya", ["home"] = "Blog bosh sahifasi", ["room"] = "O‘qish xonasi",
            ["stories"] = "ta hikoya", ["allIntro"] = "Kiyim javonidagi savol, sinab ko‘rish uchun tajriba yoki e’tibordan chetda qolgan tafsilot. Tanish savoldan boshlang.",
            ["keep"] = "Yana o‘qing", ["take"] = "Eslab qoling", ["question"] = "O‘ylash uchun bitta savol", ["sources"] = "Manbalar va qo‘shimcha o‘qish",
            ["further"] = "Qo‘shimcha o‘qish", ["pieces"] = "Hikoyadagi kiyimlar", ["ask"] = "Kiyim haqida so‘rash", ["essay"] = "Esse",
            ["skip"] = "Asosiy matnga o‘tish", ["menu"] = "Asosiy menyu", ["preview"] = "Ko‘rib chiqish — bu sahifa hali ommaviy emas", ["pattern"] = "Naqsh —<br>faqat boshlanishi.",
            ["thread"] = "Ipni kuzating", ["visual"] = "To‘plam vizualizatsiyasi", ["languages"] = "Tillar",
        },
        ["ru"] = new Dictionary<string, string>
        {
            ["all"] = "Все истории", ["min"] = "мин чтения", ["read"] = "Читать историю", ["readLabel"] = "Читать", ["featured"] = "Главная история",
            ["think"] = "Есть над чем подумать", ["browse"] = "Все истории ({0})", ["home"] = "Главная блога", ["room"] = "Читальный зал",
            ["stories"] = "историй", ["allIntro"] = "Вопрос гардероба, эксперимент или деталь, которую легко не заметить. Начните со знакомого вопроса.",
            ["keep"] = "Читать дальше", ["take"] = "Возьмите с собой", ["question"] = "Один вопрос на память", ["sources"] = "Источники и что почитать",
            ["further"] = "Почитать", ["pieces"] = "Вещи из этой истории", ["ask"] = "Спросить о вещи", ["essay"] = "Эссе",
            ["skip"] = "Перейти к тексту", ["menu"] = "Главное меню", ["preview"] = "Предпросмотр — страница не опубликована", ["pattern"] = "Узор —<br>это только начало.",
            ["thread"] = "Следовать за нитью", ["visual"] = "Визуализация коллекции", ["languages"] = "Языки",
        },
    };

    private static readonly IReadOnlyDictionary<string, string> LanguageNames = new Dictionary<string, string> { ["en"] = "English", ["uz"] = "O‘zbekcha", ["ru"] = "Русский" };

    private SiteSettings S => site.Settings;

    private string Lang => site.Language;

    private string P => SiteLanguages.Prefix(Lang);

    private string T(string key) => Strings[Lang].GetValueOrDefault(key) ?? Strings["en"][key];

    public static string StoryLink(ArticleDocument story) => $"{SiteLanguages.Prefix(story.Language)}/journal/{story.Slug}/";

    private string TopicLink(TopicDocument topic) => $"{P}/topics/{topic.Key}/";

    private string Home_() => $"{P}/";

    private string ArchiveLink() => $"{P}/journal/";

    private TopicDocument TopicOf(ArticleDocument story) =>
        site.Topics.FirstOrDefault(t => t.Key == story.Topic) ?? new TopicDocument(story.Topic, story.Topic, story.Topic, "", 0);

    private string TopicShort(ArticleDocument story) => TopicOf(story).In(Lang).Short;

    private static int ReadTime(ArticleDocument story) => ArticleService.ReadMinutes(ArticleService.Words(story));

    /// <summary>Published editions of a story (same translation key), including itself.</summary>
    private IReadOnlyList<ArticleDocument> EditionsOf(ArticleDocument story) =>
        story.TranslationKey is { } key && site.Editions is { } all
            ? all.Where(s => s.TranslationKey == key).ToList()
            : [story];

    private string Head(string title, string description, string canonicalPath, bool preview = false, IReadOnlyList<(string Lang, string Path)>? alternates = null)
    {
        var robots = S.NoIndex || preview ? "<meta name=\"robots\" content=\"noindex,nofollow\">" : "";
        var origin = S.BaseUrl ?? "";
        var canonical = S.BaseUrl is not null && !preview ? $"<link rel=\"canonical\" href=\"{E(origin + canonicalPath)}\">" : "";
        var hreflang = alternates is { Count: > 1 } && !preview
            ? string.Concat(alternates.Select(a => $"<link rel=\"alternate\" hreflang=\"{a.Lang}\" href=\"{E(origin + a.Path)}\">"))
            : "";
        var feed = $"<link rel=\"alternate\" type=\"application/rss+xml\" title=\"{E(S.Title)}\" href=\"{P}/feed.xml\">";
        return $"<!doctype html><html lang=\"{Lang}\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
            $"<title>{E(title)} — {E(S.Title)}</title><meta name=\"description\" content=\"{E(description)}\">{robots}{canonical}{hreflang}{feed}" +
            $"<meta property=\"og:title\" content=\"{E(title)}\"><meta property=\"og:description\" content=\"{E(description)}\"><meta property=\"og:type\" content=\"article\">" +
            "<meta name=\"theme-color\" content=\"#f8f7f3\"><link rel=\"stylesheet\" href=\"/boot-splash.css\"><link rel=\"stylesheet\" href=\"/site.css\">" +
            "<link rel=\"icon\" type=\"image/svg+xml\" href=\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'%3E%3Crect width='32' height='32' rx='7' fill='%23dd492f'/%3E%3Ctext x='16' y='24' font-family='Georgia' font-size='27' text-anchor='middle' fill='white'%3EO%3C/text%3E%3C/svg%3E\">" +
            $"</head><body><a class=\"skip\" href=\"#main\">{E(T("skip"))}</a>" +
            (preview ? $"<div class=\"preview-banner\" role=\"status\">{E(T("preview"))}</div>" : "");
    }

    /// <summary>Header with topics and a language switcher (same page when an edition exists, else that language's home).</summary>
    private string Header(string active = "", IReadOnlyList<(string Lang, string Path)>? alternates = null)
    {
        var logo = S.LogoSha256 is { } sha
            ? $"<img src=\"{E(site.MediaUrl(sha))}\" alt=\"Ocharo\" width=\"162\" height=\"60\">"
            : "<span class=\"footer-brand\">Ocharo</span>";
        var topics = string.Concat(site.Topics.Select(t =>
            $"<a href=\"{TopicLink(t)}\"{(active == t.Key ? " aria-current=\"page\"" : "")}>{E(t.In(Lang).Short)}</a>"));
        var available = site.AvailableLanguages ?? ["en"];
        var languages = string.Concat(SiteLanguages.All.Where(l => l != Lang && available.Contains(l)).Select(l =>
        {
            var target = alternates?.FirstOrDefault(a => a.Lang == l).Path ?? $"{SiteLanguages.Prefix(l)}/";
            return $"<a class=\"language-link\" href=\"{E(target)}\" hreflang=\"{l}\" lang=\"{l}\">{E(LanguageNames[l])}</a>";
        }));
        return $"<header class=\"site-header\"><div class=\"header-inner\"><a class=\"brand\" href=\"{Home_()}\" aria-label=\"{E(S.Title)}\">{logo}" +
            "<span class=\"brand-divider\"></span><span class=\"brand-blog\">Blog</span>" + BuildVersion.Badge + "</a>" +
            (site.Event is { } ev ? $"<a class=\"event-badge\" href=\"{E(ev.Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(ev.Label)} {ExternalArrow}</a>" : "") +
            $"<nav aria-label=\"{E(T("menu"))}\"><a href=\"{ArchiveLink()}\"{(active == "all" ? " aria-current=\"page\"" : "")}>{E(T("all"))}</a>{topics}" +
            $"<span class=\"language-switch\" aria-label=\"{E(T("languages"))}\">{languages}</span></nav></div></header>";
    }

    private string Footer()
    {
        var links = "<a href=\"/atlas/\">Atlas</a><a href=\"/wear/\">Wear</a><a href=\"/catalogue/\">Catalogue</a>" + string.Concat(S.Links.Select(l => $"<a href=\"{E(l.Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(l.Label)} {ExternalArrow}</a>"));
        return $"<footer class=\"site-footer\"><div class=\"footer-inner\"><a class=\"footer-brand\" href=\"{Home_()}\">Ocharo<span>{E(S.FooterTagline)}</span></a>" +
            $"<div class=\"footer-links\">{links}<a href=\"{P}/feed.xml\">RSS</a></div><p class=\"edition-note\">{E(S.EditionNote)}</p></div></footer></body></html>";
    }

    private string CardVisual(ArticleDocument story) =>
        story.Cover is { } cover
            ? $"<img src=\"{E(site.MediaUrl(cover.MediaSha256))}\" alt=\"{E(cover.Alt)}\" loading=\"lazy\" width=\"1086\" height=\"1448\">"
            : $"<span class=\"cover-format\">{E(story.Format ?? TopicShort(story))}</span><span class=\"cover-prompt\">{E(story.CardPrompt ?? story.Title)}</span><span class=\"cover-blog\">{E(S.Title)}</span>";

    private string Card(ArticleDocument story)
    {
        var imageClass = story.Cover is { } cover ? E(cover.CardClass ?? "") : $"type-cover {E(story.Topic)}";
        return $"<article class=\"story-card\"><a class=\"card-image {imageClass}\" href=\"{StoryLink(story)}\" aria-label=\"{E(T("readLabel"))}: {E(story.Title)}\">{CardVisual(story)}</a>" +
            $"<div class=\"card-copy\"><div class=\"story-meta\"><a href=\"{TopicLink(TopicOf(story))}\">{E(TopicShort(story))}</a><span>{ReadTime(story)} {E(T("min"))}</span></div>" +
            $"<h3><a href=\"{StoryLink(story)}\">{E(story.Title)}</a></h3><p>{E(story.Excerpt)}</p><a class=\"text-link\" href=\"{StoryLink(story)}\">{E(T("read"))} {Arrow}</a></div></article>";
    }

    private IReadOnlyList<(string Lang, string Path)> PageAlternates(string suffix) =>
        SiteLanguages.All.Where(l => (site.AvailableLanguages ?? ["en"]).Contains(l)).Select(l => (l, $"{SiteLanguages.Prefix(l)}{suffix}")).ToList();

    public string Home()
    {
        var stories = site.Stories;
        var featured = stories.FirstOrDefault(s => s.Slug == S.FeaturedSlug) ?? stories.FirstOrDefault();
        var selection = site.Topics.SelectMany(t => stories.Where(s => s.Topic == t.Key && s != featured).Take(3)).ToList();
        var hero = S.HeroSha256 is { } heroSha
            ? $"<img src=\"{E(site.MediaUrl(heroSha))}\" alt=\"{E(S.HeroAlt)}\" width=\"1122\" height=\"1402\" fetchpriority=\"high\"><span class=\"image-label\">{E(T("visual"))}</span>"
            : featured is null ? "" : CardVisual(featured);
        var featuredHtml = featured is null ? "" :
            $"<section class=\"featured\" aria-labelledby=\"feature-title\"><a class=\"featured-image\" href=\"{StoryLink(featured)}\" aria-label=\"{E(T("readLabel"))}: {E(featured.Title)}\">{hero}</a>" +
            $"<div class=\"featured-copy\"><p class=\"eyebrow\"><span class=\"feature-tag\">{E(T("featured"))}</span> {E(TopicShort(featured))}</p><h2 id=\"feature-title\"><a href=\"{StoryLink(featured)}\">{E(featured.Title)}</a></h2>" +
            $"<p>{E(featured.Excerpt)}</p><a class=\"filled-link\" href=\"{StoryLink(featured)}\">{E(T("read"))} {Arrow}</a><p class=\"feature-footnote\">{ReadTime(featured)} {E(T("min"))} · {E(featured.Format ?? T("essay"))}</p></div></section>";
        var clothTopic = site.Topics.FirstOrDefault(t => t.Key == "cloth") ?? site.Topics.LastOrDefault();
        var band = S.BandSha256 is { } bandSha && clothTopic is not null
            ? $"<section class=\"pattern-note\" aria-label=\"{E(clothTopic.In(Lang).Name)}\"><div><p class=\"eyebrow\">{E(clothTopic.In(Lang).Short)}</p><h2>{T("pattern")}</h2>" +
              $"<a class=\"text-link\" href=\"{TopicLink(clothTopic)}\">{E(T("thread"))} {Arrow}</a></div><div><img src=\"{E(site.MediaUrl(bandSha))}\" alt=\"{E(S.BandAlt)}\" loading=\"lazy\" width=\"3200\" height=\"660\"><p class=\"visual-note\">{E(T("visual"))}</p></div></section>"
            : "";
        var alternates = PageAlternates("/");
        return Head(S.HomeTitle, S.HomeDescription, Home_(), alternates: alternates) + Header(alternates: alternates) +
            $"<main id=\"main\" class=\"home-main\"><section class=\"masthead\"><p class=\"eyebrow\">{E(S.Eyebrow)}</p><h1>{E(S.Title)}<span class=\"title-dot\">.</span></h1>" +
            $"<p class=\"masthead-description\">{E(S.MastheadDescription)}</p></section>{featuredHtml}" +
            $"<section class=\"latest\" aria-labelledby=\"latest-title\"><div class=\"section-heading\"><h2 id=\"latest-title\">{E(T("think"))}</h2>" +
            $"<a class=\"text-link archive-link\" href=\"{ArchiveLink()}\">{E(string.Format(CultureInfo.InvariantCulture, T("browse"), stories.Count))} {Arrow}</a></div><div class=\"stories-grid\">{string.Concat(selection.Select(Card))}</div></section>{band}</main>" +
            Footer();
    }

    public string Archive()
    {
        var alternates = PageAlternates("/journal/");
        return Head(T("all"), $"{site.Stories.Count} {T("stories")}", ArchiveLink(), alternates: alternates) + Header("all", alternates) +
            $"<main id=\"main\" class=\"topic-main\"><a class=\"back-link\" href=\"{Home_()}\">{BackArrow} {E(T("home"))}</a><section class=\"topic-heading\"><p class=\"eyebrow\">{E(T("room"))} · {site.Stories.Count} {E(T("stories"))}</p>" +
            $"<h1>{E(T("all"))}<span class=\"title-dot\">.</span></h1><p>{E(T("allIntro"))}</p></section>" +
            $"<div class=\"stories-grid\">{string.Concat(site.Stories.Select(Card))}</div></main>" + Footer();
    }

    public string? Topic(string key)
    {
        var topic = site.Topics.FirstOrDefault(t => t.Key == key);
        if (topic is null) return null;
        var text = topic.In(Lang);
        var stories = site.Stories.Where(s => s.Topic == key).ToList();
        var alternates = PageAlternates($"/topics/{key}/");
        return Head(text.Name, text.Description, TopicLink(topic), alternates: alternates) + Header(topic.Key, alternates) +
            $"<main id=\"main\" class=\"topic-main\"><a class=\"back-link\" href=\"{ArchiveLink()}\">{BackArrow} {E(T("all"))}</a><section class=\"topic-heading\"><p class=\"eyebrow\">{E(S.Title)} · {stories.Count} {E(T("stories"))}</p>" +
            $"<h1>{E(text.Name)}<span class=\"title-dot\">.</span></h1><p>{E(text.Description)}</p></section><div class=\"stories-grid\">{string.Concat(stories.Select(Card))}</div></main>" + Footer();
    }

    /// <summary>Renders one story in its own language; <paramref name="preview"/> marks the page non-public (drafts, scheduled).</summary>
    public string Article(ArticleDocument story, bool preview = false)
    {
        if (story.Language != Lang) return new SiteRenderer(site with { Language = story.Language }).Article(story, preview);
        var topic = TopicOf(story);
        var body = new StringBuilder();
        body.Append($"<p class=\"intro\">{E(story.Intro)}</p>");
        foreach (var section in story.Sections)
        {
            body.Append($"<section><h2>{E(section.Heading)}</h2>");
            body.Append(ArticleFormatting.Render(section));
            if (section.List is { Count: > 0 } list) body.Append($"<ul>{string.Concat(list.Select(i => $"<li>{E(i)}</li>"))}</ul>");
            if (section.SourceIndexes is { Count: > 0 } indexes)
                body.Append($"<p class=\"section-sources\">{E(T("further"))}: " + string.Join(" · ", indexes.Where(i => i < story.Sources.Count).Select(i =>
                    $"<a href=\"{E(story.Sources[i].Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(story.Sources[i].Label)} {ExternalArrow}</a>")) + "</p>");
            body.Append("</section>");
        }

        if (story.Takeaway is { } takeaway)
            body.Append($"<aside class=\"reading-takeaway\" aria-label=\"{E(T("take"))}\"><p class=\"eyebrow\">{E(T("take"))}</p><p>{E(takeaway)}</p></aside>");
        if (story.ClosingQuestion is { } question)
            body.Append($"<section class=\"reader-question\"><h2>{E(T("question"))}</h2><p>{E(question)}</p></section>");
        body.Append(Pieces(story));
        if (story.Sources.Count > 0)
            body.Append($"<details class=\"article-sources\"><summary>{E(T("sources"))}</summary><ul>" + string.Concat(story.Sources.Select(s =>
                $"<li><a href=\"{E(s.Url)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(s.Label)} {ExternalArrow}</a>{(s.Supports is null ? "" : $"<p>{E(s.Supports)}</p>")}</li>")) + "</ul></details>");
        body.Append($"<p class=\"draft-note\">{E(S.Title)} · {E(S.EditionNote)}</p>");

        var cover = story.Cover is { } c
            ? $"<figure class=\"article-cover {E(c.CardClass ?? "")}\"><div class=\"cover-surface\"><img src=\"{E(site.MediaUrl(c.MediaSha256))}\" alt=\"{E(c.Alt)}\" width=\"1086\" height=\"1448\"></div><figcaption>{E(c.Caption)}</figcaption></figure>"
            : "";
        var related = Related(story);
        var relatedHtml = related.Count == 0 ? "" :
            $"<section class=\"related-stories\" aria-labelledby=\"related-title\"><div class=\"section-heading\"><h2 id=\"related-title\">{E(T("keep"))}</h2><a class=\"text-link archive-link\" href=\"{ArchiveLink()}\">{E(T("all"))} {Arrow}</a></div>" +
            $"<div class=\"stories-grid\">{string.Concat(related.Select(Card))}</div></section>";
        var published = story.PublishedAt is { } at ? $"<time datetime=\"{at:yyyy-MM-dd}\">{at.ToString("d MMMM yyyy", CultureFor(Lang))}</time>" : "";
        var editions = EditionsOf(story).Select(e => (e.Language, StoryLink(e))).ToList();
        return Head(story.SeoTitle ?? story.Title, story.SeoDescription ?? story.Excerpt ?? "", StoryLink(story), preview, editions) + Header(story.Topic, editions) +
            $"<main id=\"main\" class=\"article-main\"><a class=\"back-link\" href=\"{ArchiveLink()}\">{BackArrow} {E(T("all"))}</a><article><header class=\"article-heading\">" +
            $"<div class=\"story-meta\"><a href=\"{TopicLink(topic)}\">{E(TopicShort(story))}</a><span>{ReadTime(story)} {E(T("min"))}</span><span>{E(story.Format ?? T("essay"))}</span>{(published.Length > 0 ? $"<span>{published}</span>" : "")}</div>" +
            $"<h1>{E(story.Title)}</h1><p class=\"article-deck\">{E(story.Excerpt)}</p></header>{cover}<div class=\"article-body\">{body}</div></article>{relatedHtml}</main>" + Footer();
    }

    private static CultureInfo CultureFor(string language) => language switch
    {
        "ru" => CultureInfo.GetCultureInfo("ru-RU"),
        "uz" => CultureInfo.GetCultureInfo("uz-Latn-UZ"),
        _ => CultureInfo.InvariantCulture,
    };

    private List<ArticleDocument> Related(ArticleDocument story)
    {
        var chosen = story.RelatedSlugs.Select(slug => site.Stories.FirstOrDefault(s => s.Slug == slug)).OfType<ArticleDocument>().ToList();
        if (chosen.Count > 0) return chosen.Take(2).ToList();
        return site.Stories.Where(s => s.Slug != story.Slug && s.Topic == story.Topic)
            .Concat(site.Stories.Where(s => s.Topic != story.Topic)).Take(2).ToList();
    }

    private string Pieces(ArticleDocument story)
    {
        var pieces = story.ProductIds.Select(id => site.Products.GetValueOrDefault(id)).OfType<CatalogueProduct>().ToList();
        if (pieces.Count == 0) return "";
        var telegram = S.Links.FirstOrDefault(l => l.Url.Contains("t.me", StringComparison.OrdinalIgnoreCase))?.Url;
        static string Image(CatalogueProduct p) =>
            (p.PreviewUrl ?? p.ImageUrl) is { } src ? $"<img src=\"{E(src)}\" alt=\"{E(p.Name)}\" loading=\"lazy\">" : "";
        var cards = string.Concat(pieces.Select(p =>
            $"<figure class=\"piece-card\">{Image(p)}" +
            $"<figcaption><strong>{E(p.Name)}</strong><span>{E(p.GarmentName)}{(p.Fabric is null ? "" : " · " + E(p.Fabric))}</span>" +
            $"{Price(p.RetailPrice)}</figcaption></figure>"));
        var ask = telegram is null ? "" : $"<a class=\"text-link\" href=\"{E(telegram)}\" target=\"_blank\" rel=\"noopener noreferrer\">{E(T("ask"))} {ExternalArrow}</a>";
        return $"<section class=\"story-pieces\" aria-labelledby=\"pieces-{E(story.Slug)}\"><h2 id=\"pieces-{E(story.Slug)}\">{E(T("pieces"))}</h2><div class=\"pieces-grid\">{cards}</div>{ask}</section>";
    }

    private static string Price(long? retail) =>
        retail is { } price ? $"<span>{price.ToString("N0", CultureInfo.InvariantCulture)} UZS</span>" : "";

    /// <summary>RSS 2.0 feed of this language's published stories (absolute links need <see cref="SiteSettings.BaseUrl"/>).</summary>
    public string Feed()
    {
        var origin = S.BaseUrl ?? "";
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var channel = new XElement("channel",
            new XElement("title", S.Title),
            new XElement("link", origin + Home_()),
            new XElement("description", S.HomeDescription),
            new XElement("language", Lang),
            new XElement("lastBuildDate", site.GeneratedAt.ToString("R", CultureInfo.InvariantCulture)),
            new XElement(atom + "link", new XAttribute("href", origin + $"{P}/feed.xml"), new XAttribute("rel", "self"), new XAttribute("type", "application/rss+xml")),
            site.Stories.Take(50).Select(s => new XElement("item",
                new XElement("title", s.Title),
                new XElement("link", origin + StoryLink(s)),
                new XElement("guid", new XAttribute("isPermaLink", origin.Length > 0 ? "true" : "false"), origin + StoryLink(s)),
                new XElement("description", s.Excerpt ?? ""),
                new XElement("category", TopicOf(s).In(Lang).Name),
                s.PublishedAt is { } at ? new XElement("pubDate", at.ToString("R", CultureInfo.InvariantCulture)) : null)));
        var rss = new XElement("rss", new XAttribute("version", "2.0"), new XAttribute(XNamespace.Xmlns + "atom", atom), channel);
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + rss.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>Every language's pages; English pages always, other languages when they have published stories.</summary>
    public string Sitemap(IReadOnlyList<string> languages)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var origin = S.BaseUrl ?? "";
        var all = site.Editions ?? site.Stories;
        var paths = languages.SelectMany(l =>
                new[] { $"{SiteLanguages.Prefix(l)}/", $"{SiteLanguages.Prefix(l)}/journal/" }
                    .Concat(site.Topics.Select(t => $"{SiteLanguages.Prefix(l)}/topics/{t.Key}/")))
            .Concat(all.Select(StoryLink));
        var urlset = new XElement(ns + "urlset", paths.Select(p => new XElement(ns + "url", new XElement(ns + "loc", origin + p))));
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + urlset.ToString(SaveOptions.DisableFormatting);
    }

    public string Robots() =>
        S.NoIndex
            ? "User-agent: *\nDisallow: /\n"
            : $"User-agent: *\nAllow: /\n{(S.BaseUrl is null ? "" : $"Sitemap: {S.BaseUrl}/sitemap.xml\n")}";
}
