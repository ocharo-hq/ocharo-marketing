namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Schedule data for the local marketing workflow.</summary>
public sealed record ScheduleModel(string Id, string DraftId, int DraftRevision, string Channel, DateTimeOffset ScheduledAt, string Status, string? Message, string? ExternalId, string? DraftTitle = null);
