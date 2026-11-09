using OcharoMarketing.Application.Cards.Queries;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Cards.QueryHandlers;

/// <summary>Handles <see cref="CardAtlasGetQuery"/>.</summary>
internal sealed class CardAtlasGetQueryHandler(MarketingService posts)
    : IQueryHandler<CardAtlasGetQuery, AppResult<byte[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<byte[]>> HandleAsync(CardAtlasGetQuery request, CancellationToken ct)
    {
        var data = await posts.ExportAtlasAsync(request.AssetIds, ct);

        return AppResult<byte[]>.Ok(data);
    }
}
