using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Infrastructure.Automation;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Channel strategy, recorded engagement and local monitoring; never contacts a social provider.</summary>
public sealed class OperationsService(IOperationsRepository records, IMarketingRepository marketing, IDeliveryRepository delivery,
    AutomationService automation, AutomationRunner runner, DeliveryPolicy policy, WorkerHealthService workers, TimeProvider time)
{
    public static readonly string[] Channels = ["instagram", "telegram", "x", "whatsapp", "blog", "atlas", "wear", "catalogue"];

    public async Task<ChannelStrategyModel[]> ChannelsAsync(CancellationToken ct)
    {
        var values = (await records.ListAsync("channel:", ct)).Select(MarketingJson.Read<ChannelStrategyModel>).ToDictionary(x => x.Channel);
        return Channels.Select(channel => values.GetValueOrDefault(channel) ?? Default(channel)).ToArray();
    }

    public async Task<ChannelStrategyModel> SaveChannelAsync(string channel, ChannelStrategyModel input, CancellationToken ct)
    {
        Require(Channels.Contains(channel) && input.Channel == channel, "Choose a supported channel.");
        Require(input.Revision >= 0 && input.WeeklyTarget is >= 0 and <= 100 && input.ReviewEveryDays is >= 1 and <= 90,
            "Weekly target must be 0–100 and review interval 1–90 days.");
        Require(input.Audience is { Length: <= 500 } && input.Purpose is { Length: <= 500 } && input.Formats is { Length: <= 500 }
            && input.Pillars is { Length: <= 500 } && input.Owner is { Length: <= 120 } && input.ProfileUrl is { Length: <= 500 }, "Keep channel fields within their displayed limits.");
        if (!string.IsNullOrWhiteSpace(input.ProfileUrl)) ValidateUrl(input.ProfileUrl);
        var key = "channel:" + channel;
        var raw = await records.ReadAsync(key, ct);
        var current = raw is null ? Default(channel) : MarketingJson.Read<ChannelStrategyModel>(raw);
        if (current.Revision != input.Revision) throw new MarketingException("This channel changed. Reload its saved version; your edits remain in the form.", 409);
        var saved = input with { Revision = current.Revision + 1, Audience = input.Audience.Trim(), Purpose = input.Purpose.Trim(),
            Formats = input.Formats.Trim(), Pillars = input.Pillars.Trim(), Owner = input.Owner.Trim(), ProfileUrl = input.ProfileUrl.Trim(), UpdatedAt = time.GetUtcNow() };
        if (!await records.CompareExchangeAsync(key, raw, MarketingJson.Write(saved), ct))
            throw new MarketingException("This channel changed. Reload before saving.", 409);
        return saved;
    }

    public async Task<EngagementObservationModel[]> ObservationsAsync(CancellationToken ct) =>
        (await records.ListAsync("engagement:", ct)).Select(MarketingJson.Read<EngagementObservationModel>)
            .OrderByDescending(x => x.Observation.ObservedAt).ThenByDescending(x => x.RecordedAt).ToArray();

    public async Task<EngagementObservationModel> RecordAsync(EngagementInput input, string actor, CancellationToken ct)
    {
        Require(Guid.TryParseExact(input.Id, "D", out _), "Provide the observation's unique identifier.");
        Require(Channels.Contains(input.Channel), "Choose a supported channel.");
        Require(input.Title is { Length: > 0 and <= 160 } && !string.IsNullOrWhiteSpace(input.Title), "A post title is required (160 characters maximum).");
        Require(input.EvidenceNote is { Length: > 0 and <= 1000 } && !string.IsNullOrWhiteSpace(input.EvidenceNote), "Describe the source of these manual counts (1000 characters maximum).");
        ValidateUrl(input.PostUrl);
        var now = time.GetUtcNow();
        Require(input.PublishedAt.Year >= 2000 && input.ObservedAt >= input.PublishedAt && input.ObservedAt <= now.AddMinutes(2),
            "Observation time must follow publication and cannot be in the future.");
        long?[] values = [input.Views, input.Reach, input.Likes, input.Comments, input.Shares, input.Saves, input.Clicks, input.Enquiries, input.Orders];
        Require(values.Any(x => x.HasValue) && values.All(x => x is null or >= 0 and <= 1_000_000_000),
            "Enter at least one count from 0 to 1,000,000,000. Leave unavailable counts blank.");
        if (!string.IsNullOrEmpty(input.ScheduleId))
        {
            var schedule = await delivery.FindScheduleAsync(input.ScheduleId, ct);
            Require(schedule is not null && schedule.Channel == input.Channel, "The selected schedule must belong to this channel.");
        }
        var normalized = input with { Id = Guid.Parse(input.Id).ToString("D"), Title = input.Title.Trim(), PostUrl = input.PostUrl.Trim(), EvidenceNote = input.EvidenceNote.Trim(),
            ScheduleId = string.IsNullOrWhiteSpace(input.ScheduleId) ? null : input.ScheduleId, PublishedAt = input.PublishedAt.ToUniversalTime(), ObservedAt = input.ObservedAt.ToUniversalTime() };
        var key = "engagement:" + normalized.Id;
        var saved = new EngagementObservationModel(normalized, now, actor);
        if (await records.CompareExchangeAsync(key, null, MarketingJson.Write(saved), ct)) return saved;
        var previous = MarketingJson.Read<EngagementObservationModel>((await records.ReadAsync(key, ct))!);
        if (previous.Observation != normalized) throw new MarketingException("That observation identifier already records different values. Create a new observation.", 409);
        return previous;
    }

    public async Task<OperationsOverviewModel> OverviewAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var monday = local.Date.AddDays(-((int)local.DayOfWeek + 6) % 7);
        var start = new DateTimeOffset(monday, zone.GetUtcOffset(monday)).ToUniversalTime();
        var end = start.AddDays(7);
        var strategies = await ChannelsAsync(ct);
        var observations = await ObservationsAsync(ct);
        var latest = observations.GroupBy(x => (x.Observation.Channel, x.Observation.PostUrl)).Select(group => group.First()).ToArray();
        var schedules = await marketing.GetSchedulesAsync(ct);
        var operations = await delivery.GetOperationsAsync(ct);
        var drafts = await marketing.GetDraftsAsync(ct);
        var settings = await automation.GetAsync(ct);
        var cached = await delivery.GetCachedObservationsAsync(ct);
        var summaries = strategies.Select(strategy =>
        {
            var channel = strategy.Channel;
            var channelSchedules = schedules.Where(x => x.Channel == channel).ToArray();
            var confirmed = operations.Where(x => x.Provider == channel && x.Status == "published" && x.Receipt?.Stage == "published").ToArray();
            var recent = confirmed.Count(x => x.UpdatedAt >= start && x.UpdatedAt < end);
            var manual = latest.Where(x => x.Observation.Channel == channel).ToArray();
            var manualCount = manual.Count(x => x.Observation.PublishedAt >= start && x.Observation.PublishedAt < end
                && !confirmed.Any(op => op.ScheduleId == x.Observation.ScheduleId));
            var providerDate = cached.Where(x => x.Provider == channel).Select(x => x.ObservedAt).Max();
            var manualDate = manual.Select(x => (DateTimeOffset?)x.Observation.ObservedAt).Max();
            var observed = new[] { providerDate, manualDate }.Max();
            var slots = settings.Slots.Where(x => x.Channel == channel && x.Enabled)
                .SelectMany(x => x.Days.Select(day => (day, x.Time))).Distinct().Count();
            var warnings = new List<string>();
            if (strategy.Active && strategy.WeeklyTarget > 0 && slots < strategy.WeeklyTarget && channel is "instagram" or "telegram" or "x")
                warnings.Add("Weekly slot count is below the channel target.");
            if (strategy.Active && (observed is null || now - observed > TimeSpan.FromDays(strategy.ReviewEveryDays)))
                warnings.Add("Engagement review is due; missing observations are not zero results.");
            if (strategy.Active && string.IsNullOrWhiteSpace(strategy.Owner)) warnings.Add("Choose who owns this channel.");
            if (channelSchedules.Any(x => x.Status == "planned" && x.ScheduledAt < now)) warnings.Add("A planned post is overdue and has not been authorized for delivery.");
            return new ChannelSummaryModel(strategy, slots, channelSchedules.Count(x => x.ScheduledAt >= now && x.ScheduledAt < now.AddDays(7) && x.Status is "planned" or "queued"),
                recent, manualCount, channelSchedules.Count(x => x.Status == "planned" && x.ScheduledAt < now), observed, warnings.ToArray());
        }).ToArray();
        return new(now, start, end, "Asia/Tashkent", summaries, workers.Read(), await runner.StatusAsync(ct), policy.Settings,
            drafts.Count(x => x.ApprovedRevision == x.Revision && !schedules.Any(s => s.DraftId == x.Id && s.Status != "cancelled")),
            operations.Count(x => x.Status == "queued"), operations.Count(x => x.Status == "running"), operations.Count(x => x.Status == "failed"),
            operations.Count(x => x.Status == "unknown"), operations.Count(x => x.Status is "held" or "revoked"),
            schedules.Where(x => x.Status == "planned" && x.ScheduledAt < now).OrderBy(x => x.ScheduledAt).Take(100).ToArray(),
            operations.Where(x => x.Status is "failed" or "unknown" or "held" or "revoked" || x.Status == "queued" && x.DueAt < now.AddMinutes(-5))
                .OrderByDescending(x => x.UpdatedAt).Take(100).ToArray(), observations.Take(200).ToArray(), cached.ToArray(), observations.Length);
    }

    private static ChannelStrategyModel Default(string channel) => new(channel, 0, false, "", "", "", "", "", "", 0, 7, null);
    private static void Require(bool valid, string message) { if (!valid) throw new MarketingException(message, 400); }
    private static void ValidateUrl(string? url) => Require(url is { Length: > 0 and <= 500 } && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && uri.UserInfo.Length == 0 && !string.IsNullOrWhiteSpace(uri.Host), "Use an HTTPS page URL without credentials (500 characters maximum).");
}
