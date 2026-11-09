using OcharoMarketing.Application.Deliveries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Commands;

/// <summary>Represents a command that verifies a delivery connection with its provider.</summary>
public sealed record ConnectionVerifyCommand : ICommand<AppResult<ConnectionModel>>
{
    /// <summary>Gets the connection.</summary>
    public required string Id { get; init; }
}
