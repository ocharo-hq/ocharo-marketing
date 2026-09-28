using OcharoMarketing.Domain.Scanning;

namespace OcharoMarketing.Application.Scanning;

/// <summary>Persists analysis intents and guards review revisions.</summary>
public interface ISourceAnalysisRepository
{
    Task<SourceAnalysisRecord?> FindAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<SourceAnalysisRecord>> ListAsync(CancellationToken ct);
    Task<bool> TryCreateAsync(SourceAnalysisRecord record, CancellationToken ct);
    Task UpdateAsync(SourceAnalysisRecord record, int expectedRevision, CancellationToken ct);
}
