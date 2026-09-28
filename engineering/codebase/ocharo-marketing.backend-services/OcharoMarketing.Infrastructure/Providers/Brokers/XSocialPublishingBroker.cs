using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Publishes X text or one image with identity-bound, expiring upload checkpoints.</summary>
/// <remarks>Every side effect is checkpointed before dispatch; partial or missing receipts prohibit blind replay.</remarks>
public sealed class XSocialPublishingBroker(
    HttpClient client, XProviderOptions options, TimeProvider? timeProvider = null) : ISocialPublishingBroker
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    public string Provider => "x";
    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string> { "text", "photo" };

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest input, CancellationToken cancellationToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.AccessToken))
            return Result(BrokerOutcome.Rejected, "not_enabled", "X publication requires explicit enablement and user credentials.");
        if (!ProviderInput.IsNumericId(options.UserId) || input.DestinationId != options.UserId
            || input.AccountId is not null && input.AccountId != options.UserId)
            return Result(BrokerOutcome.Rejected, "account_unverified", "Configured X user identity must match the publication destination.");
        if (string.IsNullOrWhiteSpace(input.Text) || input.Text.EnumerateRunes().Count() > 280 || input.Template is not null)
            return Result(BrokerOutcome.Rejected, "invalid_text", "X text must contain at most 280 characters; provider weighting also applies.");
        if (input.Media is { Count: > 1 })
            return Result(BrokerOutcome.Unsupported, "album_not_supported", "This adapter supports one uploaded image.");
        if (input.ResumeReceipt is not null && (input.ResumeReceipt.Provider != Provider || input.ResumeReceipt.Stage != "media_uploaded"))
            return Result(BrokerOutcome.Unsupported, "reconcile_required", "Existing X dispatch receipts require reconciliation before another publication.");
        if (cancellationToken.IsCancellationRequested)
            return Result(BrokerOutcome.Rejected, "cancelled_before_send", "Operation cancelled before dispatch.");
        if (input.SaveCheckpointAsync is null)
            return Result(BrokerOutcome.Rejected, "checkpoint_required", "Persist each X dispatch before sending.");

        var checkpoint = input.ResumeReceipt;
        var photo = input.Media?.FirstOrDefault();
        if (photo is not null && (photo.ContentType is not ("image/png" or "image/jpeg") || photo.Data.Length is 0 or > 5_242_880))
            return Result(BrokerOutcome.Rejected, "invalid_photo", "X image must be PNG/JPEG within five megabytes.");
        if (checkpoint is not null && (!ProviderInput.IsNumericId(checkpoint.ExternalId)
            || !TryCheckpointExpiry(checkpoint, out var resumedExpiry) || resumedExpiry <= clock.GetUtcNow()
            || checkpoint.RelatedIds?.GetValueOrDefault("account_id") != options.UserId
            || photo is not null && checkpoint.RelatedIds?.GetValueOrDefault("media_sha256") != Hash(photo.Data)))
            return new(BrokerOutcome.Rejected, Provider, "upload_unverified_or_expired",
                "Saved X media identity, content or expiry is not valid for this request.", checkpoint);

        if (photo is not null && checkpoint is null)
        {
            var uploadDispatch = new ProviderReceipt(Provider, "media_upload_started", options.UserId!,
                new Dictionary<string, string> { ["account_id"] = options.UserId!, ["media_sha256"] = Hash(photo.Data) });
            try { await input.SaveCheckpointAsync(uploadDispatch, cancellationToken); }
            catch (Exception) { return new(BrokerOutcome.Rejected, Provider, "checkpoint_failed",
                "Upload dispatch checkpoint failed; media upload was not attempted.", uploadDispatch); }
            var uploadStarted = clock.GetUtcNow();
            using var upload = ProviderHttpTransport.Create(HttpMethod.Post, "https://api.x.com/2/media/upload", options.AccessToken);
            upload.Content = JsonContent.Create(new { media = Convert.ToBase64String(photo.Data), media_category = "tweet_image" });
            var uploaded = await ProviderHttpTransport.SendAsync(client, upload, true, cancellationToken);
            if (uploaded.Outcome != BrokerOutcome.Succeeded)
                return new(uploaded.Outcome, Provider, uploaded.Code, uploaded.Message,
                    uploaded.Outcome == BrokerOutcome.Ambiguous ? uploadDispatch with { Stage = "media_upload_unknown" } : uploadDispatch);
            if (uploaded.Json is not { } uploadRoot || !ProviderReadEvidence.TryObject(uploadRoot, "data", out var uploadData)
                || ProviderHttpTransport.Id(uploadData, "id") is not { } mediaId || !ProviderInput.IsNumericId(mediaId))
                return new(BrokerOutcome.Ambiguous, Provider, "missing_upload_receipt",
                    "X accepted media without a usable upload ID.", uploadDispatch with { Stage = "media_upload_unknown" });
            var related = new Dictionary<string, string> { ["account_id"] = options.UserId!, ["media_sha256"] = Hash(photo.Data) };
            if (!uploadData.TryGetProperty("expires_after_secs", out var ttl) || ttl.ValueKind != System.Text.Json.JsonValueKind.Number || !ttl.TryGetInt32(out var seconds) || seconds <= 0)
                return new(BrokerOutcome.Ambiguous, Provider, "upload_expiry_unavailable",
                    "X did not return a valid upload expiry; final publication was not attempted.",
                    new(Provider, "media_upload_unknown", mediaId, related));
            related["expires_at"] = uploadStarted.AddSeconds(seconds).ToString("O", CultureInfo.InvariantCulture);
            checkpoint = new(Provider, "media_uploaded", mediaId, related);
            if (ProviderReadEvidence.HasErrors(uploadRoot))
                return new(BrokerOutcome.Ambiguous, Provider, "partial_upload",
                    "X returned partial upload errors; final publication was not attempted.", checkpoint with { Stage = "media_upload_unknown" });
            if (uploadData.TryGetProperty("processing_info", out var processing)
                && ProviderHttpTransport.Text(processing, "state") is not ("succeeded" or "success"))
                return new(BrokerOutcome.Unsupported, Provider, "media_processing_unsupported",
                    "This adapter requires a ready image upload; final publication was not attempted.", checkpoint);
            try { await input.SaveCheckpointAsync(checkpoint, cancellationToken); }
            catch (Exception) { return new(BrokerOutcome.Ambiguous, Provider, "checkpoint_failed",
                "Uploaded media could not be checkpointed; final publication was not attempted.", checkpoint); }
        }

        if (checkpoint is not null && (!TryCheckpointExpiry(checkpoint, out var expiry) || expiry <= clock.GetUtcNow()))
            return new(BrokerOutcome.Rejected, Provider, "media_expired", "Uploaded X image expired before publication.", checkpoint);
        var finalDispatch = checkpoint is null
            ? new ProviderReceipt(Provider, "publication_started", options.UserId!,
                new Dictionary<string, string> { ["account_id"] = options.UserId! })
            : checkpoint with { Stage = "publication_started" };
        try { await input.SaveCheckpointAsync(finalDispatch, cancellationToken); }
        catch (Exception) { return new(BrokerOutcome.Rejected, Provider, "checkpoint_failed",
            "Publication dispatch checkpoint failed; final publication was not attempted.", checkpoint ?? finalDispatch); }

        using var request = ProviderHttpTransport.Create(HttpMethod.Post, "https://api.x.com/2/tweets", options.AccessToken);
        var payload = new Dictionary<string, object?> { ["text"] = input.Text };
        if (checkpoint is not null) payload["media"] = new { media_ids = new[] { checkpoint.ExternalId } };
        request.Content = JsonContent.Create(payload);
        var reply = await ProviderHttpTransport.SendAsync(client, request, true, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded)
            return new(reply.Outcome, Provider, reply.Code, reply.Message,
                reply.Outcome == BrokerOutcome.Ambiguous ? finalDispatch with { Stage = "publication_unknown" } : finalDispatch);
        if (reply.Json is not { } root || !ProviderReadEvidence.TryObject(root, "data", out var data)
            || ProviderHttpTransport.Id(data, "id") is not { } id || !ProviderInput.IsNumericId(id))
            return new(BrokerOutcome.Ambiguous, Provider, "missing_receipt",
                "X accepted the request without a usable post ID.", finalDispatch with { Stage = "publication_unknown" });
        var receiptIds = new Dictionary<string, string> { ["account_id"] = options.UserId!, ["post_id"] = id };
        if (checkpoint is not null) receiptIds["media_id"] = checkpoint.ExternalId;
        if (ProviderReadEvidence.HasErrors(root) || ProviderHttpTransport.Id(data, "author_id") is { } author && author != options.UserId)
            return new(BrokerOutcome.Ambiguous, Provider, "partial_or_mismatched_publication",
                "X returned conflicting publication evidence; reconcile before retrying.", new(Provider, "publication_unknown", id, receiptIds));
        return new(BrokerOutcome.Succeeded, Provider, "published", "X post accepted.",
            new(Provider, "published", id, receiptIds));
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static bool TryCheckpointExpiry(ProviderReceipt receipt, out DateTimeOffset expiry)
        => DateTimeOffset.TryParseExact(receipt.RelatedIds?.GetValueOrDefault("expires_at"), "O",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out expiry);

    private SocialPublishResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
