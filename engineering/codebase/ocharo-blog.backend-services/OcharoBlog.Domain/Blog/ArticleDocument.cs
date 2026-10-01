namespace OcharoBlog.Domain.Blog;

/// <summary>One blog story as a versioned document; every save stores a revision.</summary>
public sealed record ArticleDocument
{
    /// <summary>Public address segment: <c>/journal/{slug}/</c>.</summary>
    public required string Slug { get; init; }

    /// <summary>Topic key (see <see cref="TopicDocument"/>).</summary>
    public required string Topic { get; init; }

    /// <summary><see cref="ArticleStatus"/>.</summary>
    public string Status { get; init; } = ArticleStatus.Draft;

    /// <summary>Content language (<c>en</c>, <c>uz</c>, <c>ru</c>).</summary>
    public string Language { get; init; } = "en";

    /// <summary>The independently presented publication; existing documents remain Blog.</summary>
    public string Publication { get; init; } = "blog";
    public string Kind { get; init; } = "story";
    public IReadOnlyList<string> Places { get; init; } = [];
    public IReadOnlyList<string> Techniques { get; init; } = [];
    public IReadOnlyList<string> Motifs { get; init; } = [];
    public IReadOnlyList<string> Occasions { get; init; } = [];
    public IReadOnlyList<string> TextileSlugs { get; init; } = [];
    public TextileDetails? Textile { get; init; }
    public ImageProvenance? ImageProvenance { get; init; }

    public required string Title { get; init; }
    public string? Excerpt { get; init; }
    public string? Intro { get; init; }

    /// <summary>Editorial format label, e.g. "Wardrobe puzzle".</summary>
    public string? Format { get; init; }

    /// <summary>What the reader should be able to do afterwards (internal brief; not rendered).</summary>
    public string? ReaderPromise { get; init; }

    public IReadOnlyList<ArticleSection> Sections { get; init; } = [];
    public string? Takeaway { get; init; }
    public string? ClosingQuestion { get; init; }
    public IReadOnlyList<ArticleSource> Sources { get; init; } = [];

    /// <summary>Exactly-chosen "keep reading" stories; empty means automatic.</summary>
    public IReadOnlyList<string> RelatedSlugs { get; init; } = [];

    /// <summary>Short line on the typographic card cover.</summary>
    public string? CardPrompt { get; init; }

    /// <summary>Optional photographic cover.</summary>
    public ArticleCover? Cover { get; init; }

    /// <summary>Catalogue product ids shown under the story ("Pieces in this story").</summary>
    public IReadOnlyList<string> ProductIds { get; init; } = [];

    /// <summary>When a scheduled story goes live (UTC).</summary>
    public DateTimeOffset? PublishAt { get; init; }

    /// <summary>First publication time (UTC); kept when a story is republished.</summary>
    public DateTimeOffset? PublishedAt { get; init; }

    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }

    /// <summary>Groups language editions of one story (hreflang alternates); null for a standalone story.</summary>
    public string? TranslationKey { get; init; }
}

/// <summary>Public site languages; English lives at the root, others under <c>/{code}/</c>.</summary>
public static class SiteLanguages
{
    public static readonly IReadOnlyList<string> All = ["en", "uz", "ru"];

    public static string Prefix(string language) => language == "en" ? "" : "/" + language;
}

/// <summary>A headed section: paragraphs, an optional list, and indexes into the story's sources.</summary>
public sealed record ArticleSection(string Heading, IReadOnlyList<string> Paragraphs, IReadOnlyList<string>? List, IReadOnlyList<int>? SourceIndexes,
    string? Format = null);

/// <summary>A cited source; <see cref="Supports"/> explains what claim it backs.</summary>
public sealed record ArticleSource(string Label, string Url, string? Supports);

/// <summary>A photographic cover from the media store.</summary>
public sealed record ArticleCover(string MediaSha256, string Alt, string Caption, string? CardClass);

/// <summary>Story lifecycle. Only published stories reach the public site, feed, sitemap and export.</summary>
public static class ArticleStatus
{
    public const string Draft = "draft";
    public const string Scheduled = "scheduled";
    public const string Published = "published";
    public const string Archived = "archived";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Draft, Scheduled, Published, Archived };
}

/// <summary>A reading section of the site (<c>/topics/{key}/</c>); <see cref="Translations"/> holds uz/ru labels.</summary>
public sealed record TopicDocument(string Key, string Name, string Short, string Description, int SortOrder,
    IReadOnlyDictionary<string, TopicText>? Translations = null)
{
    /// <summary>The topic's labels in a language, falling back to English.</summary>
    public TopicText In(string language) =>
        language != "en" && Translations?.GetValueOrDefault(language) is { } text && !string.IsNullOrWhiteSpace(text.Name)
            ? text
            : new TopicText(Name, Short, Description);
}

/// <summary>Topic labels in one language.</summary>
public sealed record TopicText(string Name, string Short, string Description);

/// <summary>Public-site presentation settings.</summary>
public sealed record SiteSettings
{
    public string Title { get; init; } = "Ocharo Blog";
    public string Eyebrow { get; init; } = "Clothes. Curiosity. Everyday life.";
    public string MastheadDescription { get; init; } = "For the clothes you keep thinking about, and the questions a photograph cannot answer.";
    public string HomeTitle { get; init; } = "Stories about clothes and the lives we wear them in";
    public string HomeDescription { get; init; } = "Wardrobe puzzles, styling experiments, and textile stories from the Ocharo Blog.";
    public string FooterTagline { get; init; } = "Contemporary Uzbek clothing";
    public string EditionNote { get; init; } = "Preview edition · Editorial drafts";

    /// <summary>Keeps search engines out until the owner decides to launch.</summary>
    public bool NoIndex { get; init; } = true;

    /// <summary>Absolute origin used in the feed, sitemap and canonical links (e.g. https://blog.example).</summary>
    public string? BaseUrl { get; init; }

    public IReadOnlyList<SiteLink> Links { get; init; } =
    [
        new("Instagram", "https://www.instagram.com/ocharostile/"),
        new("Contact on Telegram", "https://t.me/Ocharo_Style"),
    ];

    public string? FeaturedSlug { get; init; }
    public string? LogoSha256 { get; init; }
    public string? HeroSha256 { get; init; }
    public string HeroAlt { get; init; } = "A collection visualisation of patterned Ocharo nimcha designs on mannequins.";
    public string? BandSha256 { get; init; }
    public string BandAlt { get; init; } = "A visual study bringing different patterned nimcha designs together.";
}

/// <summary>A footer link.</summary>
public sealed record SiteLink(string Label, string Url);

/// <summary>Object-specific facts; uncertainty is retained rather than inferred from appearance.</summary>
public sealed record TextileDetails(
    string ObjectId, string Collection, string Place, string Period, string Materials,
    string Technique, string? Dimensions, string? Maker, string? AttributionNote);

/// <summary>Image rights and provenance travel with editorial content through every revision/export.</summary>
public sealed record ImageProvenance(string Kind, string Credit, string Rights, string SourceUrl);

public static class Publications
{
    public static readonly IReadOnlySet<string> All = new HashSet<string> { "blog", "atlas", "wear" };
    public static readonly IReadOnlySet<string> Kinds = new HashSet<string> { "story", "textile", "archive", "guide" };
    public static string Path(ArticleDocument d) => d.Publication switch
    {
        "atlas" => $"/atlas/{(d.Kind is "textile" or "archive" ? "textiles" : "stories")}/{d.Slug}/",
        "wear" => $"/wear/guides/{d.Slug}/",
        _ => $"{SiteLanguages.Prefix(d.Language)}/journal/{d.Slug}/",
    };
}
