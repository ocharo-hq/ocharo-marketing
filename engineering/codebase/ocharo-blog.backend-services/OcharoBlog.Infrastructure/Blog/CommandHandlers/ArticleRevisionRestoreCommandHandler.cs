using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleRevisionRestoreCommand"/>.</summary>
internal sealed class ArticleRevisionRestoreCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleRevisionRestoreCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(
        ArticleRevisionRestoreCommand request,
        CancellationToken ct)
    {
        var restored = await articles.RestoreAsync(request.Id, request.Revision, request.ExpectedRevision, ct);

        return restored.ToAppResult();
    }
}
