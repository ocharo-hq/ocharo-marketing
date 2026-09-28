namespace OcharoMarketing.Application.Providers;

/// <summary>Separates configured access from provider-observed identity and capability evidence.</summary>
public sealed record ProviderVerificationResult(
    BrokerOutcome Outcome, string Provider, string Code, string Message, ProviderIdentity? Identity = null);
