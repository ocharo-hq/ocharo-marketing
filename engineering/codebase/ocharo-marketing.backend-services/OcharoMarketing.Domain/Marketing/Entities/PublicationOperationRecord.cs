namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Persists delivery evidence independently of provider secrets.</summary>
public sealed class PublicationOperationRecord
{
    public string Id { get; set; } = "";
    public string ScheduleId { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public int ConnectionRevision { get; set; }
    public string ConfigurationStamp { get; set; } = "";
    public string Status { get; set; } = "queued";
    public long DueAt { get; set; }
    public string? ClaimToken { get; set; }
    public long? LeaseExpiresAt { get; set; }
    public long? DispatchStartedAt { get; set; }
    public int Attempts { get; set; }
    public string Json { get; set; } = "";
}
