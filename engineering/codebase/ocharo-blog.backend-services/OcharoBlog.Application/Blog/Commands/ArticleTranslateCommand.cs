using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to create a draft edition of a story in another language.</summary>
public sealed record ArticleTranslateCommand : ICommand<AppResult<ArticleDetail>>
{
    /// <summary>Gets the source story's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the edition's language.</summary>
    public required string Language { get; init; }
}
