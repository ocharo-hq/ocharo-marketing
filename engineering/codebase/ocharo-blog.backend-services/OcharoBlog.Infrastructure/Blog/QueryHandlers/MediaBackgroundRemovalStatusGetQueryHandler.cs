using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="MediaBackgroundRemovalStatusGetQuery"/>.</summary>
internal sealed class MediaBackgroundRemovalStatusGetQueryHandler(IMediaEditingService editing)
    : IQueryHandler<MediaBackgroundRemovalStatusGetQuery, AppResult<ImageWorkerStatus>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ImageWorkerStatus>> HandleAsync(
        MediaBackgroundRemovalStatusGetQuery request,
        CancellationToken ct)
    {
        return AppResult<ImageWorkerStatus>.Ok(await editing.BackgroundStatusAsync(ct));
    }
}
