namespace OcharoMarketing.Infrastructure.Marketing;

/// <summary>Configures bounded local storage and explicit provider execution opt-ins.</summary>
public sealed class MarketingOptions
{
    public bool LocalPilot { get; set; } = true;
    public bool EnableLivePublishing { get; set; }
    public string DataDirectory { get; set; } = "App_Data";
    public long MaximumAssetBytes { get; set; } = 26_214_400;
    /// <summary>Ocharo Catalogue origin whose public feed supplies products (e.g. https://localhost:8278).</summary>
    public string? PlatformBaseUrl { get; set; }
    public string BrandSlug { get; set; } = "ocharo";
    public string? CatalogueBaseUrl { get; set; }
    /// <summary>Ocharo Blog origin whose RSS feed supplies stories to announce (e.g. https://localhost:8280).</summary>
    public string? BlogBaseUrl { get; set; }
}
