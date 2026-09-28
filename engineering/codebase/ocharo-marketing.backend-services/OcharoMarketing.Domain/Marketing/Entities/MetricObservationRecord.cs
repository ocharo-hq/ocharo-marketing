namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Persists delivery evidence independently of provider secrets.</summary>
public sealed class MetricObservationRecord
{
    public string Id { get; set; } = "";
    public string ScheduleId { get; set; } = "";
    public string Name { get; set; } = "";
    public long RecordedAt { get; set; }
    public string Json { get; set; } = "";
}
