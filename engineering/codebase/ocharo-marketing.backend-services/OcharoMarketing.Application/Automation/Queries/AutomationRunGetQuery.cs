using OcharoMarketing.Application.Automation.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Queries;

/// <summary>Represents a query for the last unattended automation passes.</summary>
public sealed record AutomationRunGetQuery : IQuery<AppResult<AutomationRunModel>>
{
}
