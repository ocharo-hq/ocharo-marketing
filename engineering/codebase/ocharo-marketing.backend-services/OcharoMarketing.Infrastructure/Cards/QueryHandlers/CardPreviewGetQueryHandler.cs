using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Queries;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Cards.QueryHandlers;

/// <summary>Handles <see cref="CardPreviewGetQuery"/>.</summary>
internal sealed class CardPreviewGetQueryHandler(MarketingService posts)
    : IQueryHandler<CardPreviewGetQuery, AppResult<CardPreview>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<CardPreview>> HandleAsync(CardPreviewGetQuery request, CancellationToken ct)
    {
        var data = await posts.PreviewAsync(request.Input, ct);

        return AppResult<CardPreview>.Ok(data);
    }
}
