using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="MediaEditGetAllQuery"/>.</summary>
internal sealed class MediaEditGetAllQueryHandler(IBlogRepository repository)
    : IQueryHandler<MediaEditGetAllQuery, AppResult<IReadOnlyList<MediaEditRecord>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<MediaEditRecord>>> HandleAsync(
        MediaEditGetAllQuery request,
        CancellationToken ct)
    {
        return AppResult<IReadOnlyList<MediaEditRecord>>.Ok(await repository.GetMediaEditsAsync(request.Sha256, ct));
    }
}
