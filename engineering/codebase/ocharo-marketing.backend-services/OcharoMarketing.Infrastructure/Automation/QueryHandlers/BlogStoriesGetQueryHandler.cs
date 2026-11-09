using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Application.Automation.Queries;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.QueryHandlers;

/// <summary>Handles <see cref="BlogStoriesGetQuery"/>.</summary>
internal sealed class BlogStoriesGetQueryHandler(BlogStoryService blog)
    : IQueryHandler<BlogStoriesGetQuery, AppResult<BlogStoryModel[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<BlogStoryModel[]>> HandleAsync(BlogStoriesGetQuery request, CancellationToken ct)
    {
        var data = await blog.ListAsync(ct);

        return AppResult<BlogStoryModel[]>.Ok(data);
    }
}
