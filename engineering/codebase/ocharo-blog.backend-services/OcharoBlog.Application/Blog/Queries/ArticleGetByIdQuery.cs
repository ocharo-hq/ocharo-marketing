using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a story with its revision stamp.</summary>
public sealed record ArticleGetByIdQuery : IQuery<AppResult<ArticleDetail>>
{
    /// <summary>Gets the story's id.</summary>
    public required Guid Id { get; init; }
}
