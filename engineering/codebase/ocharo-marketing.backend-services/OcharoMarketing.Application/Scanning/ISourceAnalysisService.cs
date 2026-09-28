namespace OcharoMarketing.Application.Scanning;

/// <summary>Coordinates source-bound suggestions and explicit human review.</summary>
public interface ISourceAnalysisService
{
    Task<IReadOnlyList<SourceAnalysisModel>> ListAsync(CancellationToken ct);
    Task<SourceAnalysisModel> AnalyzeAsync(SourceAnalysisInput input, CancellationToken ct);
    Task<SourceAnalysisModel> ReviewAsync(string id, SourceReviewInput input, CancellationToken ct);
}
