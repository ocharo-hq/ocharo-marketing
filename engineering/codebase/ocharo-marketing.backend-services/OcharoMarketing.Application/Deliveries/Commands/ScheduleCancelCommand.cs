using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Commands;

/// <summary>Represents a command that cancels a plan, and its publication while none has been dispatched.</summary>
public sealed record ScheduleCancelCommand : ICommand<AppResult<ScheduleModel>>
{
    /// <summary>Gets the plan.</summary>
    public required string Id { get; init; }
}
