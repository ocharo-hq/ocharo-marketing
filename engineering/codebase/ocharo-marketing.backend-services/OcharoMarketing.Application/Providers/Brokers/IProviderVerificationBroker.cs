namespace OcharoMarketing.Application.Providers;

/// <summary>Verifies configured provider identity and destination without publishing content.</summary>
public interface IProviderVerificationBroker
{
    string Provider { get; }
    Task<ProviderVerificationResult> VerifyAsync(ProviderVerificationRequest input, CancellationToken cancellationToken);
}
