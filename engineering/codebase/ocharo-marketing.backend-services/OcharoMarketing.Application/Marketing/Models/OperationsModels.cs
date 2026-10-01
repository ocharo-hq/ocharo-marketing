namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Operator-owned strategy; an inactive empty default makes no business commitment.</summary>
public sealed record ChannelStrategyModel(string Channel, int Revision, bool Active, string Audience, string Purpose,
    string Formats, string Pillars, string Owner, string ProfileUrl, int WeeklyTarget, int ReviewEveryDays, DateTimeOffset? UpdatedAt);

/// <summary>One immutable manual observation of a published post. Missing counts are unknown, never zero.</summary>
public sealed record EngagementInput(string Id, string Channel, string PostUrl, string Title, string? ScheduleId,
    DateTimeOffset PublishedAt, DateTimeOffset ObservedAt, long? Views, long? Reach, long? Likes, long? Comments,
    long? Shares, long? Saves, long? Clicks, long? Enquiries, long? Orders, string EvidenceNote);

/// <summary>Records who entered a manual observation without pretending the provider verified it.</summary>
public sealed record EngagementObservationModel(EngagementInput Observation, DateTimeOffset RecordedAt, string RecordedBy,
    string Source = "manual")
{
    public decimal? EngagementRate => Observation.Reach is > 0 && Observation.Likes.HasValue && Observation.Comments.HasValue
        && Observation.Shares.HasValue && Observation.Saves.HasValue
        ? decimal.Round((Observation.Likes.Value + (decimal)Observation.Comments.Value + Observation.Shares.Value + Observation.Saves.Value)
            / Observation.Reach.Value * 100, 2) : null;
}

/// <summary>Reports this host's real background worker lifecycle; restarted hosts begin unobserved.</summary>
public sealed record WorkerHealthModel(string Worker, string Status, DateTimeOffset? StartedAt, DateTimeOffset? CheckedAt,
    DateTimeOffset? LastSuccessAt, string? LastError, int PollSeconds);

/// <summary>Combines strategy with evidence without treating planned or accepted posts as published.</summary>
public sealed record ChannelSummaryModel(ChannelStrategyModel Strategy, int WeeklySlots, int UpcomingPlans,
    int ConfirmedReceiptsThisWeek, int ManualPostsThisWeek, int DuePlans, DateTimeOffset? LastObservationAt, string[] Warnings);

/// <summary>Read-only monitoring over local receipts, worker health and manual evidence.</summary>
public sealed record OperationsOverviewModel(DateTimeOffset ObservedAt, DateTimeOffset WeekStartsAt, DateTimeOffset WeekEndsAt,
    string TimeZone, ChannelSummaryModel[] Channels, WorkerHealthModel[] Workers, AutomationRunModel Automation,
    DeliverySettingsModel Delivery, int ApprovedUnplanned, int Queued, int Running, int Failed, int Unknown, int Held,
    ScheduleModel[] AttentionPlans, PublicationOperationModel[] AttentionDeliveries,
    EngagementObservationModel[] Observations, MetricObservationModel[] ProviderObservations, int ObservationCount);
