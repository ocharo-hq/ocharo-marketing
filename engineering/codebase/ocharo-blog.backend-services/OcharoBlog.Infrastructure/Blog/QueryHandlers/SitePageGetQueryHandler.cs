using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Site;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="SitePageGetQuery"/>.</summary>
internal sealed class SitePageGetQueryHandler(SiteService site)
    : IQueryHandler<SitePageGetQuery, AppResult<SitePageLookup>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SitePageLookup>> HandleAsync(SitePageGetQuery request, CancellationToken ct)
    {
        return AppResult<SitePageLookup>.Ok(new SitePageLookup(await site.RenderAsync(request.Path, ct)));
    }
}
