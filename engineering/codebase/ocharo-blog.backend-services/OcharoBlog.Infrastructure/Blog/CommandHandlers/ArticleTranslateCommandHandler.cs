using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleTranslateCommand"/>.</summary>
internal sealed class ArticleTranslateCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleTranslateCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleTranslateCommand request, CancellationToken ct)
    {
        return (await articles.TranslateAsync(request.Id, request.Language, ct)).ToAppResult();
    }
}
