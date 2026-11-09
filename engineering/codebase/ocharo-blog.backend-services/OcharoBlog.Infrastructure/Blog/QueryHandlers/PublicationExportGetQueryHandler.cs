using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="PublicationExportGetQuery"/>.</summary>
internal sealed class PublicationExportGetQueryHandler(PublicationSiteService publications)
    : IQueryHandler<PublicationExportGetQuery, AppResult<byte[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<byte[]>> HandleAsync(PublicationExportGetQuery request, CancellationToken ct)
    {
        return (await publications.ExportAsync(request.Publication, ct, request.Locale)).ToAppResult();
    }
}
