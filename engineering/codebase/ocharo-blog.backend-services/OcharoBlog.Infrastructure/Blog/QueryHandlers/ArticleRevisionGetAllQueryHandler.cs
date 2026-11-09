using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="ArticleRevisionGetAllQuery"/>.</summary>
internal sealed class ArticleRevisionGetAllQueryHandler(ArticleService articles)
    : IQueryHandler<ArticleRevisionGetAllQuery, AppResult<IReadOnlyList<RevisionSummary>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<RevisionSummary>>> HandleAsync(
        ArticleRevisionGetAllQuery request,
        CancellationToken ct)
    {
        return (await articles.RevisionsAsync(request.ArticleId, ct)).ToAppResult();
    }
}
