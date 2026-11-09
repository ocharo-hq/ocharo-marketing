using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Site;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="PublicationPageGetQuery"/>.</summary>
internal sealed class PublicationPageGetQueryHandler(PublicationSiteService publications)
    : IQueryHandler<PublicationPageGetQuery, AppResult<SitePageLookup>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SitePageLookup>> HandleAsync(PublicationPageGetQuery request, CancellationToken ct)
    {
        var page = await publications.RenderAsync(request.Path, request.Arguments, ct, request.Preview);

        return AppResult<SitePageLookup>.Ok(new SitePageLookup(page));
    }
}
