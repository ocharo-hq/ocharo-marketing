using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Domain.Marketing.Entities;

namespace OcharoMarketing.Application.Marketing.Repositories;

/// <summary>Provides atomic durable publication authorization, ownership and observations.</summary>
public interface IDeliveryRepository
{
    Task<DeliveryConnectionRecord?> FindConnectionAsync(string id, CancellationToken ct);
    Task InvalidateConnectionAsync(string id, string message, CancellationToken ct);
    Task<DeliveryIdentityModel> SaveVerifiedConnectionAsync(DeliveryIdentityModel identity, string configurationStamp, CancellationToken ct);
    Task<ScheduleModel?> FindScheduleAsync(string id, CancellationToken ct);
    Task<PublicationOperationModel?> FindOperationAsync(string scheduleId, CancellationToken ct);
    Task<IReadOnlyList<PublicationOperationModel>> GetOperationsAsync(CancellationToken ct);
    Task<PublicationOperationModel> QueueAsync(PublicationOperationModel operation, string configurationStamp, CancellationToken ct);
    Task RecoverExpiredAsync(DateTimeOffset now, CancellationToken ct);
    Task<PublicationClaim?> ClaimDueAsync(DateTimeOffset now, TimeSpan lease, CancellationToken ct);
    Task<bool> ConnectionMatchesAsync(PublicationClaim claim, string configurationStamp, CancellationToken ct);
    Task MarkDispatchAsync(PublicationClaim claim, DateTimeOffset now, CancellationToken ct);
    Task CheckpointAsync(PublicationClaim claim, ProviderReceipt receipt, DateTimeOffset now, CancellationToken ct);
    Task FinishAsync(PublicationClaim claim, string status, string message, ProviderReceipt? receipt, DateTimeOffset now, CancellationToken ct);
    Task HoldQueuedAsync(string? provider, string message, DateTimeOffset now, CancellationToken ct);
    Task<ScheduleModel> CancelAsync(string scheduleId, DateTimeOffset now, CancellationToken ct);
    Task<PublicationOperationModel> ApplyReconciliationAsync(string scheduleId, string expectedReceiptId, string connectionId, int connectionRevision, string configurationStamp, ProviderReceipt? verifiedReceipt, string message, DateTimeOffset now, CancellationToken ct);
    Task<MetricCacheModel> GetMetricsAsync(string scheduleId, CancellationToken ct);
    Task<IReadOnlyList<MetricObservationModel>> GetCachedObservationsAsync(CancellationToken ct);
    Task<string> ReserveMetricReadAsync(string scheduleId, string connectionId, int connectionRevision, DateTimeOffset now, int maximumPerHour, int cooldownSeconds, CancellationToken ct, string purpose = "metrics");
    Task CompleteMetricReadAsync(string attemptId, MetricAttemptModel attempt, IReadOnlyList<MetricObservationModel> observations, CancellationToken ct);
}
