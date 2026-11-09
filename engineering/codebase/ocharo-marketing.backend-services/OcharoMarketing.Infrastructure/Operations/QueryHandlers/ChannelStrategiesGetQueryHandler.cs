using OcharoMarketing.Application.Operations.Models;
using OcharoMarketing.Application.Operations.Queries;
using OcharoMarketing.Infrastructure.Operations.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Operations.QueryHandlers;

/// <summary>Handles <see cref="ChannelStrategiesGetQuery"/>.</summary>
internal sealed class ChannelStrategiesGetQueryHandler(OperationsService operations)
    : IQueryHandler<ChannelStrategiesGetQuery, AppResult<ChannelStrategyModel[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ChannelStrategyModel[]>> HandleAsync(
        ChannelStrategiesGetQuery request,
        CancellationToken ct)
    {
        var data = await operations.ChannelsAsync(ct);

        return AppResult<ChannelStrategyModel[]>.Ok(data);
    }
}
