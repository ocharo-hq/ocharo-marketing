using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="TopicDeleteCommand"/>.</summary>
internal sealed class TopicDeleteCommandHandler(TopicService topics)
    : ICommandHandler<TopicDeleteCommand, AppResult<string>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<string>> HandleAsync(TopicDeleteCommand request, CancellationToken ct)
    {
        return (await topics.DeleteAsync(request.Key, ct)).ToAppResult();
    }
}
