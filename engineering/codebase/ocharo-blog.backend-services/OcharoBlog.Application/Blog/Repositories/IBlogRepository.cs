using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace OcharoBlog.Application.Blog.Repositories;

/// <summary>Durable blog state; saves are revision-checked, a stale one refused as a conflict.</summary>
public interface IBlogRepository
{
    Task<IReadOnlyList<ArticleRecord>> GetArticlesAsync(CancellationToken ct);
    Task<ArticleRecord?> FindArticleAsync(Guid id, CancellationToken ct);
    Task<ArticleRecord?> FindArticleBySlugAsync(string slug, CancellationToken ct);
    /// <summary>Inserts a story with its first revision; refuses a slug another story holds.</summary>
    Task<Result<ArticleRecord>> InsertArticleAsync(ArticleRecord record, string summary, CancellationToken ct);

    /// <summary>Saves a story's next revision; refuses a missing story, a stale revision or a slug another story
    /// holds.</summary>
    Task<Result<ArticleRecord>> UpdateArticleAsync(
        ArticleRecord record,
        int expectedRevision,
        string summary,
        CancellationToken ct);

    Task<IReadOnlyList<ArticleRevisionRecord>> GetRevisionsAsync(Guid articleId, CancellationToken ct);
    Task<ArticleRevisionRecord?> FindRevisionAsync(Guid articleId, int revision, CancellationToken ct);

    Task<IReadOnlyList<TopicRecord>> GetTopicsAsync(CancellationToken ct);
    Task SaveTopicAsync(TopicRecord record, CancellationToken ct);
    Task DeleteTopicAsync(string key, CancellationToken ct);

    Task<SiteSettingsRecord?> FindSettingsAsync(CancellationToken ct);
    /// <summary>Saves the settings' next revision; refuses a stale one.</summary>
    Task<Result<SiteSettingsRecord>> SaveSettingsAsync(
        SiteSettingsRecord record,
        int expectedRevision,
        CancellationToken ct);

    Task<MediaBlobRecord?> FindMediaAsync(string sha256, CancellationToken ct);
    Task<IReadOnlyList<MediaBlobRecord>> GetMediaAsync(CancellationToken ct);
    Task SaveMediaAsync(MediaBlobRecord record, CancellationToken ct);
    /// <summary>The files stored before Files, which still have no file id.</summary>
    Task<IReadOnlyList<MediaBlobRecord>> GetMediaWithoutFileAsync(CancellationToken ct);
    /// <summary>Records the Files file that holds a stored file's bytes.</summary>
    Task SetMediaFileAsync(string sha256, string fileId, CancellationToken ct);
    Task SaveMediaWithEditAsync(MediaBlobRecord record, MediaEditRecord edit, CancellationToken ct);
    Task<IReadOnlyList<MediaEditRecord>> GetMediaEditsAsync(string sha, CancellationToken ct);
}
