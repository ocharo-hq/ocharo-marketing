namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Metric data for the local marketing workflow.</summary>
public sealed record MetricModel(string Channel, string Name, decimal? Value, string Status, DateTimeOffset? ObservedAt, string Explanation, string? ScheduleId = null, string? ReceiptId = null, string? Scope = null, string? Aggregation = null);
