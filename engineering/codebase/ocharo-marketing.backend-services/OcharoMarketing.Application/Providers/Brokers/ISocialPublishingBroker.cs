namespace OcharoMarketing.Application.Providers;

/// <summary>Integrates an external social publication or customer messaging capability.</summary>
/// <remarks>Ambiguous results require reconciliation before another publication attempt.</remarks>
public interface ISocialPublishingBroker
{
    string Provider { get; }
    IReadOnlySet<string> Capabilities { get; }
    Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken cancellationToken);
}
