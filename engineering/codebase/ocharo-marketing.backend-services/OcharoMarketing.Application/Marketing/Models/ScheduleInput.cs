namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Schedule data for the local marketing workflow.</summary>
public sealed record ScheduleInput(int ExpectedRevision, string Channel, DateTimeOffset ScheduledAt);
