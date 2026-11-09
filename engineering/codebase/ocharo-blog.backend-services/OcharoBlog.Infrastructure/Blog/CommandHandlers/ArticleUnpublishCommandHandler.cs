using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleUnpublishCommand"/>.</summary>
internal sealed class ArticleUnpublishCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleUnpublishCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleUnpublishCommand request, CancellationToken ct)
    {
        return (await articles.UnpublishAsync(request.Id, request.ExpectedRevision, ct)).ToAppResult();
    }
}
