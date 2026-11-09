namespace OcharoMarketing.Domain.Marketing.Entities;

/// <summary>Stores the WoW2 Files file holding an approved draft revision's export ZIP: exported there, or moved there by
/// the migration and filled at boot. An export made before Files without one reads from the local blob folder.</summary>
public sealed class ExportFileRecord
{
    /// <summary>Gets or sets the exported draft.</summary>
    public string DraftId { get; set; } = string.Empty;

    /// <summary>Gets or sets the approved revision the export froze.</summary>
    public int Revision { get; set; }

    /// <summary>Gets or sets the SHA-256 of the ZIP's bytes, which a read checks.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Gets or sets the WoW2 Files file.</summary>
    public string FileId { get; set; } = string.Empty;
}
