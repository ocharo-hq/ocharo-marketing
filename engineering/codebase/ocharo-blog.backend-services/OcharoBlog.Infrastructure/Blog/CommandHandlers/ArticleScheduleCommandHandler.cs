using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleScheduleCommand"/>: a missing time is refused before the story is read.</summary>
internal sealed class ArticleScheduleCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleScheduleCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleScheduleCommand request, CancellationToken ct)
    {
        if (request.PublishAt is not { } publishAt)
            return AppResult<ArticleDetail>.Fail(BlogErrors.Invalid("Choose a publication time."));

        return (await articles.ScheduleAsync(request.Id, request.ExpectedRevision, publishAt, ct)).ToAppResult();
    }
}
