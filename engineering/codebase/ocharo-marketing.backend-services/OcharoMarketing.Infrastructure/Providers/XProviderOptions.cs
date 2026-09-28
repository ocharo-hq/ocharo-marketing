namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Separately opts in authorized X publication and potentially billable metric reads.</summary>
public sealed class XProviderOptions
{
    public bool Enabled { get; set; }
    public bool EnableMetricReads { get; set; }
    public string? AccessToken { get; set; }
    public string? UserId { get; set; }
}
