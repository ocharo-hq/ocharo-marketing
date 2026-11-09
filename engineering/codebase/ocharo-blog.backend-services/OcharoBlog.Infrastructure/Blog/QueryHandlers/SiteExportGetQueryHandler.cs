using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Site;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="SiteExportGetQuery"/>.</summary>
internal sealed class SiteExportGetQueryHandler(SiteService site)
    : IQueryHandler<SiteExportGetQuery, AppResult<byte[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<byte[]>> HandleAsync(SiteExportGetQuery request, CancellationToken ct)
    {
        return AppResult<byte[]>.Ok(await site.ExportAsync(ct));
    }
}
