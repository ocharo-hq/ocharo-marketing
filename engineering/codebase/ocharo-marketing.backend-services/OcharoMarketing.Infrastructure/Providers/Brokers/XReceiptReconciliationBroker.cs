using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Verifies a known final X post ID and author using an explicitly enabled read.</summary>
public sealed class XReceiptReconciliationBroker(HttpClient client, XProviderOptions options) : IReceiptReconciliationBroker
{
    public string Provider => "x";

    public async Task<ReceiptReconciliationResult> ReconcileAsync(
        ReceiptReconciliationRequest input, CancellationToken cancellationToken)
    {
        if (!options.EnableMetricReads || string.IsNullOrWhiteSpace(options.AccessToken))
            return Result(BrokerOutcome.Rejected, "not_enabled", "X receipt reads require separate explicit read enablement and credentials.");
        var receipt = input.Receipt;
        if (!ProviderInput.IsNumericId(options.UserId) || input.AccountId != options.UserId
            || receipt is null || receipt.Provider != Provider
            || receipt.RelatedIds?.GetValueOrDefault("account_id") != input.AccountId)
            return Result(BrokerOutcome.Rejected, "receipt_mismatch", "Use a receipt bound to the configured source account.");
        if (!ProviderInput.IsNumericId(receipt.ExternalId)
            || receipt.Stage != "published" && (receipt.Stage != "publication_unknown"
                || receipt.RelatedIds?.GetValueOrDefault("post_id") != receipt.ExternalId))
            return Result(BrokerOutcome.Unsupported, "known_post_required",
                "No known final X post ID is available; a dispatch or upload marker cannot be reconciled as a post.");
        if (receipt.RelatedIds?.TryGetValue("post_id", out var knownPost) == true && knownPost != receipt.ExternalId)
            return Result(BrokerOutcome.Rejected, "receipt_mismatch", "Stored final post identities do not agree.");

        using var request = ProviderHttpTransport.Create(HttpMethod.Get,
            "https://api.x.com/2/tweets/" + receipt.ExternalId + "?tweet.fields=author_id", options.AccessToken);
        var reply = await ProviderHttpTransport.SendAsync(client, request, false, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded)
            return Result(reply.Outcome, reply.Code, reply.Message);
        if (reply.Json is not { } root || ProviderReadEvidence.HasErrors(root)
            || !ProviderReadEvidence.TryObject(root, "data", out var data))
            return Result(BrokerOutcome.Rejected, "receipt_unavailable",
                "X did not return complete final post evidence; no publication was attempted.");
        if (ProviderHttpTransport.Id(data, "id") != receipt.ExternalId
            || ProviderHttpTransport.Id(data, "author_id") != input.AccountId)
            return Result(BrokerOutcome.Rejected, "identity_mismatch", "X returned a different post or source account.");

        var related = receipt.RelatedIds is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(receipt.RelatedIds);
        related["account_id"] = input.AccountId;
        related["post_id"] = receipt.ExternalId;
        return new(BrokerOutcome.Succeeded, Provider, "receipt_verified",
            "Existing X post identity verified by a read; no publication was attempted.",
            receipt with { Stage = "published", RelatedIds = related });
    }

    private ReceiptReconciliationResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
