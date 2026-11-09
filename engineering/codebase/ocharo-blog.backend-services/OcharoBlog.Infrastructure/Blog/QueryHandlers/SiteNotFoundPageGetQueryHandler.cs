using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Site;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="SiteNotFoundPageGetQuery"/>.</summary>
internal sealed class SiteNotFoundPageGetQueryHandler(SiteService site)
    : IQueryHandler<SiteNotFoundPageGetQuery, AppResult<SitePage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SitePage>> HandleAsync(SiteNotFoundPageGetQuery request, CancellationToken ct)
    {
        return AppResult<SitePage>.Ok(await site.NotFoundAsync(request.Path, ct));
    }
}
