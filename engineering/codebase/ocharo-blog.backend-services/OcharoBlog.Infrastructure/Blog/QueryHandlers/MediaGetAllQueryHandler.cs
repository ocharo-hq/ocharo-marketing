using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="MediaGetAllQuery"/>.</summary>
internal sealed class MediaGetAllQueryHandler(IBlogRepository repository)
    : IQueryHandler<MediaGetAllQuery, AppResult<IReadOnlyList<MediaBlobRecord>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<MediaBlobRecord>>> HandleAsync(
        MediaGetAllQuery request,
        CancellationToken ct)
    {
        return AppResult<IReadOnlyList<MediaBlobRecord>>.Ok(await repository.GetMediaAsync(ct));
    }
}
