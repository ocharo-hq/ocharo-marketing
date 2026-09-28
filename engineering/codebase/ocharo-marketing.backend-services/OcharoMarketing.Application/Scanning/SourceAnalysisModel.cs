namespace OcharoMarketing.Application.Scanning;

/// <summary>Separates generated observations from operator-reviewed copy.</summary>
public sealed record SourceAnalysisModel(
    string Id,
    int Revision,
    string Status,
    string Provider,
    string? Model,
    string Locale,
    string Prompt,
    IReadOnlyList<SourceEvidence> Sources,
    string? SuggestedText,
    string? ReviewedText,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ReviewedAt);
