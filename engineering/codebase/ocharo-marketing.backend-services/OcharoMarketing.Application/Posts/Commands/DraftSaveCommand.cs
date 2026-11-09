using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Posts.Commands;

/// <summary>Represents a command to create a draft, or to save a new revision of one.</summary>
public sealed record DraftSaveCommand : ICommand<AppResult<DraftModel>>
{
    /// <summary>Gets the draft to revise; none creates a draft.</summary>
    public string? Id { get; init; }

    /// <summary>Gets the draft's content and the revision it was read at.</summary>
    public required DraftInput Input { get; init; }
}
