using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="MediaContentGetQuery"/>.</summary>
internal sealed class MediaContentGetQueryHandler(IMediaStore media)
    : IQueryHandler<MediaContentGetQuery, AppResult<MediaOpening>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<MediaOpening>> HandleAsync(MediaContentGetQuery request, CancellationToken ct)
    {
        var content = await media.OpenAsync(request.Sha256, request.Preview, ct);

        return AppResult<MediaOpening>.Ok(new MediaOpening(content));
    }
}
