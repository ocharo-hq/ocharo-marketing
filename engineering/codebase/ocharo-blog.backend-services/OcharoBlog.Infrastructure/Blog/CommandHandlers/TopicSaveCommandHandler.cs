using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="TopicSaveCommand"/>.</summary>
internal sealed class TopicSaveCommandHandler(TopicService topics)
    : ICommandHandler<TopicSaveCommand, AppResult<TopicDocument>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<TopicDocument>> HandleAsync(TopicSaveCommand request, CancellationToken ct)
    {
        return (await topics.SaveAsync(request.Topic, ct)).ToAppResult();
    }
}
