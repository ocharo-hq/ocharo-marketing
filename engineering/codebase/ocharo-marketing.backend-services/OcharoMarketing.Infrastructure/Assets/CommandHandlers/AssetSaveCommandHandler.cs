using OcharoMarketing.Application.Assets.Commands;
using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Assets.CommandHandlers;

/// <summary>Handles <see cref="AssetSaveCommand"/>: refuses an empty or oversized upload, then stores it.</summary>
internal sealed class AssetSaveCommandHandler(IAssetService assets)
    : ICommandHandler<AssetSaveCommand, AppResult<AssetModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<AssetModel>> HandleAsync(AssetSaveCommand request, CancellationToken ct)
    {
        if (request.Bytes.Length is <= 0 or > AssetSaveCommand.MaximumBytes)
            return AppResult<AssetModel>.Fail(MarketingErrors.TooLarge("Choose a nonempty image smaller than 25 MB."));

        var asset = await assets.SaveAsync(
            request.Bytes,
            request.FileName,
            request.Provenance,
            request.Role,
            ct);

        return AppResult<AssetModel>.Ok(asset);
    }
}
