using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a page of a public app: Atlas, Wear or the Catalogue.</summary>
public sealed record PublicationPageGetQuery : IQuery<AppResult<SitePageLookup>>
{
    /// <summary>Gets the page's path, from the app's root.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the page's query arguments.</summary>
    public required IReadOnlyDictionary<string, string> Arguments { get; init; }

    /// <summary>Gets whether the page renders as an editor's preview, drafts included.</summary>
    public bool Preview { get; init; }
}
