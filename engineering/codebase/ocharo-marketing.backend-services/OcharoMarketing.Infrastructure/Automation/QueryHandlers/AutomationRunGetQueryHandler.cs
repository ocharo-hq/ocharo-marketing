using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Application.Automation.Queries;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.QueryHandlers;

/// <summary>Handles <see cref="AutomationRunGetQuery"/>.</summary>
internal sealed class AutomationRunGetQueryHandler(AutomationRunner runner)
    : IQueryHandler<AutomationRunGetQuery, AppResult<AutomationRunModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<AutomationRunModel>> HandleAsync(
        AutomationRunGetQuery request,
        CancellationToken ct)
    {
        var data = await runner.StatusAsync(ct);

        return AppResult<AutomationRunModel>.Ok(data);
    }
}
