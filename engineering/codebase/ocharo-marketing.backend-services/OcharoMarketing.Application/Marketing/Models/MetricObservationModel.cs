namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record MetricObservationModel(string Id, string ScheduleId, string ConnectionId, string Provider, string ReceiptId,
    string Name, decimal? Value, string Availability, DateTimeOffset? ObservedAt, string? Reason,
    string Scope = "post", string Aggregation = "cumulative");
