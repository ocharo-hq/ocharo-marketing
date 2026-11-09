using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="TopicGetAllQuery"/>.</summary>
internal sealed class TopicGetAllQueryHandler(TopicService topics)
    : IQueryHandler<TopicGetAllQuery, AppResult<IReadOnlyList<TopicDocument>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<TopicDocument>>> HandleAsync(
        TopicGetAllQuery request,
        CancellationToken ct)
    {
        return AppResult<IReadOnlyList<TopicDocument>>.Ok(await topics.ListAsync(ct));
    }
}
