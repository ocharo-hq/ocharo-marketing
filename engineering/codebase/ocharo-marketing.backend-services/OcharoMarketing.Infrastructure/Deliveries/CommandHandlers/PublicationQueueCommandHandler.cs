using OcharoMarketing.Application.Deliveries.Commands;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.CommandHandlers;

/// <summary>Handles <see cref="PublicationQueueCommand"/>.</summary>
internal sealed class PublicationQueueCommandHandler(PublicationService publication)
    : ICommandHandler<PublicationQueueCommand, AppResult<PublicationOperationModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<PublicationOperationModel>> HandleAsync(
        PublicationQueueCommand request,
        CancellationToken ct)
    {
        var data = await publication.QueueAsync(request.ScheduleId, request.Input, ct);

        return AppResult<PublicationOperationModel>.Ok(data);
    }
}
