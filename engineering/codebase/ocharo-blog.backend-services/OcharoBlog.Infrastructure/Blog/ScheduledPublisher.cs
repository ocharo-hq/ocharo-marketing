using Microsoft.Extensions.Logging;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Publishes scheduled stories when their time arrives; the host runs a pass every
/// <c>Blog:SchedulerSeconds</c>. State lives in the database, so a restart loses nothing.</summary>
public sealed class ScheduledPublisher(ArticleService articles, SqliteImportGate gate,
    ILogger<ScheduledPublisher> logger)
{
    /// <summary>Publishes every story whose scheduled time has passed; none while the SQLite store waits for its
    /// import.</summary>
    public async Task PublishDueAsync(CancellationToken ct)
    {
        if (gate.Holds("scheduled publishing", logger)) return;
        foreach (var slug in await articles.PublishDueAsync(ct))
            logger.LogInformation("Published scheduled story {Slug}", slug);
    }
}
