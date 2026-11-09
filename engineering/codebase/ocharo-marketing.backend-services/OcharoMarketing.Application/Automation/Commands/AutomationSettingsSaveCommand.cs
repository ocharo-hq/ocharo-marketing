using OcharoMarketing.Application.Automation.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Commands;

/// <summary>Represents a command that saves the posting rhythm and draft rules.</summary>
public sealed record AutomationSettingsSaveCommand : ICommand<AppResult<AutomationSettingsModel>>
{
    /// <summary>Gets the settings to save.</summary>
    public required AutomationSettingsModel Settings { get; init; }
}
