using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to archive a story.</summary>
public sealed record ArticleArchiveCommand : ICommand<AppResult<ArticleDetail>>
{
    /// <summary>Gets the story's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the revision the editor acted on.</summary>
    public required int ExpectedRevision { get; init; }
}
