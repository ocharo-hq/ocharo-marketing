using OcharoMarketing.Application.Operations.Commands;
using OcharoMarketing.Application.Operations.Models;
using OcharoMarketing.Infrastructure.Operations.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Operations.CommandHandlers;

/// <summary>Handles <see cref="ChannelStrategySaveCommand"/>.</summary>
internal sealed class ChannelStrategySaveCommandHandler(OperationsService operations)
    : ICommandHandler<ChannelStrategySaveCommand, AppResult<ChannelStrategyModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ChannelStrategyModel>> HandleAsync(
        ChannelStrategySaveCommand request,
        CancellationToken ct)
    {
        var data = await operations.SaveChannelAsync(request.Channel, request.Input, ct);

        return AppResult<ChannelStrategyModel>.Ok(data);
    }
}
