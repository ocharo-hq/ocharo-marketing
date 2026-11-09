using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to store an uploaded image once per SHA-256.</summary>
public sealed record MediaUploadCommand : ICommand<AppResult<MediaBlobRecord>>
{
    /// <summary>Gets the image's bytes.</summary>
    public required byte[] Bytes { get; init; }

    /// <summary>Gets the name the upload carried.</summary>
    public required string FileName { get; init; }
}
