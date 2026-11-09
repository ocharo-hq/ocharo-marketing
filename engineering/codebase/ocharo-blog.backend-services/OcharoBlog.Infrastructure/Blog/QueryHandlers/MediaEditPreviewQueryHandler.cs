using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="MediaEditPreviewQuery"/>.</summary>
internal sealed class MediaEditPreviewQueryHandler(IMediaEditingService editing)
    : IQueryHandler<MediaEditPreviewQuery, AppResult<EditedImage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<EditedImage>> HandleAsync(MediaEditPreviewQuery request, CancellationToken ct)
    {
        return (await editing.PreviewAsync(request.Sha256, request.Recipe, ct)).ToAppResult();
    }
}
