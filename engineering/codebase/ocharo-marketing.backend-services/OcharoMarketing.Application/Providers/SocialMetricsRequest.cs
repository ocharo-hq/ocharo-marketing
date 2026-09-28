namespace OcharoMarketing.Application.Providers;

/// <summary>Requests supported post counters bound to an authorized account and final receipt.</summary>
public sealed record SocialMetricsRequest(
    string AccountId, ProviderReceipt Receipt, IReadOnlyList<string>? Metrics = null);
