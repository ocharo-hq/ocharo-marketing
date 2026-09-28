namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the durable Draft state.</summary>
public sealed class DraftRecord
{
    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the Revision value.</summary>
    public int Revision { get; set; } = 1;

    /// <summary>Gets or sets the ApprovedRevision value.</summary>
    public int? ApprovedRevision { get; set; } = null;

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;
}
