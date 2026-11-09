using OcharoMarketing.Application.Automation.Commands;
using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.CommandHandlers;

/// <summary>Handles <see cref="AutomationSettingsSaveCommand"/>.</summary>
internal sealed class AutomationSettingsSaveCommandHandler(AutomationService automation)
    : ICommandHandler<AutomationSettingsSaveCommand, AppResult<AutomationSettingsModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<AutomationSettingsModel>> HandleAsync(
        AutomationSettingsSaveCommand request,
        CancellationToken ct)
    {
        var data = await automation.SaveAsync(request.Settings, ct);

        return AppResult<AutomationSettingsModel>.Ok(data);
    }
}
