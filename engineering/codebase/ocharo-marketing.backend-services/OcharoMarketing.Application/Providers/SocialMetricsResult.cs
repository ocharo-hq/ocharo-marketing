namespace OcharoMarketing.Application.Providers;

/// <summary>Preserves available observations and safe partial-response evidence independently of publication.</summary>
public sealed record SocialMetricsResult(
    BrokerOutcome Outcome, string Provider, string Code, string Message,
    IReadOnlyList<SocialMetricObservation> Observations, bool HasPartialErrors = false);
