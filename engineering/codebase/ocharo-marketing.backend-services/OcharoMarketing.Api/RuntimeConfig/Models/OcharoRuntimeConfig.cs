using System.Text.Json.Serialization;

namespace OcharoMarketing.Api.RuntimeConfig.Models;

/// <summary>The runtime-config document the Marketing SPA reads: each Ocharo app address under the key the SPA
/// declares, left out when unset. The keys are the SPA's own names, so they keep their casing on the wire.</summary>
public sealed record OcharoRuntimeConfig
{
    [JsonPropertyName("OCHARO_DESK_URL")]
    public string? DeskUrl { get; init; }

    [JsonPropertyName("OCHARO_CATALOGUE_URL")]
    public string? CatalogueUrl { get; init; }

    [JsonPropertyName("OCHARO_STUDIO_URL")]
    public string? StudioUrl { get; init; }

    [JsonPropertyName("OCHARO_MARKETING_URL")]
    public string? MarketingUrl { get; init; }

    [JsonPropertyName("OCHARO_EDITORIAL_URL")]
    public string? EditorialUrl { get; init; }

    [JsonPropertyName("OCHARO_BRAND_URL")]
    public string? BrandUrl { get; init; }

    [JsonPropertyName("OCHARO_ATLAS_URL")]
    public string? AtlasUrl { get; init; }

    [JsonPropertyName("OCHARO_WEAR_URL")]
    public string? WearUrl { get; init; }

    [JsonPropertyName("OCHARO_BLOG_URL")]
    public string? BlogUrl { get; init; }
}
