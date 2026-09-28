namespace OcharoMarketing.Application.Providers;

/// <summary>Carries approved content and durable checkpoints independently of business entities.</summary>
/// <remarks>The caller validates approval and messaging permission before invoking a broker.</remarks>
public sealed record SocialPublishRequest(
    string DestinationId,
    string Text,
    IReadOnlyList<SocialMediaAsset>? Media = null,
    MessagingTemplate? Template = null,
    bool CustomerServiceWindowConfirmed = false,
    bool RecipientPermissionConfirmed = false,
    ProviderReceipt? ResumeReceipt = null,
    Func<ProviderReceipt, CancellationToken, Task>? SaveCheckpointAsync = null,
    string? AccountId = null);
