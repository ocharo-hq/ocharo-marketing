namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record MetricCacheModel(string ScheduleId, MetricObservationModel[] Observations, MetricAttemptModel? LastAttempt)
{
    public MetricObservationModel[] History { get; init; } = [];
    public bool HistoryTruncated { get; init; }
}
