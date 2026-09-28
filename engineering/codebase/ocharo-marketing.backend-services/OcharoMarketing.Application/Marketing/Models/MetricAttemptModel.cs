namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record MetricAttemptModel(DateTimeOffset AttemptedAt, string Status, string Message);
