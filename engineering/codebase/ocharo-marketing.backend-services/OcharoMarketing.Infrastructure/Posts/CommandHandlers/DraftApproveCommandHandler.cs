using OcharoMarketing.Application.Posts.Commands;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Posts.CommandHandlers;

/// <summary>Handles <see cref="DraftApproveCommand"/>.</summary>
internal sealed class DraftApproveCommandHandler(MarketingService posts)
    : ICommandHandler<DraftApproveCommand, AppResult<DraftModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<DraftModel>> HandleAsync(DraftApproveCommand request, CancellationToken ct)
    {
        var data = await posts.ApproveAsync(request.Id, request.ExpectedRevision, ct);

        return AppResult<DraftModel>.Ok(data);
    }
}
