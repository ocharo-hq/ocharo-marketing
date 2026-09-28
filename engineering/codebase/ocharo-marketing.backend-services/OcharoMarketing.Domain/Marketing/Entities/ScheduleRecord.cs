namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the durable Schedule state.</summary>
public sealed class ScheduleRecord
{
    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the DraftId value.</summary>
    public string DraftId { get; set; } = string.Empty;

    /// <summary>Gets or sets the DraftRevision value.</summary>
    public int DraftRevision { get; set; } = 1;

    /// <summary>Gets or sets the ScheduledAt value.</summary>
    public long ScheduledAt { get; set; } = 0;

    /// <summary>Gets or sets the Status value.</summary>
    public string Status { get; set; } = "planned";

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;
}
