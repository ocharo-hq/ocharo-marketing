using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Performs explicit bounded owned-receipt reads and retains prior observations on errors.</summary>
public sealed class MetricObservationService(IDeliveryRepository delivery, DeliveryPolicy policy, IEnumerable<ISocialMetricsBroker> brokers, TimeProvider time)
{
    public async Task<MetricCacheModel> RefreshAsync(string scheduleId, CancellationToken ct)
    {
        var operation = await delivery.FindOperationAsync(scheduleId, ct) ?? throw new MarketingException("No owned publication receipt exists for this plan.", 409);
        var configured = policy.Destination(operation.ConnectionId);
        if (!policy.Settings.MetricReadsEnabled || configured is not { Configured: true, MetricReadsEnabled: true } || operation.Provider != "x")
            throw new MarketingException("Observed-metric reads are disabled or unsupported. X reads may consume provider credits and require explicit server opt-in.", 409);
        if (operation.Status != "published" || operation.Receipt is null)
            throw new MarketingException("Only a verified owned final publication receipt can be refreshed.", 409);
        var connection = await delivery.FindConnectionAsync(operation.ConnectionId, ct);
        if (connection is null || connection.VerificationStatus != "verified" || connection.ConfigurationStamp != configured.ConfigurationStamp)
            throw new MarketingException("Verify the current original account before reading metrics.", 409);
        var identity = MarketingJson.Read<DeliveryIdentityModel>(connection.Json);
        if (!identity.Capabilities.Contains("public-metrics") || identity.AccountId != operation.Snapshot.Identity.AccountId || identity.DestinationId != operation.Snapshot.Identity.DestinationId)
            throw new MarketingException("The current account differs from the owner of this publication.", 409);
        var now = time.GetUtcNow();
        var attemptId = await delivery.ReserveMetricReadAsync(scheduleId, identity.ConnectionId, identity.Revision, now, policy.MaximumMetricReadsPerHour, policy.MetricCooldownSeconds, ct);
        try
        {
            var result = await brokers.Single(x => x.Provider == "x").ReadAsync(new(identity.AccountId, operation.Receipt), ct);
            var success = result.Outcome == BrokerOutcome.Succeeded;
            var observations = success ? result.Observations.Select(item => new MetricObservationModel($"{attemptId}-{item.Name}", scheduleId,
                identity.ConnectionId, "x", operation.Receipt.ExternalId, item.Name, item.Value, item.Availability, item.ObservedAt, item.Reason)).ToArray() : [];
            await delivery.CompleteMetricReadAsync(attemptId, new(now, success ? result.HasPartialErrors ? "partial" : "succeeded" : "failed", result.Message), observations, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            await delivery.CompleteMetricReadAsync(attemptId, new(now, "failed", "The provider read failed. Previous observations retain their original values and timestamps."), [], ct);
        }
        return await delivery.GetMetricsAsync(scheduleId, ct);
    }
}
