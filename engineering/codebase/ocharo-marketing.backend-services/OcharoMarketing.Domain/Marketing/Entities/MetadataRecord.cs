namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the durable Metadata state.</summary>
public sealed class MetadataRecord
{
    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;
}
