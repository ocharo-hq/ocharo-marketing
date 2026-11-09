using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="ArticleGetByIdQuery"/>.</summary>
internal sealed class ArticleGetByIdQueryHandler(ArticleService articles)
    : IQueryHandler<ArticleGetByIdQuery, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleGetByIdQuery request, CancellationToken ct)
    {
        return (await articles.GetAsync(request.Id, ct)).ToAppResult();
    }
}
