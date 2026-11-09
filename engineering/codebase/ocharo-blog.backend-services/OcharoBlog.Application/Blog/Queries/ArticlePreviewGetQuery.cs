using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a story rendered as its public page would show it.</summary>
public sealed record ArticlePreviewGetQuery : IQuery<AppResult<string>>
{
    /// <summary>Gets the story's id.</summary>
    public required Guid Id { get; init; }
}
