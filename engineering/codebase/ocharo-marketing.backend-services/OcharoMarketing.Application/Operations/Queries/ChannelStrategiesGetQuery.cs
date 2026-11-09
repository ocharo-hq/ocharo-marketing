using OcharoMarketing.Application.Operations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Operations.Queries;

/// <summary>Represents a query for every channel's strategy.</summary>
public sealed record ChannelStrategiesGetQuery : IQuery<AppResult<ChannelStrategyModel[]>>
{
}
