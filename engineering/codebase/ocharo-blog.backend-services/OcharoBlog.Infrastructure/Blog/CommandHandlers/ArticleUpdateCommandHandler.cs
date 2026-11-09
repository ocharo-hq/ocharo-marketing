using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleUpdateCommand"/>: changing published or scheduled content needs approval
/// permission.</summary>
internal sealed class ArticleUpdateCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleUpdateCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleUpdateCommand request, CancellationToken ct)
    {
        if ((await articles.GetAsync(request.Id, ct)).IsFailure(out var error, out var current))
            return AppResult<ArticleDetail>.Fail(error);
        if (current.Document.Status is ArticleStatus.Published or ArticleStatus.Scheduled && !request.CanApprove)
            return AppResult<ArticleDetail>.Fail(
                BlogErrors.Forbidden("Editing published or scheduled content requires approval permission."));

        return (await articles.UpdateAsync(request.Id, request.Input, ct)).ToAppResult();
    }
}
