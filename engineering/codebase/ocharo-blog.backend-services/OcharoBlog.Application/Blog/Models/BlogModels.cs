using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Models;

/// <summary>A story row for the admin list.</summary>
public sealed record ArticleSummary(
    Guid Id,
    string Slug,
    string Topic,
    string Status,
    string Title,
    string? Excerpt,
    int ReadMinutes,
    int Words,
    bool HasCover,
    int ProductCount,
    DateTimeOffset? PublishAt,
    DateTimeOffset? PublishedAt,
    int Revision,
    DateTimeOffset UpdatedAt,
    string Language = "en",
    string? TranslationKey = null,
    string Publication = "blog",
    string Kind = "story");

/// <summary>A story with its revision stamp.</summary>
public sealed record ArticleDetail(Guid Id, ArticleDocument Document, int Revision, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int ReadMinutes, int Words);

/// <summary>Create/update payload; lifecycle changes use the dedicated commands.</summary>
public sealed record ArticleInput
{
    public int? ExpectedRevision { get; init; }
    public required string Slug { get; init; }
    public required string Topic { get; init; }
    public string Language { get; init; } = "en";
    public string Publication { get; init; } = "blog";
    public string Kind { get; init; } = "story";
    public IReadOnlyList<string>? Places { get; init; }
    public IReadOnlyList<string>? Techniques { get; init; }
    public IReadOnlyList<string>? Motifs { get; init; }
    public IReadOnlyList<string>? Occasions { get; init; }
    public IReadOnlyList<string>? TextileSlugs { get; init; }
    public TextileDetails? Textile { get; init; }
    public ImageProvenance? ImageProvenance { get; init; }
    public required string Title { get; init; }
    public string? Excerpt { get; init; }
    public string? Intro { get; init; }
    public string? Format { get; init; }
    public string? ReaderPromise { get; init; }
    public IReadOnlyList<ArticleSection>? Sections { get; init; }
    public string? Takeaway { get; init; }
    public string? ClosingQuestion { get; init; }
    public IReadOnlyList<ArticleSource>? Sources { get; init; }
    public IReadOnlyList<string>? RelatedSlugs { get; init; }
    public string? CardPrompt { get; init; }
    public ArticleCover? Cover { get; init; }
    public IReadOnlyList<string>? ProductIds { get; init; }
    public string? SeoTitle { get; init; }
    public string? SeoDescription { get; init; }
    public string? TranslationKey { get; init; }
    public string? ChangeSummary { get; init; }
}

/// <summary>Creates a draft edition of a story in another language.</summary>
public sealed record TranslateInput(string Language);

/// <summary>A revision-checked lifecycle command.</summary>
public sealed record LifecycleInput(int ExpectedRevision, DateTimeOffset? PublishAt = null);

/// <summary>A saved revision in the history list.</summary>
public sealed record RevisionSummary(int Revision, DateTimeOffset SavedAt, string Summary);

/// <summary>Stored media as returned to the admin.</summary>
public sealed record MediaView(string Sha256, string FileName, string ContentType, long ByteLength, int? Width, int? Height, string Url, string? PreviewUrl);

/// <summary>A catalogue product as the blog needs it (from the Ocharo Catalogue public feed).</summary>
public sealed record CatalogueProduct(string Id, string Name, string GarmentName, string? Fabric, long? RetailPrice, string? ImageUrl, string? PreviewUrl,
    string? Slug = null, string? Description = null, string? Category = null, string? Availability = null,
    IReadOnlyList<string>? Sizes = null, IReadOnlyList<string>? CollectionIds = null,
    decimal? PriceAmount = null, string? Currency = null, string? ImageAlt = null,
    string? ImageKind = null, string? ImageCredit = null, string? ImageRights = null,
    IReadOnlyList<CatalogueImage>? Images = null);

public sealed record CatalogueImage(string Url, string Alt, string? Kind, string? Credit, string? Rights);


/// <summary>Settings plus revision stamp.</summary>
public sealed record SettingsEnvelope(SiteSettings Settings, int Revision);

/// <summary>Whether the Brand service currently supplies the site's logo and links (Studio notice).</summary>
public sealed record BrandStatus(bool Connected, int Release, string? LogoSha256, IReadOnlyList<SiteLink> Links);
