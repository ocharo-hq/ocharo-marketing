using OcharoMarketing.Application.Deliveries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Queries;

/// <summary>Represents a query for a plan's publication operation.</summary>
public sealed record PublicationGetQuery : IQuery<AppResult<PublicationOperationLookup>>
{
    /// <summary>Gets the plan.</summary>
    public required string ScheduleId { get; init; }
}
