namespace OcharoMarketing.Application.Scanning;

/// <summary>Requests one explicit source-image analysis with an idempotent operation identity.</summary>
public sealed record SourceAnalysisInput(
    string OperationId,
    IReadOnlyList<string> AssetIds,
    string Provider = "openai",
    string Locale = "uz",
    string? Prompt = null);
