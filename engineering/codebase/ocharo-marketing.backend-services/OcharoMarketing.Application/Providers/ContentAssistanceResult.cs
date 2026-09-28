namespace OcharoMarketing.Application.Providers;

/// <summary>Reports suggested text or an explicit failure without fabricated content.</summary>
public sealed record ContentAssistanceResult(
    BrokerOutcome Outcome,
    string Provider,
    string? Text,
    string Code,
    string Message,
    string? Model = null,
    ProviderReceipt? Receipt = null);
