namespace OcharoMarketing.Domain.Assets.Entities;

/// <summary>Stores the durable Asset state.</summary>
public sealed class AssetRecord
{
    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>Gets or sets the local blob of an asset stored before WoW2 Files; empty for one stored in
    /// Files.</summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the WoW2 Files file holding the bytes; none for an asset still only in the local blob
    /// folder.</summary>
    public string? FileId { get; set; }
}
