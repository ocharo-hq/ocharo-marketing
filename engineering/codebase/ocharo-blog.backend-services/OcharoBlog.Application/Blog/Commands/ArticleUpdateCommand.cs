using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to save a story's next revision.</summary>
public sealed record ArticleUpdateCommand : ICommand<AppResult<ArticleDetail>>
{
    /// <summary>Gets the story's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the story as the editor wrote it, with the revision it was read at.</summary>
    public required ArticleInput Input { get; init; }

    /// <summary>Gets whether the editor may change published or scheduled content.</summary>
    public bool CanApprove { get; init; }
}
