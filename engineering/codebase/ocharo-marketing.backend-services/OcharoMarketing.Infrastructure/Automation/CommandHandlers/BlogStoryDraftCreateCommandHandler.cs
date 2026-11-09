using OcharoMarketing.Application.Automation.Commands;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.CommandHandlers;

/// <summary>Handles <see cref="BlogStoryDraftCreateCommand"/>.</summary>
internal sealed class BlogStoryDraftCreateCommandHandler(BlogStoryService blog)
    : ICommandHandler<BlogStoryDraftCreateCommand, AppResult<DraftModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<DraftModel>> HandleAsync(BlogStoryDraftCreateCommand request, CancellationToken ct)
    {
        var data = await blog.DraftAsync(request.Input, ct);

        return AppResult<DraftModel>.Ok(data);
    }
}
