using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to create a draft story.</summary>
public sealed record ArticleCreateCommand : ICommand<AppResult<ArticleDetail>>
{
    /// <summary>Gets the story as the editor wrote it.</summary>
    public required ArticleInput Input { get; init; }
}
