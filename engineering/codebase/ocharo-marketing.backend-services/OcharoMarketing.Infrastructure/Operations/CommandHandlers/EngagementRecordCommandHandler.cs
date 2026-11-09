using OcharoMarketing.Application.Operations.Commands;
using OcharoMarketing.Application.Operations.Models;
using OcharoMarketing.Infrastructure.Operations.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Operations.CommandHandlers;

/// <summary>Handles <see cref="EngagementRecordCommand"/>.</summary>
internal sealed class EngagementRecordCommandHandler(OperationsService operations)
    : ICommandHandler<EngagementRecordCommand, AppResult<EngagementObservationModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<EngagementObservationModel>> HandleAsync(
        EngagementRecordCommand request,
        CancellationToken ct)
    {
        var data = await operations.RecordAsync(request.Input, request.Actor, ct);

        return AppResult<EngagementObservationModel>.Ok(data);
    }
}
