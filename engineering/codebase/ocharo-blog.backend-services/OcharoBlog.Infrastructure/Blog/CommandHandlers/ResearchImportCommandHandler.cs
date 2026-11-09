using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="ResearchImportCommand"/>.</summary>
internal sealed class ResearchImportCommandHandler(EditorialStarterImporter importer)
    : ICommandHandler<ResearchImportCommand, AppResult<StarterImportResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<StarterImportResult>> HandleAsync(
        ResearchImportCommand request,
        CancellationToken ct)
    {
        return (await importer.ImportAsync(request.ContentRoot, ct)).ToAppResult();
    }
}
