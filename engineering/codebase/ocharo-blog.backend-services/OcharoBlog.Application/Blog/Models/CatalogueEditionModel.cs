namespace OcharoBlog.Application.Blog.Models;

/// <summary>A single public release read. Products and its fixed-page edition share the same release boundary.</summary>
public sealed record CatalogueSnapshot(IReadOnlyList<CatalogueProduct> Products, CatalogueEdition? Edition = null, IReadOnlyList<string>? AvailableLocales = null);

/// <summary>Platform-owned, explicitly released presentation. Marketing does not author or mutate its pages.</summary>
public sealed record CatalogueEdition(string Id, string BrandId, string SourceDesignId, int SourceRevision,
    string Title, string Locale, string TemplateId, int PageCount, DateTimeOffset ReleasedAt,
    string PdfUrl, IReadOnlyList<CatalogueEditionPage> Pages);

public sealed record CatalogueEditionPage(string Id, int Number, string Title, decimal WidthMm, decimal HeightMm,
    IReadOnlyList<string> ProductIds, string ImageUrl, string PdfUrl, string PngUrl);
