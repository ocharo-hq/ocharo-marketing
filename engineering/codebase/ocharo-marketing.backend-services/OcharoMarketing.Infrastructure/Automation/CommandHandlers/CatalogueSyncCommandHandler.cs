using OcharoMarketing.Application.Automation.Commands;
using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.CommandHandlers;

/// <summary>Handles <see cref="CatalogueSyncCommand"/>.</summary>
internal sealed class CatalogueSyncCommandHandler(AutomationRunner runner)
    : ICommandHandler<CatalogueSyncCommand, AppResult<CatalogueSyncReport>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<CatalogueSyncReport>> HandleAsync(
        CatalogueSyncCommand request,
        CancellationToken ct)
    {
        var data = await runner.SyncCatalogueAsync(ct);

        return AppResult<CatalogueSyncReport>.Ok(data);
    }
}
