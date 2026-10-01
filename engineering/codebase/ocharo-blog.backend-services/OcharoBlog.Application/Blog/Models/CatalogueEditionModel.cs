namespace OcharoBlog.Application.Blog.Models;

/// <summary>A single public release read. Products and its fixed-page edition share the same release boundary.</summary>
public sealed record CatalogueSnapshot(IReadOnlyList<CatalogueProduct> Products, CatalogueEdition? Edition = null, IReadOnlyList<string>? AvailableLocales = null, IReadOnlyList<CatalogueCollection>? Catalogues = null);

/// <summary>Platform-owned, explicitly released presentation. Marketing does not author or mutate its pages.</summary>
public sealed record CatalogueEdition(string Id, string BrandId, string SourceDesignId, int SourceRevision,
    string Title, string Locale, string TemplateId, int PageCount, DateTimeOffset ReleasedAt,
    string PdfUrl, IReadOnlyList<CatalogueEditionPage> Pages);

public sealed record CatalogueEditionPage(string Id, int Number, string Title, decimal WidthMm, decimal HeightMm,
    IReadOnlyList<string> ProductIds, string ImageUrl, string PdfUrl, string PngUrl);

/// <summary>A named released catalogue may expose a responsive web version, a fixed PDF version, or both.</summary>
public sealed record CatalogueCollection(string Id, string Slug, string Title, string? Description, string Locale,
    DateTimeOffset UpdatedAt, IReadOnlyList<string> AvailableLocales, CatalogueWeb? Web, CatalogueEdition? Pdf);

public sealed record CatalogueWeb(string Theme, IReadOnlyList<CatalogueWebPage> Pages, string MediaGroup = "all",
    CataloguePriceDisplay? PriceDisplay = null, IReadOnlyDictionary<string, string>? Copy = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? ProductMedia = null);
public sealed record CataloguePriceDisplay(bool Show = true, bool ShowCurrency = true, bool ShowQuantity = true,
    bool ShowRetail = true, bool ShowWholesale = true);
public sealed record CatalogueWebPage(string Id, string Title, string Layout, IReadOnlyList<string> ProductIds);
