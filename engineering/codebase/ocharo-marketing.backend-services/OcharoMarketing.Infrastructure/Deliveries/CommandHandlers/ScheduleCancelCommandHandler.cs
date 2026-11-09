using OcharoMarketing.Application.Deliveries.Commands;
using OcharoMarketing.Application.Deliveries.Repositories;
using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.CommandHandlers;

/// <summary>Handles <see cref="ScheduleCancelCommand"/>.</summary>
internal sealed class ScheduleCancelCommandHandler(IDeliveryRepository delivery, TimeProvider time)
    : ICommandHandler<ScheduleCancelCommand, AppResult<ScheduleModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ScheduleModel>> HandleAsync(ScheduleCancelCommand request, CancellationToken ct)
    {
        var schedule = await delivery.CancelAsync(request.Id, time.GetUtcNow(), ct);

        return AppResult<ScheduleModel>.Ok(schedule);
    }
}
