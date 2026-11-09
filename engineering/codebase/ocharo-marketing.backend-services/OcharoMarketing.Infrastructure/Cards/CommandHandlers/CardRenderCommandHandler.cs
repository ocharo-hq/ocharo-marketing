using OcharoMarketing.Application.Cards.Commands;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Cards.CommandHandlers;

/// <summary>Handles <see cref="CardRenderCommand"/>.</summary>
internal sealed class CardRenderCommandHandler(MarketingService posts)
    : ICommandHandler<CardRenderCommand, AppResult<GenerationJobModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<GenerationJobModel>> HandleAsync(CardRenderCommand request, CancellationToken ct)
    {
        var data = await posts.RenderAsync(request.Input, request.Requester, ct);

        return AppResult<GenerationJobModel>.Ok(data);
    }
}
