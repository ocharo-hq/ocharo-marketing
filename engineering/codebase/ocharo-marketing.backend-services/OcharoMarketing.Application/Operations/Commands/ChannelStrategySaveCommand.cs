using OcharoMarketing.Application.Operations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Operations.Commands;

/// <summary>Represents a command that saves a channel's strategy.</summary>
public sealed record ChannelStrategySaveCommand : ICommand<AppResult<ChannelStrategyModel>>
{
    /// <summary>Gets the channel.</summary>
    public required string Channel { get; init; }

    /// <summary>Gets the strategy, carrying the revision it was read at.</summary>
    public required ChannelStrategyModel Input { get; init; }
}
