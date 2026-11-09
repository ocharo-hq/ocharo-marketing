using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Posts.Commands;

/// <summary>Represents a command to approve a draft revision.</summary>
public sealed record DraftApproveCommand : ICommand<AppResult<DraftModel>>
{
    /// <summary>Gets the draft.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the revision the approver read.</summary>
    public required int ExpectedRevision { get; init; }
}
