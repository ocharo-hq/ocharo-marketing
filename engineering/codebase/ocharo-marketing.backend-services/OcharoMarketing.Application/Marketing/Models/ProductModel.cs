namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Product data for the local marketing workflow.</summary>
public sealed record ProductModel(string Id, string Name, string Category, string Material, string Description, string HeroAssetId, string[] AssetIds, string[] Sizes, decimal? Price, string Currency, string ReviewStatus, string SourceRevision, string[] Issues,
    IReadOnlyDictionary<string, ProductCopyModel>? Copy = null, decimal? WholesalePrice = null,
    string? Availability = null, string[]? AvailableSizes = null);

/// <summary>Localized product copy imported from Ocharo Catalogue; <see cref="Status"/> is draft until the owner approves it.</summary>
public sealed record ProductCopyModel(string Status, string? Name, string? Fabric, string? Decoration, string? Description);
