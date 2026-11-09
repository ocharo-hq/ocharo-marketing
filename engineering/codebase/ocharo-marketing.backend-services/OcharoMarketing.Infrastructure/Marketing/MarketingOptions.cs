namespace OcharoMarketing.Infrastructure.Marketing;

/// <summary>Holds the <c>Marketing</c> section: bounded local storage, the Ocharo origins Marketing reads, and
/// explicit provider execution opt-ins.</summary>
/// <remarks>The organization this instance serves (<c>Marketing:OrganizationId</c>) binds to the API's access
/// options.</remarks>
public sealed class MarketingOptions
{
    /// <summary>The section name.</summary>
    public const string SectionName = "Marketing";

    /// <summary>Turns live delivery to the social providers on; off, plans stay plans. Read live: a reload
    /// applies at once.</summary>
    public bool EnableLivePublishing { get; set; }

    /// <summary>The folder holding the SQLite store, local blobs and the Data Protection keys; relative to the working
    /// directory.</summary>
    public string DataDirectory { get; set; } = "App_Data";

    /// <summary>The largest asset a person may upload, in bytes; above zero.</summary>
    public long MaximumAssetBytes { get; set; } = 26_214_400;

    /// <summary>Ocharo Catalogue origin whose public feed supplies products (e.g. https://localhost:8278).</summary>
    public string? PlatformBaseUrl { get; set; }

    /// <summary>The brand whose catalogue the public inquiry form checks products against.</summary>
    public string BrandSlug { get; set; } = "ocharo";

    /// <summary>Ocharo Catalogue origin the catalogue sync reads (e.g. https://localhost:8278).</summary>
    public string? CatalogueBaseUrl { get; set; }

    /// <summary>Ocharo Blog origin whose RSS feed supplies stories to announce (e.g. https://localhost:8280).</summary>
    public string? BlogBaseUrl { get; set; }

    /// <summary>Lets a person verify a provider account against the provider. Read live.</summary>
    public bool EnableAccountVerification { get; set; }

    /// <summary>Lets the desk read paid provider metrics. Read live.</summary>
    public bool EnableMetricReads { get; set; }

    /// <summary>The most provider metric reads per hour, from 0 to 100. Read live.</summary>
    public int MaximumMetricReadsPerHour { get; set; } = 10;

    /// <summary>The shortest wait between two metric reads of one publication, in seconds; at least 1. Read
    /// live.</summary>
    public int MetricRefreshCooldownSeconds { get; set; } = 60;

    /// <summary>Each provider's connection revision (<c>telegram</c>, <c>x</c>); raising one revokes the authorizations
    /// stamped with the previous. Unset is 1. Read live.</summary>
    public Dictionary<string, int> ConnectionRevisions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The time between unattended automation passes; zero or less turns the passes off.</summary>
    public TimeSpan AutomationInterval { get; set; } = TimeSpan.FromMinutes(5);
}
