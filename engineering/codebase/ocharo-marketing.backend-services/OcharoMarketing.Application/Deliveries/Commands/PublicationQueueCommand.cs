using OcharoMarketing.Application.Deliveries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Commands;

/// <summary>Represents a command that queues a plan's publication.</summary>
public sealed record PublicationQueueCommand : ICommand<AppResult<PublicationOperationModel>>
{
    /// <summary>Gets the plan.</summary>
    public required string ScheduleId { get; init; }

    /// <summary>Gets the frozen revision and connection to publish with.</summary>
    public required QueuePublicationInput Input { get; init; }
}
