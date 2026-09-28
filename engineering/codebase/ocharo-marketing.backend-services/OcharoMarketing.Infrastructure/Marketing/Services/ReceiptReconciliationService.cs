using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Reconciles only a stored candidate final X ID through a separately authorized bounded GET.</summary>
public sealed class ReceiptReconciliationService(IDeliveryRepository repository, DeliveryPolicy policy,
    IEnumerable<IReceiptReconciliationBroker> brokers, TimeProvider time)
{
    public async Task<PublicationOperationModel> ReconcileAsync(string scheduleId, CancellationToken ct)
    {
        var operation = await repository.FindOperationAsync(scheduleId, ct) ?? throw new MarketingException("Publication operation not found.", 404);
        if (!operation.CanReconcile) throw new MarketingException(operation.ReconciliationMessage, 409);
        var configured = policy.Destination(operation.ConnectionId);
        if (!policy.Settings.MetricReadsEnabled || configured is not { Configured: true, MetricReadsEnabled: true })
            throw new MarketingException("Read-only receipt verification is disabled. X reads require explicit opt-in and may consume provider credits.", 409);
        var connection = await repository.FindConnectionAsync(operation.ConnectionId, ct);
        if (connection is null || connection.VerificationStatus != "verified" || connection.ConfigurationStamp != configured.ConfigurationStamp)
            throw new MarketingException("Verify the current original account before receipt reconciliation.", 409);
        var identity = MarketingJson.Read<DeliveryIdentityModel>(connection.Json);
        if (identity.AccountId != operation.Snapshot.Identity.AccountId || identity.DestinationId != operation.Snapshot.Identity.DestinationId)
            throw new MarketingException("The verified account differs from the frozen receipt owner.", 409);
        var now = time.GetUtcNow();
        var attempt = await repository.ReserveMetricReadAsync(scheduleId, identity.ConnectionId, connection.Revision, now,
            policy.MaximumMetricReadsPerHour, policy.MetricCooldownSeconds, ct, "reconciliation");
        ReceiptReconciliationResult result;
        try { result = await brokers.Single(x => x.Provider == "x").ReconcileAsync(new(identity.AccountId, operation.Receipt!), ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { result = new(BrokerOutcome.Rejected, "x", "read_failed", "Receipt verification failed. The publication remains unknown and will not retry."); }
        var verified = result.Outcome == BrokerOutcome.Succeeded && result.Provider == "x" && result.Receipt is { Provider: "x", Stage: "published" }
            && result.Receipt.ExternalId == operation.Receipt!.ExternalId && result.Receipt.RelatedIds?.GetValueOrDefault("account_id") == identity.AccountId;
        await repository.CompleteMetricReadAsync(attempt, new(now, verified ? "succeeded" : "failed", result.Message), [], ct);
        return await repository.ApplyReconciliationAsync(scheduleId, operation.Receipt!.ExternalId, identity.ConnectionId, connection.Revision,
            configured.ConfigurationStamp, verified ? result.Receipt : null, verified ? "Provider GET verified the existing final receipt and its account owner. No publication was retried." : result.Message, now, ct);
    }
}
