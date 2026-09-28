namespace OcharoMarketing.Application.Providers;

/// <summary>Reports generated artifacts or an explicit provider limitation.</summary>
public sealed record ImageGenerationResult(
    BrokerOutcome Outcome,
    string Provider,
    IReadOnlyList<GeneratedImage> Images,
    string Code,
    string Message,
    ProviderReceipt? Receipt = null,
    string? Model = null);
