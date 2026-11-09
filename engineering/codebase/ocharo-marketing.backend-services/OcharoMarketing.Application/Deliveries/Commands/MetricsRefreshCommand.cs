using OcharoMarketing.Application.Deliveries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Commands;

/// <summary>Represents a command that reads a plan's metrics from its provider.</summary>
public sealed record MetricsRefreshCommand : ICommand<AppResult<MetricCacheModel>>
{
    /// <summary>Gets the plan.</summary>
    public required string ScheduleId { get; init; }
}
