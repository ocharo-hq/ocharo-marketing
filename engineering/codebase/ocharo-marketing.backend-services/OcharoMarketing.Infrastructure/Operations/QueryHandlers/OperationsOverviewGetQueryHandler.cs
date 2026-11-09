using OcharoMarketing.Application.Operations.Models;
using OcharoMarketing.Application.Operations.Queries;
using OcharoMarketing.Infrastructure.Operations.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Operations.QueryHandlers;

/// <summary>Handles <see cref="OperationsOverviewGetQuery"/>.</summary>
internal sealed class OperationsOverviewGetQueryHandler(OperationsService operations)
    : IQueryHandler<OperationsOverviewGetQuery, AppResult<OperationsOverviewModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<OperationsOverviewModel>> HandleAsync(
        OperationsOverviewGetQuery request,
        CancellationToken ct)
    {
        var data = await operations.OverviewAsync(ct);

        return AppResult<OperationsOverviewModel>.Ok(data);
    }
}
