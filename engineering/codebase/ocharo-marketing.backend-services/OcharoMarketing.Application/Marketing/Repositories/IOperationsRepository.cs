namespace OcharoMarketing.Application.Marketing.Repositories;

/// <summary>Provides compare-and-swap settings and append-only observations in the workspace metadata store.</summary>
public interface IOperationsRepository
{
    Task<string?> ReadAsync(string key, CancellationToken ct);
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct);
    Task<bool> CompareExchangeAsync(string key, string? expectedJson, string json, CancellationToken ct);
}
