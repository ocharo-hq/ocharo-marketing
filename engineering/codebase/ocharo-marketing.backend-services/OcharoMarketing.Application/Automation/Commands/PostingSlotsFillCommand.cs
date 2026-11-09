using OcharoMarketing.Application.Automation.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Commands;

/// <summary>Represents a command that fills the posting slots ahead with approved drafts.</summary>
public sealed record PostingSlotsFillCommand : ICommand<AppResult<SlotFillReport>>
{
    /// <summary>Gets how many days ahead to plan.</summary>
    public required int Days { get; init; }
}
