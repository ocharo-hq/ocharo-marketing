using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Posts.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Posts.QueryHandlers;

/// <summary>Handles <see cref="DraftExportGetQuery"/>.</summary>
internal sealed class DraftExportGetQueryHandler(IAssetService assets)
    : IQueryHandler<DraftExportGetQuery, AppResult<byte[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<byte[]>> HandleAsync(DraftExportGetQuery request, CancellationToken ct)
    {
        var data = await assets.ExportDraftAsync(request.Id, ct);

        return AppResult<byte[]>.Ok(data);
    }
}
