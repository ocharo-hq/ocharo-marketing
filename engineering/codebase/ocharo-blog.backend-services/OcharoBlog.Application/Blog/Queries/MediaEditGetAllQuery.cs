using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get the edits saved from an image, newest first.</summary>
public sealed record MediaEditGetAllQuery : IQuery<AppResult<IReadOnlyList<MediaEditRecord>>>
{
    /// <summary>Gets the original image's SHA-256.</summary>
    public required string Sha256 { get; init; }
}
