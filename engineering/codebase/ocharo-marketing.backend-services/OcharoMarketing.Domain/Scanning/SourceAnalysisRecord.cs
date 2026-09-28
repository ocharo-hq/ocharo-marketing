namespace OcharoMarketing.Domain.Scanning;

/// <summary>Preserves source evidence and separately reviewed assistance text.</summary>
public sealed class SourceAnalysisRecord
{
    public string Id { get; set; } = string.Empty;
    public int Revision { get; set; } = 1;
    public string Status { get; set; } = "pending";
    public string Provider { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string Locale { get; set; } = "uz";
    public string Prompt { get; set; } = string.Empty;
    public string SourcesJson { get; set; } = "[]";
    public string RequestFingerprint { get; set; } = string.Empty;
    public string? SuggestedText { get; set; }
    public string? ReviewedText { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}
