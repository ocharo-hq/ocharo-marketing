using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Verifies a configured user-context identity without publishing or reading post counters.</summary>
public sealed class XAccountVerificationBroker(
    HttpClient client, XProviderOptions options, TimeProvider? timeProvider = null) : IProviderVerificationBroker
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    public string Provider => "x";

    public async Task<ProviderVerificationResult> VerifyAsync(ProviderVerificationRequest input, CancellationToken cancellationToken)
    {
        if ((!options.Enabled && !options.EnableMetricReads) || string.IsNullOrWhiteSpace(options.AccessToken))
            return Result(BrokerOutcome.Rejected, "not_enabled", "X identity verification requires explicitly enabled publishing or metric reads.");
        if (!ProviderInput.IsNumericId(options.UserId) || input.DestinationId != options.UserId
            || input.ExpectedAccountId is not null && input.ExpectedAccountId != options.UserId)
            return Result(BrokerOutcome.Rejected, "account_mismatch", "X verification must match the configured user destination.");

        using var request = ProviderHttpTransport.Create(HttpMethod.Get, "https://api.x.com/2/users/me", options.AccessToken);
        var reply = await ProviderHttpTransport.SendAsync(client, request, false, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded) return Result(reply.Outcome, reply.Code, reply.Message);
        if (reply.Json is not { } root || ProviderReadEvidence.HasErrors(root)
            || !ProviderReadEvidence.TryObject(root, "data", out var data)
            || ProviderHttpTransport.Id(data, "id") is not { } id || !ProviderInput.IsNumericId(id))
            return Result(BrokerOutcome.Rejected, "identity_unavailable", "X did not return an unambiguous user identity.");
        if (id != options.UserId)
            return Result(BrokerOutcome.Rejected, "account_mismatch", "X credentials identify a different user.");

        var capabilities = new List<string>();
        if (options.Enabled) capabilities.AddRange(["text", "photo"]);
        if (options.EnableMetricReads) capabilities.Add("public-metrics");
        return new(BrokerOutcome.Succeeded, Provider, "verified", "Configured X user identity verified.",
            new(id, id, ProviderReadEvidence.DisplayName(ProviderHttpTransport.Text(data, "name"),
                ProviderReadEvidence.DisplayName(ProviderHttpTransport.Text(data, "username"), id)),
                capabilities, clock.GetUtcNow()));
    }

    private ProviderVerificationResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
