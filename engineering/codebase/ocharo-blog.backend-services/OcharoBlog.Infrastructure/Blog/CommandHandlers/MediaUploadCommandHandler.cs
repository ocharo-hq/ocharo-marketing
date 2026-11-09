using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="MediaUploadCommand"/>: an upload holds 1 byte to 20 MiB.</summary>
internal sealed class MediaUploadCommandHandler(IMediaStore media)
    : ICommandHandler<MediaUploadCommand, AppResult<MediaBlobRecord>>
{
    /// <summary>The largest upload Editorial stores: 20 MiB.</summary>
    private const int MaximumBytes = 20 * 1024 * 1024;

    /// <inheritdoc />
    public async ValueTask<AppResult<MediaBlobRecord>> HandleAsync(MediaUploadCommand request, CancellationToken ct)
    {
        if (request.Bytes.Length is <= 0 or > MaximumBytes)
            return AppResult<MediaBlobRecord>.Fail(BlogErrors.TooLarge("Images must contain 1 byte to 20 MiB."));

        return (await media.StoreAsync(request.Bytes, request.FileName, ct)).ToAppResult();
    }
}
