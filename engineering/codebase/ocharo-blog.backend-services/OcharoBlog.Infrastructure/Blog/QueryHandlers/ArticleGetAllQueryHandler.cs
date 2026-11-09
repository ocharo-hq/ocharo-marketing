using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="ArticleGetAllQuery"/>.</summary>
internal sealed class ArticleGetAllQueryHandler(ArticleService articles)
    : IQueryHandler<ArticleGetAllQuery, AppResult<IReadOnlyList<ArticleSummary>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<ArticleSummary>>> HandleAsync(
        ArticleGetAllQuery request,
        CancellationToken ct)
    {
        var stories = await articles.ListAsync(
            request.Status,
            request.Topic,
            request.Search,
            ct,
            request.Language,
            request.Publication);

        return AppResult<IReadOnlyList<ArticleSummary>>.Ok(stories);
    }
}
