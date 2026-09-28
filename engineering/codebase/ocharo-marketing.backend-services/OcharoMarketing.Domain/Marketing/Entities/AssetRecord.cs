namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the durable Asset state.</summary>
public sealed class AssetRecord
{
    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>Gets or sets the BlobPath value.</summary>
    public string BlobPath { get; set; } = string.Empty;
}
