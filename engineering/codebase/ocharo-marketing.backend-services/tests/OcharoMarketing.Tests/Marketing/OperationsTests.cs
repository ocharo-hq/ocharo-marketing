using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Domain.Marketing.Entities;
using OcharoMarketing.Infrastructure.Automation;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Persistence;
using OcharoMarketing.Persistence.Marketing.Repositories;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Real SQLite evidence for concurrent strategy editors, manual observations and honest monitor counts.</summary>
public sealed class OperationsTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly Clock clock = new();
    private readonly string blobs = Path.Combine(Path.GetTempPath(), "ocharo-operations-tests", Guid.NewGuid().ToString("N"));
    public Task InitializeAsync() => database.InitializeAsync();
    public async Task DisposeAsync() { await database.DisposeAsync(); if (Directory.Exists(blobs)) Directory.Delete(blobs, true); }

    [Fact]
    public async Task Strategy_defaults_do_not_activate_channels_and_stale_save_preserves_winner()
    {
        await using var first = database.NewContext(); await using var second = database.NewContext();
        var initial = await Service(first).ChannelsAsync(default);
        Assert.Equal(8, initial.Length); Assert.All(initial, x => { Assert.False(x.Active); Assert.Equal(0, x.WeeklyTarget); });
        var input = initial.Single(x => x.Channel == "instagram") with { Active = true, Audience = "International buyers", Purpose = "Recognition", Pillars = "Fabric stories", Owner = "Designer", WeeklyTarget = 3 };
        var saved = await Service(first).SaveChannelAsync("instagram", input, default);
        Assert.Equal(1, saved.Revision);
        var error = await Assert.ThrowsAsync<MarketingException>(() => Service(second).SaveChannelAsync("instagram", input with { Purpose = "Lost edit" }, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal("Recognition", (await Service(second).ChannelsAsync(default)).Single(x => x.Channel == "instagram").Purpose);
        Assert.Empty(await new DeliveryRepository(second).GetOperationsAsync(default));
    }

    [Fact]
    public async Task Compare_exchange_serializes_two_initial_creators_without_overwrite()
    {
        await using var first = database.NewContext(); await using var second = database.NewContext();
        Assert.True(await new OperationsRepository(first).CompareExchangeAsync("channel:race", null, "first", default));
        Assert.False(await new OperationsRepository(second).CompareExchangeAsync("channel:race", null, "second", default));
        Assert.False(await new OperationsRepository(second).CompareExchangeAsync("channel:race", "stale", "second", default));
        Assert.Equal("first", await new OperationsRepository(first).ReadAsync("channel:race", default));
    }

    [Fact]
    public async Task Manual_snapshots_are_durable_idempotent_and_never_change_delivery_status()
    {
        await using var first = database.NewContext();
        var input = Observation();
        var saved = await Service(first).RecordAsync(input, "reviewer", default);
        Assert.Equal("manual", saved.Source); Assert.Equal(10m, saved.EngagementRate);
        await using var reopened = database.NewContext();
        var retry = await Service(reopened).RecordAsync(input with { Id = input.Id.ToUpperInvariant() }, "another-reviewer", default);
        Assert.Equal(saved, retry); Assert.Single(await Service(reopened).ObservationsAsync(default));
        Assert.Empty(await new DeliveryRepository(reopened).GetOperationsAsync(default));
        Assert.Empty(await new MarketingRepository(reopened).GetSchedulesAsync(default));
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => Service(reopened).RecordAsync(input with { Reach = 50 }, "reviewer", default))).StatusCode);
    }

    [Fact]
    public async Task Missing_counts_keep_rates_unknown_while_a_real_zero_is_saved()
    {
        await using var db = database.NewContext();
        var saved = await Service(db).RecordAsync(Observation() with { Likes = null, Views = 0 }, "reviewer", default);
        Assert.Null(saved.EngagementRate); Assert.Equal(0, saved.Observation.Views); Assert.Null(saved.Observation.Likes);
    }

    [Theory]
    [InlineData("negative")][InlineData("future")][InlineData("before")][InlineData("javascript")][InlineData("credentials")][InlineData("empty")][InlineData("foreign-schedule")]
    public async Task Invalid_or_unbound_observations_do_not_write(string invalid)
    {
        await using var db = database.NewContext();
        var input = Observation();
        input = invalid switch {
            "negative" => input with { Likes = -1 }, "future" => input with { ObservedAt = clock.Now.AddHours(1) },
            "before" => input with { ObservedAt = input.PublishedAt.AddMinutes(-1) }, "javascript" => input with { PostUrl = "javascript:alert(1)" },
            "credentials" => input with { PostUrl = "https://user:secret@example.test/post" },
            "empty" => input with { Views = null, Reach = null, Likes = null, Comments = null, Shares = null, Saves = null },
            _ => input with { ScheduleId = "foreign" } };
        Assert.Equal(400, (await Assert.ThrowsAsync<MarketingException>(() => Service(db).RecordAsync(input, "reviewer", default))).StatusCode);
        Assert.Empty(await Service(db).ObservationsAsync(default));
    }

    [Fact]
    public async Task Monitor_deduplicates_manual_snapshots_uses_tashkent_week_and_keeps_plans_separate()
    {
        await using var db = database.NewContext(); var service = Service(db);
        var input = Observation(); await service.RecordAsync(input, "reviewer", default);
        await service.RecordAsync(input with { Id = Guid.NewGuid().ToString(), ObservedAt = clock.Now, Reach = 200 }, "reviewer", default);
        var repository = new MarketingRepository(db);
        var draft = new DraftModel("draft", "Planned only", "product", "t02-bone", "en", "Caption", ["instagram"], [], 1, 1, "approved", clock.Now, clock.Now);
        await repository.SaveDraftAsync(draft, null, default);
        await repository.ApproveDraftAsync(draft.Id, 1, default);
        await repository.ScheduleAsync(new("plan", "draft", 1, "instagram", clock.Now.AddHours(-2), "planned", null, null, draft.Title), default);
        var overview = await service.OverviewAsync(default);
        var instagram = overview.Channels.Single(x => x.Strategy.Channel == "instagram");
        Assert.Equal(1, instagram.ManualPostsThisWeek); Assert.Equal(0, instagram.ConfirmedReceiptsThisWeek);
        Assert.Equal(1, instagram.DuePlans); Assert.Equal(2, overview.Observations.Length);
        Assert.Equal(new DateTimeOffset(2026, 9, 27, 19, 0, 0, TimeSpan.Zero), overview.WeekStartsAt);
        Assert.All(overview.Workers, x => Assert.Equal("unobserved", x.Status));
        Assert.False(overview.Delivery.PublishingEnabled); Assert.False(overview.Delivery.MetricReadsEnabled);
    }

    [Fact]
    public async Task Cross_week_receipt_reconciliation_counts_confirmation_not_publication_date()
    {
        await using var db = database.NewContext(); var service = Service(db); var lastWeek = clock.Now.AddDays(-8);
        var draft = new DraftModel("old-draft", "Old post", "p", "t02-bone", "en", "Caption", ["x"], [], 1, 1, "approved", lastWeek, lastWeek);
        var snapshot = new PublicationSnapshotModel(draft, new("x", "x", 1, "account", "account", "Account", ["public-metrics"], lastWeek), []);
        var operation = new PublicationOperationModel("old-op", "old-schedule", "x", "x", 1, "published", "complete", lastWeek,
            lastWeek, lastWeek, clock.Now, "Receipt reconciled today", new("x", "published", "old-post"), snapshot);
        db.PublicationOperations.Add(new PublicationOperationRecord { Id=operation.Id, ScheduleId=operation.ScheduleId, Provider="x", ConnectionId="x", Status="published", Json=MarketingJson.Write(operation) });
        await db.SaveChangesAsync();
        var overview = await service.OverviewAsync(default);
        Assert.Equal(1, overview.Channels.Single(x => x.Strategy.Channel == "x").ConfirmedReceiptsThisWeek);
        Assert.Equal(0, overview.Channels.Single(x => x.Strategy.Channel == "x").ManualPostsThisWeek);
        // DTO and UI explicitly call this confirmations; no derived publication date exists for this receipt.
        Assert.DoesNotContain("publishedThisWeek", MarketingJson.Write(overview));
    }

    [Fact]
    public async Task Manual_only_channels_do_not_receive_impossible_slot_deficit_warnings()
    {
        await using var db = database.NewContext(); var service = Service(db);
        var whatsapp = (await service.ChannelsAsync(default)).Single(x => x.Channel == "whatsapp");
        await service.SaveChannelAsync("whatsapp", whatsapp with { Active = true, WeeklyTarget = 2, Owner = "Operator" }, default);
        var result = (await service.OverviewAsync(default)).Channels.Single(x => x.Strategy.Channel == "whatsapp");
        Assert.DoesNotContain(result.Warnings, x => x.Contains("slot count"));
    }

    [Fact]
    public void Worker_health_is_unobserved_until_real_checks_and_turns_stale_without_new_checks()
    {
        var health = new WorkerHealthService(clock);
        Assert.All(health.Read(), x => Assert.Equal("unobserved", x.Status));
        health.Started("automation"); health.Completed("automation");
        Assert.Equal("healthy", health.Read().Single(x => x.Worker == "automation").Status);
        clock.Now = clock.Now.AddMinutes(12);
        Assert.Equal("stale", health.Read().Single(x => x.Worker == "automation").Status);
        health.Failed("automation"); Assert.Equal("failed", health.Read().Single(x => x.Worker == "automation").Status);
        health.Completed("automation"); Assert.Null(health.Read().Single(x => x.Worker == "automation").LastError);
    }

    private EngagementInput Observation() => new(Guid.NewGuid().ToString(), "instagram", "https://example.test/posts/one", "Synthetic post", null,
        clock.Now.AddHours(-2), clock.Now.AddHours(-1), 500, 100, 4, 2, 1, 3, null, null, null, "Synthetic manual Insights evidence");

    private OperationsService Service(OcharoMarketingDbContext db)
    {
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobs), Options.Create(new MarketingOptions()));
        var marketing = new MarketingService(repository, assets, new Cards());
        var automation = new AutomationService(repository, marketing, new Cards(), clock);
        var catalogue = new CatalogueSyncService(repository, assets, new HttpClient(new NeverHttp()), Options.Create(new MarketingOptions()));
        return new(new OperationsRepository(db), repository, new DeliveryRepository(db), automation,
            new AutomationRunner(catalogue, automation, repository, clock), new DeliveryPolicy(new ConfigurationBuilder().Build(), new TelegramProviderOptions(), new XProviderOptions()),
            new WorkerHealthService(clock), clock);
    }

    private sealed class Clock : TimeProvider { public DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class NeverHttp : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => throw new InvalidOperationException("Operations may not contact providers."); }
    private sealed class Cards : ICardRenderingService {
        public IReadOnlyList<CardPreset> GetPresets() => [new("t02-bone", "Bone", "T02", "bone", "", false, 1080, 1350)];
        public CardPreview Compose(CardCompositionInput input) => throw new NotSupportedException();
        public Task<byte[]> RenderPngAsync(string html, CancellationToken ct) => throw new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken ct) => throw new NotSupportedException();
    }
}
