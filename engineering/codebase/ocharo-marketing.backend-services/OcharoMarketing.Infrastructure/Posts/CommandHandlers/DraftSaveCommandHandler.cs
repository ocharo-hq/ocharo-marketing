using OcharoMarketing.Application.Posts.Commands;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Posts.CommandHandlers;

/// <summary>Handles <see cref="DraftSaveCommand"/>.</summary>
internal sealed class DraftSaveCommandHandler(MarketingService posts)
    : ICommandHandler<DraftSaveCommand, AppResult<DraftModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<DraftModel>> HandleAsync(DraftSaveCommand request, CancellationToken ct)
    {
        var data = await posts.SaveDraftAsync(request.Id, request.Input, ct);

        return AppResult<DraftModel>.Ok(data);
    }
}
