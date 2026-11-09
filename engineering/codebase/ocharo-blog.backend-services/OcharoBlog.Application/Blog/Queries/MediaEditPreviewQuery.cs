using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get an image edit rendered without storing it.</summary>
public sealed record MediaEditPreviewQuery : IQuery<AppResult<EditedImage>>
{
    /// <summary>Gets the original image's SHA-256.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets the edit to render.</summary>
    public required MediaEditRecipe Recipe { get; init; }
}
