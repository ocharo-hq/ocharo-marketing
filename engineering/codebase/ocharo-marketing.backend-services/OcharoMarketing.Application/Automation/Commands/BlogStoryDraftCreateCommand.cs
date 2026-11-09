using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Commands;

/// <summary>Represents a command that creates a draft post announcing a blog story.</summary>
public sealed record BlogStoryDraftCreateCommand : ICommand<AppResult<DraftModel>>
{
    /// <summary>Gets the story, product and channels.</summary>
    public required StoryDraftInput Input { get; init; }
}
