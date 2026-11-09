using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Deliveries.Queries;
using OcharoMarketing.Application.Deliveries.Repositories;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.QueryHandlers;

/// <summary>Handles <see cref="MetricsGetQuery"/>.</summary>
internal sealed class MetricsGetQueryHandler(IDeliveryRepository delivery)
    : IQueryHandler<MetricsGetQuery, AppResult<MetricCacheModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<MetricCacheModel>> HandleAsync(MetricsGetQuery request, CancellationToken ct)
    {
        var data = await delivery.GetMetricsAsync(request.ScheduleId, ct);

        return AppResult<MetricCacheModel>.Ok(data);
    }
}
