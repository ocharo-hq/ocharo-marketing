using OcharoMarketing.Application.Deliveries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Deliveries.Queries;

/// <summary>Represents a query for a plan's cached provider metrics.</summary>
public sealed record MetricsGetQuery : IQuery<AppResult<MetricCacheModel>>
{
    /// <summary>Gets the plan.</summary>
    public required string ScheduleId { get; init; }
}
