using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleArchiveCommand"/>.</summary>
internal sealed class ArticleArchiveCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleArchiveCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleArchiveCommand request, CancellationToken ct)
    {
        return (await articles.ArchiveAsync(request.Id, request.ExpectedRevision, ct)).ToAppResult();
    }
}
