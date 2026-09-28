namespace OcharoMarketing.Application.Providers;

/// <summary>Reports observed receipt identity, or explicit unresolved availability.</summary>
public sealed record ReceiptReconciliationResult(
    BrokerOutcome Outcome, string Provider, string Code, string Message, ProviderReceipt? Receipt = null);
