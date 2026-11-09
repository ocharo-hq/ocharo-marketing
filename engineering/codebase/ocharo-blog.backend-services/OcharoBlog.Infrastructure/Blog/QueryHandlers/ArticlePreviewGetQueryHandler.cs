using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="ArticlePreviewGetQuery"/>: a blog story through the journal's renderer, any other
/// through its public app's.</summary>
internal sealed class ArticlePreviewGetQueryHandler(
    ArticleService articles,
    SiteService site,
    PublicationSiteService publications)
    : IQueryHandler<ArticlePreviewGetQuery, AppResult<string>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<string>> HandleAsync(ArticlePreviewGetQuery request, CancellationToken ct)
    {
        if ((await articles.GetAsync(request.Id, ct)).IsFailure(out var error, out var item))
            return AppResult<string>.Fail(error);

        var page = item.Document.Publication == "blog"
            ? await site.PreviewAsync(request.Id, ct)
            : await publications.PreviewAsync(request.Id, ct);

        return page.ToAppResult();
    }
}
