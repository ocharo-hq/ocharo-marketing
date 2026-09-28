namespace OcharoMarketing.Application.Providers;

/// <summary>Identifies a completed provider stage for durable reconciliation.</summary>
public sealed record ProviderReceipt(
    string Provider,
    string Stage,
    string ExternalId,
    IReadOnlyDictionary<string, string>? RelatedIds = null);
