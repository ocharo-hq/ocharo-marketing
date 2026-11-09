using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Posts.Commands;

/// <summary>Represents a command to plan an approved draft revision on a channel.</summary>
public sealed record DraftScheduleCommand : ICommand<AppResult<ScheduleModel>>
{
    /// <summary>Gets the draft.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the revision, channel and time to plan.</summary>
    public required ScheduleInput Input { get; init; }
}
