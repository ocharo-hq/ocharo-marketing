using OcharoMarketing.Application.Operations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Operations.Commands;

/// <summary>Represents a command that records a manual observation of a published post.</summary>
public sealed record EngagementRecordCommand : ICommand<AppResult<EngagementObservationModel>>
{
    /// <summary>Gets the observation.</summary>
    public required EngagementInput Input { get; init; }

    /// <summary>Gets who recorded it.</summary>
    public required string Actor { get; init; }
}
