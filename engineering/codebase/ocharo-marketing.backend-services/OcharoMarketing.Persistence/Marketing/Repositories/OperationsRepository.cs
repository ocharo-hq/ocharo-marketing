using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Marketing.Repositories;

namespace OcharoMarketing.Persistence.Marketing.Repositories;

/// <summary>Atomically stores independent operations documents without replacing another editor's revision.</summary>
public sealed class OperationsRepository(OcharoMarketingDbContext db) : IOperationsRepository
{
    public Task<string?> ReadAsync(string key, CancellationToken ct) =>
        db.Metadata.AsNoTracking().Where(x => x.Id == "operations:" + key).Select(x => x.Json).SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct) =>
        await db.Metadata.AsNoTracking().Where(x => x.Id.StartsWith("operations:" + prefix)).Select(x => x.Json).ToArrayAsync(ct);

    public async Task<bool> CompareExchangeAsync(string key, string? expectedJson, string json, CancellationToken ct)
    {
        var id = "operations:" + key;
        if (expectedJson is not null)
            return await db.Metadata.Where(x => x.Id == id && x.Json == expectedJson)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Json, json), ct) == 1;
        return await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Metadata (Id, Json) VALUES ({id}, {json}) ON CONFLICT(Id) DO NOTHING", ct) == 1;
    }
}
