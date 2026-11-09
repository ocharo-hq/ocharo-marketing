using OcharoBlog.Application.Blog.Site;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get the not-found page a public path no page answers gets.</summary>
public sealed record SiteNotFoundPageGetQuery : IQuery<AppResult<SitePage>>
{
    /// <summary>Gets the path no page answered.</summary>
    public required string Path { get; init; }
}
