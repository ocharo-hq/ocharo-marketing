using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Domain.Scanning;

namespace OcharoMarketing.Persistence.Scanning;

/// <summary>Stores analysis evidence with compare-and-swap review updates.</summary>
public sealed class SourceAnalysisRepository(OcharoMarketingDbContext database) : ISourceAnalysisRepository
{
    public Task<SourceAnalysisRecord?> FindAsync(string id, CancellationToken ct)
    {
        return database.Set<SourceAnalysisRecord>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<IReadOnlyList<SourceAnalysisRecord>> ListAsync(CancellationToken ct)
    {
        SourceAnalysisRecord[] records = await database.Set<SourceAnalysisRecord>().AsNoTracking().ToArrayAsync(ct);
        return records.OrderByDescending(x => x.CreatedAt).ToArray();
    }

    public async Task<bool> TryCreateAsync(SourceAnalysisRecord record, CancellationToken ct)
    {
        database.Add(record);
        try
        {
            await database.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            return false;
        }
        finally
        {
            database.Entry(record).State = EntityState.Detached;
        }
    }

    public async Task UpdateAsync(SourceAnalysisRecord record, int expectedRevision, CancellationToken ct)
    {
        int updated = await database.Set<SourceAnalysisRecord>()
            .Where(x => x.Id == record.Id && x.Revision == expectedRevision)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Revision, record.Revision)
                .SetProperty(x => x.Status, record.Status)
                .SetProperty(x => x.Model, record.Model)
                .SetProperty(x => x.SuggestedText, record.SuggestedText)
                .SetProperty(x => x.ReviewedText, record.ReviewedText)
                .SetProperty(x => x.Message, record.Message)
                .SetProperty(x => x.UpdatedAt, record.UpdatedAt)
                .SetProperty(x => x.ReviewedAt, record.ReviewedAt), ct);
        if (updated != 1)
        {
            throw new MarketingException("Analysis changed. Reload before saving this review.", 409);
        }
    }
}
