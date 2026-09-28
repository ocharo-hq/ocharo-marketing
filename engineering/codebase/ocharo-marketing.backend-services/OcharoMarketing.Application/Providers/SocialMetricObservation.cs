namespace OcharoMarketing.Application.Providers;

/// <summary>Distinguishes an actual observed number from unavailable, absent or invalid evidence.</summary>
public sealed record SocialMetricObservation(
    string Name, decimal? Value, string Availability, DateTimeOffset? ObservedAt, string? Reason = null);
