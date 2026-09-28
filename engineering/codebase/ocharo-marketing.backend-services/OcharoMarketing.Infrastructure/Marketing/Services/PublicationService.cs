using System.Text;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Authorizes frozen delivery snapshots and coordinates guarded broker effects.</summary>
public sealed class PublicationService(IDeliveryRepository delivery, IMarketingRepository repository, IAssetService assets,
    IExternalMarketingService external, DeliveryPolicy policy, IEnumerable<IProviderVerificationBroker> verifiers,
    IEnumerable<ISocialPublishingBroker> publishers, TimeProvider time)
{
    public DeliverySettingsModel Settings => policy.Settings;

    public async Task<IReadOnlyList<ConnectionModel>> ConnectionsAsync(CancellationToken ct)
    {
        var result = new List<ConnectionModel>();
        foreach (var original in external.GetConnections())
        {
            var configured = policy.Destination(original.Id);
            if (configured is null)
            {
                result.Add(original with { SafeMessage = original.Id switch {
                    "instagram" => "Publishing is blocked until the reviewed JPEG and public hosting workflow is verified.",
                    "whatsapp" => "Customer messaging is unavailable until consent and template/window review are implemented.", _ => null } });
                continue;
            }
            var stored = await delivery.FindConnectionAsync(original.Id, ct);
            var identity = stored is null ? null : MarketingJson.Read<DeliveryIdentityModel>(stored.Json);
            var verified = configured.Configured && stored is not null && stored.VerificationStatus == "verified" && stored.ConfigurationStamp == configured.ConfigurationStamp;
            result.Add(original with { Status = configured.Configured ? "configured" : "not-configured",
                ConnectionRevision = stored?.Revision ?? 0, Capabilities = verified ? identity!.Capabilities : original.Capabilities,
                VerificationStatus = verified ? "verified" : stored is null ? "not-verified" : configured.ReloadRequired || stored.ConfigurationStamp != configured.ConfigurationStamp ? "stale" : stored.VerificationStatus,
                ExecutionEnabled = verified && configured.PublishEnabled && Settings.PublishingEnabled,
                MetricReadsEnabled = verified && configured.MetricReadsEnabled && Settings.MetricReadsEnabled,
                AccountId = identity?.AccountId, DestinationId = identity?.DestinationId, DisplayName = identity?.DisplayName,
                VerifiedAt = identity?.VerifiedAt, CanVerify = configured.Configured && (configured.PublishEnabled || configured.MetricReadsEnabled) && Settings.AccountVerificationEnabled,
                SafeMessage = configured.ReloadRequired ? "Server account or credential settings changed. Restart the local app and verify the account again." : verified ? "Account and destination were verified. Execution still requires explicit queue authorization."
                    : stored?.VerificationStatus == "verification-failed" ? stored.SafeMessage : configured.Configured ? "Explicit read-only account verification is required before queueing." : "Configure the account, destination and credentials on the server." });
        }
        return result;
    }

    public async Task<ConnectionModel> VerifyAsync(string connectionId, CancellationToken ct)
    {
        var configured = policy.Destination(connectionId) ?? throw new MarketingException("This provider has no verified delivery setup pathway yet.", 409);
        if (!Settings.AccountVerificationEnabled || !configured.Configured || !(configured.PublishEnabled || configured.MetricReadsEnabled))
            throw new MarketingException("Read-only account verification is disabled or the server connection is incomplete.", 409);
        var verifier = verifiers.Single(x => x.Provider == configured.Provider);
        var result = await verifier.VerifyAsync(new(configured.DestinationId, configured.AccountId), ct);
        if (result.Outcome != BrokerOutcome.Succeeded || result.Identity is null)
        {
            await delivery.InvalidateConnectionAsync(connectionId, result.Message, ct);
            throw new MarketingException(result.Message, 409);
        }
        if (result.Provider != configured.Provider || result.Identity.AccountId != configured.AccountId || result.Identity.DestinationId != configured.DestinationId)
        {
            await delivery.InvalidateConnectionAsync(connectionId, "Provider identity no longer matches the configured account/destination.", ct);
            throw new MarketingException("Provider identity differs from the configured numeric account/destination. Update server binding and verify again.", 409);
        }
        if (policy.Destination(connectionId)?.ConfigurationStamp != configured.ConfigurationStamp || !Settings.AccountVerificationEnabled)
            throw new MarketingException("Connection settings changed during verification. Verify the current configuration again.", 409);
        await delivery.SaveVerifiedConnectionAsync(new(connectionId, configured.Provider, 0, result.Identity.AccountId,
            result.Identity.DestinationId, result.Identity.DisplayName, result.Identity.Capabilities.ToArray(), result.Identity.VerifiedAt), configured.ConfigurationStamp, ct);
        return (await ConnectionsAsync(ct)).Single(x => x.Id == connectionId);
    }

    public async Task<PublicationOperationModel> QueueAsync(string scheduleId, QueuePublicationInput input, CancellationToken ct)
    {
        if (!Guid.TryParse(input.OperationId, out _)) throw new MarketingException("Provide a new UUID for this explicit queue action.");
        var previous = await delivery.FindOperationAsync(scheduleId, ct);
        if (previous is not null)
        {
            if (previous.Id == input.OperationId && previous.ConnectionId == input.ConnectionId && previous.ConnectionRevision == input.ExpectedConnectionRevision && previous.Snapshot.Draft.Revision == input.ExpectedDraftRevision)
                return previous;
            throw new MarketingException("This plan already has a terminal or authorized operation. Create a new plan for another authorization.", 409);
        }
        var configured = policy.Destination(input.ConnectionId) ?? throw new MarketingException("This channel is not enabled for automatic publication. Instagram requires reviewed public JPEG delivery; WhatsApp requires consent and messaging setup.", 409);
        if (!Settings.PublishingEnabled || !configured.PublishEnabled || !configured.Configured)
            throw new MarketingException("Publishing is disabled or this server connection is incomplete. The saved plan remains manual.", 409);
        var connection = await delivery.FindConnectionAsync(input.ConnectionId, ct);
        if (connection is null || connection.VerificationStatus != "verified" || connection.Revision != input.ExpectedConnectionRevision || connection.ConfigurationStamp != configured.ConfigurationStamp)
            throw new MarketingException("Verify the current account and review its connection revision before queueing.", 409);
        var identity = MarketingJson.Read<DeliveryIdentityModel>(connection.Json);
        var schedule = await delivery.FindScheduleAsync(scheduleId, ct) ?? throw new MarketingException("Plan not found.", 404);
        if (schedule.Status != "planned" || schedule.Channel != identity.Provider || schedule.DraftRevision != input.ExpectedDraftRevision || schedule.ScheduledAt <= time.GetUtcNow())
            throw new MarketingException("Choose a future planned entry matching this channel and reviewed revision.", 409);
        var draft = await repository.FindDraftAsync(schedule.DraftId, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (draft.Revision != input.ExpectedDraftRevision || draft.ApprovedRevision != draft.Revision)
            throw new MarketingException("The draft changed or approval expired. Review it and create a new plan.", 409);
        if (draft.MediaIds.Length > 1) throw new MarketingException("This delivery pathway supports text or one photo. Select one image; no carousel is implied.", 409);
        var max = configured.Provider == "x" ? 280 : draft.MediaIds.Length == 0 ? 4096 : 1024;
        if (string.IsNullOrWhiteSpace(draft.Caption) || draft.Caption.EnumerateRunes().Count() > max)
            throw new MarketingException($"Review a nonempty caption within this pathway's {max}-character limit; it will not be truncated.", 409);
        var media = new List<DeliveryAssetModel>();
        foreach (var id in draft.MediaIds)
        {
            var asset = await assets.ReadAsync(id, ct);
            if (asset.Asset.ContentType is not ("image/png" or "image/jpeg")) throw new MarketingException("The reviewed delivery photo must be PNG or JPEG.", 409);
            var maximumBytes = configured.Provider == "x" ? 5_242_880 : 10_485_760;
            if (asset.Bytes.Length > maximumBytes) throw new MarketingException("The reviewed photo exceeds this provider's supported upload limit.", 409);
            media.Add(new(id, asset.Asset.Sha256, asset.Asset.ContentType, asset.Asset.FileName));
        }
        var capability = media.Count == 0 ? "text" : "photo";
        if (!identity.Capabilities.Contains(capability)) throw new MarketingException("The verified account lacks the required publication capability.", 409);
        var now = time.GetUtcNow();
        var operation = new PublicationOperationModel(input.OperationId, scheduleId, identity.Provider, identity.ConnectionId, identity.Revision,
            "queued", "authorized", schedule.ScheduledAt, now, null, now, "Explicitly authorized for the frozen destination and revision.", null,
            new(draft, identity, media.ToArray()));
        return await delivery.QueueAsync(operation, configured.ConfigurationStamp, ct);
    }

    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        await delivery.RecoverExpiredAsync(now, ct);
        if (!Settings.PublishingEnabled)
        {
            await delivery.HoldQueuedAsync(null, "Publishing was disabled. Authorization is revoked; create a new plan to authorize delivery again.", now, ct);
            return false;
        }
        foreach (var provider in new[] { "telegram", "x" })
            if (policy.Destination(provider) is not { Configured: true, PublishEnabled: true })
                await delivery.HoldQueuedAsync(provider, "Connection execution was disabled. Create a new plan after setup and review.", now, ct);
        var claim = await delivery.ClaimDueAsync(now, TimeSpan.FromMinutes(10), ct);
        if (claim is null) return false;
        var dispatched = false;
        try
        {
            await GuardAsync(claim, ct);
            var media = new List<SocialMediaAsset>();
            foreach (var item in claim.Operation.Snapshot.Media)
            {
                var asset = await assets.ReadAsync(item.AssetId, ct);
                if (asset.Asset.Sha256 != item.Sha256 || asset.Asset.ContentType != item.ContentType)
                    throw new MarketingException("The frozen delivery media failed its integrity check.", 409);
                media.Add(new(asset.Bytes, item.ContentType, item.FileName));
            }
            await GuardAsync(claim, ct);
            await delivery.MarkDispatchAsync(claim, time.GetUtcNow(), ct);
            dispatched = true;
            var result = await publishers.Single(x => x.Provider == claim.Operation.Provider).PublishAsync(
                new(claim.Operation.Snapshot.Identity.DestinationId, claim.Operation.Snapshot.Draft.Caption, media,
                    SaveCheckpointAsync: async (receipt, token) => {
                        await GuardAsync(claim, token);
                        await delivery.CheckpointAsync(claim, receipt, time.GetUtcNow(), token);
                    }, AccountId: claim.Operation.Snapshot.Identity.AccountId), ct);
            var status = result.Outcome switch { BrokerOutcome.Succeeded when ValidReceipt(claim.Operation, result.Receipt) => "published",
                BrokerOutcome.Rejected or BrokerOutcome.Unsupported => "failed", _ => "unknown" };
            await delivery.FinishAsync(claim, status, status == "unknown" ? "The final outcome is uncertain. Inspect provider history; no automatic retry will occur." : result.Message,
                result.Receipt, time.GetUtcNow(), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var message = dispatched ? "Delivery did not finish reliably. Inspect provider history; no automatic retry will occur."
                : exception is MarketingException safe ? safe.Message : "Delivery preparation failed before dispatch. Review the connection and artwork, then create a new plan.";
            await delivery.FinishAsync(claim, dispatched ? "unknown" : "held", message, null, time.GetUtcNow(), ct);
        }
        return true;
    }

    private async Task GuardAsync(PublicationClaim claim, CancellationToken ct)
    {
        var configured = policy.Destination(claim.Operation.ConnectionId);
        if (!Settings.PublishingEnabled || configured is not { Configured: true, PublishEnabled: true }
            || configured.AccountId != claim.Operation.Snapshot.Identity.AccountId || configured.DestinationId != claim.Operation.Snapshot.Identity.DestinationId
            || !await delivery.ConnectionMatchesAsync(claim, configured.ConfigurationStamp, ct))
            throw new MarketingException("Publishing authorization or the verified account changed. This operation cannot dispatch.", 409);
    }

    private static bool ValidReceipt(PublicationOperationModel operation, ProviderReceipt? receipt)
    {
        if (receipt is null || receipt.Provider != operation.Provider || receipt.Stage != "published" || string.IsNullOrWhiteSpace(receipt.ExternalId)) return false;
        if (operation.Provider == "telegram") return receipt.RelatedIds?.GetValueOrDefault("chat_id") == operation.Snapshot.Identity.DestinationId;
        return operation.Provider == "x" && receipt.RelatedIds?.GetValueOrDefault("account_id") == operation.Snapshot.Identity.AccountId;
    }
}
