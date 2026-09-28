namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the durable DraftRevision state.</summary>
public sealed class DraftRevisionRecord
{
    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the DraftId value.</summary>
    public string DraftId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Revision value.</summary>
    public int Revision { get; set; } = 1;

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;
}
