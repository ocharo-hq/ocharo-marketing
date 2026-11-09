using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Queries;
using OcharoMarketing.Application.Assets.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Assets.QueryHandlers;

/// <summary>Handles <see cref="AssetGetQuery"/>.</summary>
internal sealed class AssetGetQueryHandler(IAssetService assets)
    : IQueryHandler<AssetGetQuery, AppResult<AssetContent>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<AssetContent>> HandleAsync(AssetGetQuery request, CancellationToken ct)
    {
        var data = await assets.OpenAsync(request.Id, ct);

        return AppResult<AssetContent>.Ok(data);
    }
}
