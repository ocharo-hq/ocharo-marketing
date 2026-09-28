namespace OcharoMarketing.Application.Providers;

/// <summary>Binds a known provider receipt to its expected source account.</summary>
public sealed record ReceiptReconciliationRequest(string AccountId, ProviderReceipt Receipt);
