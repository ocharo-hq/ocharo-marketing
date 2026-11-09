using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Commands;

/// <summary>Represents a command that creates one draft post per product from a caption template.</summary>
public sealed record ProductDraftsCreateCommand : ICommand<AppResult<DraftModel[]>>
{
    /// <summary>Gets the products, template and channels.</summary>
    public required BulkDraftInput Input { get; init; }
}
