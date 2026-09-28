using System.Text.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Reads only observed public post counters after matching configured account and owned receipt.</summary>
public sealed class XSocialMetricsBroker(
    HttpClient client, XProviderOptions options, TimeProvider? timeProvider = null) : ISocialMetricsBroker
{
    private const long MaximumMeaningfulCounter = 9_007_199_254_740_991;
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private static readonly IReadOnlyDictionary<string, string> Fields = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["likes"] = "like_count", ["replies"] = "reply_count", ["reposts"] = "retweet_count",
        ["quotes"] = "quote_count", ["bookmarks"] = "bookmark_count", ["impressions"] = "impression_count"
    };

    public string Provider => "x";

    public async Task<SocialMetricsResult> ReadAsync(SocialMetricsRequest input, CancellationToken cancellationToken)
    {
        if (input.Metrics is { Count: 0 or > 6 })
            return Empty([], BrokerOutcome.Rejected, "invalid_metrics", "Choose bounded supported public metric names.", "invalid_input");
        var names = input.Metrics?.Distinct(StringComparer.Ordinal).ToArray() ?? Fields.Keys.ToArray();
        if (names.Any(name => string.IsNullOrEmpty(name) || name.Length > 40 || !name.All(char.IsAsciiLetter)))
            return Empty([], BrokerOutcome.Rejected, "invalid_metrics", "Choose bounded supported public metric names.", "invalid_input");
        if (names.Any(name => !Fields.ContainsKey(name)))
            return Empty(names, BrokerOutcome.Unsupported, "metric_not_supported", "This adapter reads supported public post counters only.", "not_supported");
        if (!options.EnableMetricReads || string.IsNullOrWhiteSpace(options.AccessToken))
            return Empty(names, BrokerOutcome.Rejected, "not_enabled", "X metric reads require separate explicit enablement and credentials.", "not_configured");
        if (!ProviderInput.IsNumericId(options.UserId) || input.AccountId != options.UserId
            || input.Receipt is null || input.Receipt.Provider != Provider || input.Receipt.Stage != "published"
            || !ProviderInput.IsNumericId(input.Receipt.ExternalId)
            || input.Receipt.RelatedIds?.TryGetValue("account_id", out var receiptAccount) == true && receiptAccount != input.AccountId)
            return Empty(names, BrokerOutcome.Rejected, "receipt_mismatch", "Use a final X receipt belonging to the configured account.", "identity_mismatch");

        using var request = ProviderHttpTransport.Create(HttpMethod.Get,
            "https://api.x.com/2/tweets/" + input.Receipt.ExternalId + "?tweet.fields=author_id,public_metrics", options.AccessToken);
        var reply = await ProviderHttpTransport.SendAsync(client, request, false, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded)
            return Empty(names, reply.Outcome, reply.Code, reply.Message, reply.Code switch
            {
                "http_401" or "http_403" => "permission_missing", "http_404" => "not_found",
                "http_429" => "rate_limited", _ => "read_failed"
            });
        if (reply.Json is not { } root || !ProviderReadEvidence.TryObject(root, "data", out var data))
            return Empty(names, BrokerOutcome.Rejected, "post_unavailable", "X did not return usable owned post data.", "not_returned",
                reply.Json is { } errorRoot && ProviderReadEvidence.HasErrors(errorRoot));
        if (ProviderHttpTransport.Id(data, "id") != input.Receipt.ExternalId || ProviderHttpTransport.Id(data, "author_id") != input.AccountId)
            return Empty(names, BrokerOutcome.Rejected, "identity_mismatch", "X returned a different post or author.", "identity_mismatch",
                ProviderReadEvidence.HasErrors(root));

        var hasMetrics = ProviderReadEvidence.TryObject(data, "public_metrics", out var metrics);
        var observedAt = clock.GetUtcNow();
        var observations = names.Select(name => ReadCounter(name, hasMetrics ? metrics : null, observedAt)).ToArray();
        var partial = ProviderReadEvidence.HasErrors(root);
        return new(BrokerOutcome.Succeeded, Provider,
            partial || observations.Any(item => item.Value is null) ? "partial_metrics" : "observed",
            partial ? "Available counters retained; X reported partial errors."
                : "Only returned, valid counters are recorded as observations.", observations, partial);
    }

    private static SocialMetricObservation ReadCounter(string name, JsonElement? metrics, DateTimeOffset observedAt)
    {
        if (metrics is not { } counters || !counters.TryGetProperty(Fields[name], out var value))
            return new(name, null, "not_returned", null, "X did not return this counter.");
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var count)
            || count < 0 || count > MaximumMeaningfulCounter)
            return new(name, null, "invalid_value", null, "X returned an invalid or out-of-range counter.");
        return new(name, count, "observed", observedAt);
    }

    private SocialMetricsResult Empty(IEnumerable<string> names, BrokerOutcome outcome, string code, string message,
        string availability, bool partial = false)
        => new(outcome, Provider, code, message,
            names.Select(name => new SocialMetricObservation(name, null, availability, null, message)).ToArray(), partial);
}
