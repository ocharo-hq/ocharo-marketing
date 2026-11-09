using OcharoMarketing.Application.Automation.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Queries;

/// <summary>Represents a query for the posting rhythm and draft rules.</summary>
public sealed record AutomationSettingsGetQuery : IQuery<AppResult<AutomationSettingsModel>>
{
}
