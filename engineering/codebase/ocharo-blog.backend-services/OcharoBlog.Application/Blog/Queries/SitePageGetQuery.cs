using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a page of the public blog.</summary>
public sealed record SitePageGetQuery : IQuery<AppResult<SitePageLookup>>
{
    /// <summary>Gets the page's path.</summary>
    public required string Path { get; init; }
}
