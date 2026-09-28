namespace OcharoMarketing.Application.Providers;

/// <summary>Reports provider acceptance separately from delivery and engagement.</summary>
public sealed record SocialPublishResult(
    BrokerOutcome Outcome,
    string Provider,
    string Code,
    string Message,
    ProviderReceipt? Receipt = null);
