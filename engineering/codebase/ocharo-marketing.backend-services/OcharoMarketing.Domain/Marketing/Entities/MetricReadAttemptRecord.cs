namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Persists delivery evidence independently of provider secrets.</summary>
public sealed class MetricReadAttemptRecord
{
    public string Id { get; set; } = "";
    public string Purpose { get; set; } = "metrics";
    public string ScheduleId { get; set; } = "";
    public long AttemptedAt { get; set; }
    public string Json { get; set; } = "";
}
