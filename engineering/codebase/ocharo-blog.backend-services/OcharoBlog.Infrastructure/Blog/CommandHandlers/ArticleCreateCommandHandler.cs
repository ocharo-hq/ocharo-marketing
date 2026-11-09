using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ArticleCreateCommand"/>.</summary>
internal sealed class ArticleCreateCommandHandler(ArticleService articles)
    : ICommandHandler<ArticleCreateCommand, AppResult<ArticleDetail>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ArticleDetail>> HandleAsync(ArticleCreateCommand request, CancellationToken ct)
    {
        return (await articles.CreateAsync(request.Input, ct)).ToAppResult();
    }
}
