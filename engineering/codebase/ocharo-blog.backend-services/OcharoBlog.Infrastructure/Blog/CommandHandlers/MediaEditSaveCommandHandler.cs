using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="MediaEditSaveCommand"/>: a session that no longer names its organization and account is
/// refused before the image is read.</summary>
internal sealed class MediaEditSaveCommandHandler(IMediaEditingService editing)
    : ICommandHandler<MediaEditSaveCommand, AppResult<MediaBlobRecord>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<MediaBlobRecord>> HandleAsync(MediaEditSaveCommand request, CancellationToken ct)
    {
        if (request.Actor is not { } actor)
            return AppResult<MediaBlobRecord>.Fail(
                BlogErrors.Unauthorized("Your connection expired. Your original image is unchanged."));

        return (await editing.SaveAsync(request.Sha256, request.Recipe, actor, ct)).ToAppResult();
    }
}
