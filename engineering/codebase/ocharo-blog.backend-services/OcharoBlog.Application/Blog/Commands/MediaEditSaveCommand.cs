using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to store an image edit as an immutable derivative with its receipt.</summary>
public sealed record MediaEditSaveCommand : ICommand<AppResult<MediaBlobRecord>>
{
    /// <summary>Gets the original image's SHA-256.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Gets the edit to apply.</summary>
    public required MediaEditRecipe Recipe { get; init; }

    /// <summary>Gets the organization and account saving the edit; none when the session no longer names
    /// them.</summary>
    public MediaEditActor? Actor { get; init; }
}
