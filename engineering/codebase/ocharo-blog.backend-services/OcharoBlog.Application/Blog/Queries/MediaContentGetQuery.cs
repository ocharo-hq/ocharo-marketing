using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get where a stored image's bytes come from.</summary>
public sealed record MediaContentGetQuery : IQuery<AppResult<MediaOpening>>
{
    /// <summary>Gets the image's SHA-256, as its route names it.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets whether the web preview is asked for instead of the original.</summary>
    public bool Preview { get; init; }
}
