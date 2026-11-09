using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a story's saved revisions, newest first.</summary>
public sealed record ArticleRevisionGetAllQuery : IQuery<AppResult<IReadOnlyList<RevisionSummary>>>
{
    /// <summary>Gets the story's id.</summary>
    public required Guid ArticleId { get; init; }
}
