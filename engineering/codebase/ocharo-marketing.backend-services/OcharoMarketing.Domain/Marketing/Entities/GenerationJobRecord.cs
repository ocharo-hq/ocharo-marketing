namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the durable GenerationJob state.</summary>
public sealed class GenerationJobRecord
{
    /// <summary>Gets or sets the durable provider checkpoint.</summary>
    public string? CheckpointJson { get; set; }

    /// <summary>Gets or sets the Id value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the Kind value.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the Status value.</summary>
    public string Status { get; set; } = "queued";

    /// <summary>Gets or sets the PayloadJson value.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the Json value.</summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>Gets or sets the CreatedAt value.</summary>
    public long CreatedAt { get; set; } = 0;

    /// <summary>Gets or sets the LeaseExpiresAt value.</summary>
    public long? LeaseExpiresAt { get; set; } = null;

    /// <summary>Gets or sets the ClaimToken value.</summary>
    public string? ClaimToken { get; set; } = null;

    /// <summary>Gets or sets the Attempts value.</summary>
    public int Attempts { get; set; } = 0;
}
