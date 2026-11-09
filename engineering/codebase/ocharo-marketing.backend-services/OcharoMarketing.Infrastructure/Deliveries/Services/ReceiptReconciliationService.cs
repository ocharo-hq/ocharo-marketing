using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Core.Errors;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Deliveries.Repositories;
using OcharoMarketing.Application.Providers;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoMarketing.Infrastructure.Deliveries.Services;

/// <summary>Reconciles only a stored candidate final X ID through a separately authorized bounded GET.</summary>
public sealed class ReceiptReconciliationService(IDeliveryRepository repository, DeliveryPolicy policy,
    IEnumerable<IReceiptReconciliationBroker> brokers, TimeProvider time)
{
    public async Task<PublicationOperationModel> ReconcileAsync(string scheduleId, CancellationToken ct)
    {
        var operation = await repository.FindOperationAsync(scheduleId, ct)
            ?? throw new AppException(MarketingErrors.NotFound("Publication operation not found."));
        if (!operation.CanReconcile) throw new AppException(MarketingErrors.Conflict(operation.ReconciliationMessage));
        var configured = policy.Destination(operation.ConnectionId);
        if (!policy.Settings.MetricReadsEnabled || configured is not { Configured: true, MetricReadsEnabled: true })
            throw new AppException(
                MarketingErrors.Conflict(
                    "Read-only receipt verification is disabled. X reads require explicit opt-in and may "
                        + "consume provider credits."));
        var connection = await repository.FindConnectionAsync(operation.ConnectionId, ct);
        if (connection is null
            || connection.VerificationStatus != "verified"
            || connection.ConfigurationStamp != configured.ConfigurationStamp)
            throw new AppException(
                MarketingErrors.Conflict("Verify the current original account before receipt reconciliation."));
        var identity = MarketingJson.Read<DeliveryIdentityModel>(connection.Json);
        if (identity.AccountId != operation.Snapshot.Identity.AccountId
            || identity.DestinationId != operation.Snapshot.Identity.DestinationId)
            throw new AppException(
                MarketingErrors.Conflict("The verified account differs from the frozen receipt owner."));
        var now = time.GetUtcNow();
        var attempt = await repository.ReserveMetricReadAsync(
            scheduleId,
            identity.ConnectionId,
            connection.Revision,
            now,
            policy.MaximumMetricReadsPerHour, policy.MetricCooldownSeconds, ct, "reconciliation");
        ReceiptReconciliationResult result;
        try
        {
            result = await brokers
                .Single(x => x.Provider == "x")
                .ReconcileAsync(new(identity.AccountId, operation.Receipt!), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            result = new(
                BrokerOutcome.Rejected,
                "x",
                "read_failed",
                "Receipt verification failed. The publication remains unknown and will not retry.");
        }
        var verified = result.Outcome == BrokerOutcome.Succeeded
            && result.Provider == "x"
            && result.Receipt is { Provider: "x", Stage: "published" }
            && result.Receipt.ExternalId == operation.Receipt!.ExternalId
                && result.Receipt.RelatedIds?.GetValueOrDefault("account_id") == identity.AccountId;
        await repository.CompleteMetricReadAsync(
            attempt,
            new(now, verified ? "succeeded" : "failed", result.Message),
            [],
            ct);
        return await repository.ApplyReconciliationAsync(
            scheduleId,
            operation.Receipt!.ExternalId,
            identity.ConnectionId,
            connection.Revision,
            configured.ConfigurationStamp,
            verified ? result.Receipt : null,
            verified
                ? "Provider GET verified the existing final receipt and its account owner. No publication was retried."
                : result.Message,
            now,
            ct);
    }
}
