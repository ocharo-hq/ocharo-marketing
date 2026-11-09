using OcharoMarketing.Application.Deliveries.Commands;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.CommandHandlers;

/// <summary>Handles <see cref="MetricsRefreshCommand"/>.</summary>
internal sealed class MetricsRefreshCommandHandler(MetricObservationService metrics)
    : ICommandHandler<MetricsRefreshCommand, AppResult<MetricCacheModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<MetricCacheModel>> HandleAsync(MetricsRefreshCommand request, CancellationToken ct)
    {
        var data = await metrics.RefreshAsync(request.ScheduleId, ct);

        return AppResult<MetricCacheModel>.Ok(data);
    }
}
