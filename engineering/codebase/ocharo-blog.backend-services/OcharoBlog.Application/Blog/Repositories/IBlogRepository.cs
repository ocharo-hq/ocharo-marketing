using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Repositories;

/// <summary>Durable blog state; saves are revision-checked (stale → 409).</summary>
public interface IBlogRepository
{
    Task<IReadOnlyList<ArticleRecord>> GetArticlesAsync(CancellationToken ct);
    Task<ArticleRecord?> FindArticleAsync(Guid id, CancellationToken ct);
    Task<ArticleRecord?> FindArticleBySlugAsync(string slug, CancellationToken ct);
    Task<ArticleRecord> InsertArticleAsync(ArticleRecord record, string summary, CancellationToken ct);
    Task<ArticleRecord> UpdateArticleAsync(ArticleRecord record, int expectedRevision, string summary, CancellationToken ct);
    Task<IReadOnlyList<ArticleRevisionRecord>> GetRevisionsAsync(Guid articleId, CancellationToken ct);
    Task<ArticleRevisionRecord?> FindRevisionAsync(Guid articleId, int revision, CancellationToken ct);

    Task<IReadOnlyList<TopicRecord>> GetTopicsAsync(CancellationToken ct);
    Task SaveTopicAsync(TopicRecord record, CancellationToken ct);
    Task DeleteTopicAsync(string key, CancellationToken ct);

    Task<SiteSettingsRecord?> FindSettingsAsync(CancellationToken ct);
    Task<SiteSettingsRecord> SaveSettingsAsync(SiteSettingsRecord record, int expectedRevision, CancellationToken ct);

    Task<MediaBlobRecord?> FindMediaAsync(string sha256, CancellationToken ct);
    Task<IReadOnlyList<MediaBlobRecord>> GetMediaAsync(CancellationToken ct);
    Task SaveMediaAsync(MediaBlobRecord record, CancellationToken ct);
    Task SaveMediaWithEditAsync(MediaBlobRecord record, MediaEditRecord edit, CancellationToken ct);
    Task<IReadOnlyList<MediaEditRecord>> GetMediaEditsAsync(string sha, CancellationToken ct);
}
