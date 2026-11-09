using OcharoMarketing.Application.Posts.Commands;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Posts.CommandHandlers;

/// <summary>Handles <see cref="DraftScheduleCommand"/>.</summary>
internal sealed class DraftScheduleCommandHandler(MarketingService posts)
    : ICommandHandler<DraftScheduleCommand, AppResult<ScheduleModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ScheduleModel>> HandleAsync(DraftScheduleCommand request, CancellationToken ct)
    {
        var data = await posts.ScheduleAsync(request.Id, request.Input, ct);

        return AppResult<ScheduleModel>.Ok(data);
    }
}
