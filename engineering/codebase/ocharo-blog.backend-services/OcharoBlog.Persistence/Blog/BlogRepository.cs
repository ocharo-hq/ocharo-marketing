using Microsoft.EntityFrameworkCore;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Persistence.Blog;

/// <summary>SQLite-backed blog state with revision checks enforced inside one transaction.</summary>
public sealed class BlogRepository(OcharoBlogDbContext db) : IBlogRepository
{
    public async Task<IReadOnlyList<ArticleRecord>> GetArticlesAsync(CancellationToken ct) => await db.Articles.AsNoTracking().ToListAsync(ct);

    public Task<ArticleRecord?> FindArticleAsync(Guid id, CancellationToken ct) => db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<ArticleRecord?> FindArticleBySlugAsync(string slug, CancellationToken ct) =>
        db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Slug == slug, ct);

    public async Task<ArticleRecord> InsertArticleAsync(ArticleRecord record, string summary, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.Articles.AnyAsync(a => a.Slug == record.Slug, ct)) throw BlogException.Conflict($"Another story already uses '{record.Slug}'.");
        db.Articles.Add(record);
        db.ArticleRevisions.Add(Snapshot(record, summary));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        return record;
    }

    public async Task<ArticleRecord> UpdateArticleAsync(ArticleRecord record, int expectedRevision, string summary, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var current = await db.Articles.FirstOrDefaultAsync(a => a.Id == record.Id, ct) ?? throw BlogException.NotFound("Story");
        if (current.Revision != expectedRevision)
            throw BlogException.Conflict($"The story was saved elsewhere (now revision {current.Revision}). Reload before saving.");
        if (current.Slug != record.Slug && await db.Articles.AnyAsync(a => a.Slug == record.Slug && a.Id != record.Id, ct))
            throw BlogException.Conflict($"Another story already uses '{record.Slug}'.");
        current.Slug = record.Slug;
        current.Topic = record.Topic;
        current.Status = record.Status;
        current.PublishAt = record.PublishAt;
        current.PublishedAt = record.PublishedAt;
        current.Revision = expectedRevision + 1;
        current.UpdatedAt = record.UpdatedAt;
        current.Json = record.Json;
        db.ArticleRevisions.Add(Snapshot(current, summary));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        return current;
    }

    public async Task<IReadOnlyList<ArticleRevisionRecord>> GetRevisionsAsync(Guid articleId, CancellationToken ct) =>
        await db.ArticleRevisions.AsNoTracking().Where(r => r.ArticleId == articleId).ToListAsync(ct);

    public Task<ArticleRevisionRecord?> FindRevisionAsync(Guid articleId, int revision, CancellationToken ct) =>
        db.ArticleRevisions.AsNoTracking().FirstOrDefaultAsync(r => r.ArticleId == articleId && r.Revision == revision, ct);

    public async Task<IReadOnlyList<TopicRecord>> GetTopicsAsync(CancellationToken ct) => await db.Topics.AsNoTracking().ToListAsync(ct);

    public async Task SaveTopicAsync(TopicRecord record, CancellationToken ct)
    {
        var current = await db.Topics.FirstOrDefaultAsync(t => t.Key == record.Key, ct);
        if (current is null) db.Topics.Add(record);
        else
        {
            current.SortOrder = record.SortOrder;
            current.Json = record.Json;
        }

        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    public async Task DeleteTopicAsync(string key, CancellationToken ct) => await db.Topics.Where(t => t.Key == key).ExecuteDeleteAsync(ct);

    public Task<SiteSettingsRecord?> FindSettingsAsync(CancellationToken ct) =>
        db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == Application.Blog.Services.SiteSettingsService.SettingsId, ct);

    public async Task<SiteSettingsRecord> SaveSettingsAsync(SiteSettingsRecord record, int expectedRevision, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var current = await db.Settings.FirstOrDefaultAsync(s => s.Id == record.Id, ct);
        if ((current?.Revision ?? 0) != expectedRevision) throw BlogException.Conflict("Settings were saved elsewhere. Reload before saving.");
        if (current is null)
        {
            record.Revision = 1;
            db.Settings.Add(record);
        }
        else
        {
            current.Revision += 1;
            current.Json = record.Json;
            record = current;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        db.ChangeTracker.Clear();
        return record;
    }

    public Task<MediaBlobRecord?> FindMediaAsync(string sha256, CancellationToken ct) => db.MediaBlobs.AsNoTracking().FirstOrDefaultAsync(m => m.Sha256 == sha256, ct);

    public async Task<IReadOnlyList<MediaBlobRecord>> GetMediaAsync(CancellationToken ct) =>
        await db.MediaBlobs.AsNoTracking().OrderByDescending(m => m.StoredAt).ToListAsync(ct);

    public async Task SaveMediaAsync(MediaBlobRecord record, CancellationToken ct)
    {
        if (await db.MediaBlobs.AnyAsync(m => m.Sha256 == record.Sha256, ct)) return;
        db.MediaBlobs.Add(record);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    public async Task SaveMediaWithEditAsync(MediaBlobRecord record, MediaEditRecord edit, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.MediaBlobs.AnyAsync(m => m.Sha256 == record.Sha256, ct)) db.MediaBlobs.Add(record);
        if (!await db.MediaEdits.AnyAsync(e => e.Id == edit.Id, ct)) db.MediaEdits.Add(edit);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); db.ChangeTracker.Clear();
    }

    public async Task<IReadOnlyList<MediaEditRecord>> GetMediaEditsAsync(string sha, CancellationToken ct) =>
        await db.MediaEdits.AsNoTracking().Where(e => e.Sha256 == sha).OrderByDescending(e => e.CreatedAt).Take(100).ToListAsync(ct);

    private static ArticleRevisionRecord Snapshot(ArticleRecord record, string summary) => new()
    {
        Id = Guid.NewGuid(),
        ArticleId = record.Id,
        Revision = record.Revision,
        SavedAt = record.UpdatedAt,
        Summary = summary,
        Json = record.Json,
    };
}
