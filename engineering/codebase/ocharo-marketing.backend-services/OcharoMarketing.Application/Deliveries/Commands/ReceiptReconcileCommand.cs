using OcharoMarketing.Application.Deliveries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Commands;

/// <summary>Represents a command that reconciles a publication's receipt with its provider.</summary>
public sealed record ReceiptReconcileCommand : ICommand<AppResult<PublicationOperationModel>>
{
    /// <summary>Gets the plan.</summary>
    public required string ScheduleId { get; init; }
}
