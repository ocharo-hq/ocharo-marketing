using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticlePublishCommand"/>.</summary>
internal sealed class ArticlePublishCommandHandler(ArticleService articles)
    : ICommandHandler<ArticlePublishCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticlePublishCommand request, CancellationToken ct)
    {
        return (await articles.PublishAsync(request.Id, request.ExpectedRevision, ct)).ToAppResult();
    }
}
