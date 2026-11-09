using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Application.Automation.Queries;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.QueryHandlers;

/// <summary>Handles <see cref="AutomationSettingsGetQuery"/>.</summary>
internal sealed class AutomationSettingsGetQueryHandler(AutomationService automation)
    : IQueryHandler<AutomationSettingsGetQuery, AppResult<AutomationSettingsModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<AutomationSettingsModel>> HandleAsync(
        AutomationSettingsGetQuery request,
        CancellationToken ct)
    {
        var data = await automation.GetAsync(ct);

        return AppResult<AutomationSettingsModel>.Ok(data);
    }
}
