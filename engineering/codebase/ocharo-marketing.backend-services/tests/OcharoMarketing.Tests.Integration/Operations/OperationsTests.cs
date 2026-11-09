using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using AwesomeAssertions;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Operations.Models;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Domain.Deliveries.Entities;
using OcharoMarketing.Infrastructure.Assets.Services;
using OcharoMarketing.Infrastructure.Core;
using OcharoMarketing.Infrastructure.Operations.Services;
using OcharoMarketing.Infrastructure.Posts.Services;
using OcharoMarketing.Persistence.Core.Repositories;
using OcharoMarketing.Persistence.Deliveries.Repositories;
using OcharoMarketing.Persistence.Operations.Repositories;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using OcharoMarketing.Infrastructure.Automation;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Persistence;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Operations;

/// <summary>Real SQLite evidence for concurrent strategy editors, manual observations and honest monitor
/// counts.</summary>
public sealed class OperationsTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly string blobs = Path.Combine(
        Path.GetTempPath(),
        "ocharo-operations-tests",
        Guid.NewGuid().ToString("N"));
    public Task InitializeAsync() => database.InitializeAsync();
    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobs)) Directory.Delete(blobs, true);
    }

    [Fact]
    public async Task SaveChannel_ShouldKeepDefaultsInactiveAndTheWinner_WhenASaveIsStale()
    {
        await using var first = database.NewContext(); await using var second = database.NewContext();
        var initial = await Service(first).ChannelsAsync(default);
        initial.Length.Should().Be(8);
        initial.Should().AllSatisfy(x => { x.Active.Should().BeFalse(); x.WeeklyTarget.Should().Be(0); });
        var input = initial.Single(
            x =>
                x.Channel == "instagram") with
                {
                    Active = true,
                    Audience = "International buyers",
                    Purpose = "Recognition",
                    Pillars = "Fabric stories",
                    Owner = "Designer",
                    WeeklyTarget = 3
                };
        var saved = await Service(first).SaveChannelAsync("instagram", input, default);
        saved.Revision.Should().Be(1);
        var error = (
            await FluentActions
                .Awaiting(
                    () => Service(second).SaveChannelAsync("instagram", input with { Purpose = "Lost edit" }, default))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which;
        ErrorStatus.Of(error).Should().Be(409);
        ((await Service(second).ChannelsAsync(default)).Single(x => x.Channel == "instagram").Purpose)
            .Should()
            .Be("Recognition");
        (await new DeliveryRepository(second).GetOperationsAsync(default)).Should().BeEmpty();
    }

    [Fact]
    public async Task CompareExchange_ShouldSerializeTwoInitialCreatorsWithoutOverwrite()
    {
        await using var first = database.NewContext(); await using var second = database.NewContext();
        // Operations documents are JSON, as jsonb holds them.
        const string First = """{"by":"first"}""", Second = """{"by":"second"}""", Stale = """{"by":"stale"}""";
        const string Key = "channel:race";
        (await new OperationsRepository(first).CompareExchangeAsync(Key, null, First, default)).Should().BeTrue();
        (await new OperationsRepository(second).CompareExchangeAsync(Key, null, Second, default)).Should().BeFalse();
        (await new OperationsRepository(second).CompareExchangeAsync(Key, Stale, Second, default)).Should().BeFalse();
        var stored = await new OperationsRepository(first).ReadAsync("channel:race", default);
        (JsonDocument.Parse(stored!).RootElement.GetProperty("by").GetString()).Should().Be("first");
    }

    [Fact]
    public async Task Record_ShouldStoreDurableIdempotentSnapshotsWithoutChangingDeliveryStatus()
    {
        await using var first = database.NewContext();
        var input = Observation();
        var saved = await Service(first).RecordAsync(input, "reviewer", default);
        saved.Source.Should().Be("manual"); saved.EngagementRate.Should().Be(10m);
        await using var reopened = database.NewContext();
        var retry = await Service(
            reopened).RecordAsync(input with { Id = input.Id.ToUpperInvariant() }, "another-reviewer", default);
        retry.Should().Be(saved); (await Service(reopened).ObservationsAsync(default)).Should().ContainSingle();
        (await new DeliveryRepository(reopened).GetOperationsAsync(default)).Should().BeEmpty();
        (await new MarketingRepository(reopened).GetSchedulesAsync(default)).Should().BeEmpty();
        (
            ErrorStatus
                .Of(
                    (
                        await FluentActions
                            .Awaiting(
                                () => Service(reopened).RecordAsync(input with { Reach = 50 }, "reviewer", default))
                            .Should()
                            .ThrowExactlyAsync<AppException>()).Which))
                .Should()
                .Be(409);
    }

    [Fact]
    public async Task Record_ShouldKeepRatesUnknownAndSaveARealZero_WhenCountsAreMissing()
    {
        await using var db = database.NewContext();
        var saved = await Service(db).RecordAsync(Observation() with { Likes = null, Views = 0 }, "reviewer", default);
        saved.EngagementRate.Should().BeNull();
        saved.Observation.Views.Should().Be(0);
        saved.Observation.Likes.Should().BeNull();
    }

    [Theory]
    [
        InlineData(
            "negative")][
                InlineData(
                    "future")][
                        InlineData(
                            "before")][
                                InlineData(
                                    "javascript")][
                                        InlineData("credentials")][InlineData("empty")][InlineData("foreign-schedule")]
    public async Task Record_ShouldWriteNothing_WhenTheObservationIsInvalidOrUnbound(string invalid)
    {
        await using var db = database.NewContext();
        var input = Observation();
        input = invalid switch {
            "negative" => input with { Likes = -1 },
            "future" => input with { ObservedAt = clock.GetUtcNow().AddHours(1) },
            "before" => input with { ObservedAt = input.PublishedAt.AddMinutes(-1) },
            "javascript" => input with { PostUrl = "javascript:alert(1)" },
            "credentials" => input with { PostUrl = "https://user:secret@example.test/post" },
            "empty" =>
                input with { Views = null, Reach = null, Likes = null, Comments = null, Shares = null, Saves = null },
            _ => input with { ScheduleId = "foreign" } };
        (
            ErrorStatus
                .Of(
                    (
                        await FluentActions
                            .Awaiting(() => Service(db).RecordAsync(input, "reviewer", default))
                            .Should()
                            .ThrowExactlyAsync<AppException>()).Which))
                .Should()
                .Be(400);
        (await Service(db).ObservationsAsync(default)).Should().BeEmpty();
    }

    [Fact]
    public async Task Overview_ShouldDeduplicateSnapshotsUseTheTashkentWeekAndKeepPlansApart()
    {
        await using var db = database.NewContext(); var service = Service(db);
        var input = Observation(); await service.RecordAsync(input, "reviewer", default);
        await service.RecordAsync(
            input with { Id = Guid.NewGuid().ToString(), ObservedAt = clock.GetUtcNow(), Reach = 200 },
            "reviewer", default);
        var repository = new MarketingRepository(db);
        var draft = new DraftModel("draft", "Planned only", "product", "t02-bone", "en", "Caption", ["instagram"], [],
            1, 1, "approved", clock.GetUtcNow(), clock.GetUtcNow());
        await repository.SaveDraftAsync(draft, null, default);
        await repository.ApproveDraftAsync(draft.Id, 1, default);
        await repository.ScheduleAsync(
            new("plan", "draft", 1, "instagram", clock.GetUtcNow().AddHours(-2), "planned", null, null, draft.Title),
            default);
        var overview = await service.OverviewAsync(default);
        var instagram = overview.Channels.Single(x => x.Strategy.Channel == "instagram");
        instagram.ManualPostsThisWeek.Should().Be(1); instagram.ConfirmedReceiptsThisWeek.Should().Be(0);
        instagram.DuePlans.Should().Be(1); overview.Observations.Length.Should().Be(2);
        overview.WeekStartsAt.Should().Be(new DateTimeOffset(2026, 9, 27, 19, 0, 0, TimeSpan.Zero));
        overview.Workers.Should().AllSatisfy(x => x.Status.Should().Be("unobserved"));
        overview.Delivery.PublishingEnabled.Should().BeFalse(); overview.Delivery.MetricReadsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Overview_ShouldCountTheConfirmationDate_WhenAReceiptIsReconciledAcrossWeeks()
    {
        await using var db = database.NewContext(); var service = Service(db);
        var lastWeek = clock.GetUtcNow().AddDays(-8);
        var draft = new DraftModel(
            "old-draft",
            "Old post",
            "p",
            "t02-bone",
            "en",
            "Caption",
            ["x"],
            [],
            1,
            1,
            "approved",
            lastWeek,
            lastWeek);
        var snapshot = new PublicationSnapshotModel(
            draft,
            new("x", "x", 1, "account", "account", "Account", ["public-metrics"], lastWeek),
            []);
        var operation = new PublicationOperationModel(
            "old-op",
            "old-schedule",
            "x",
            "x",
            1,
            "published",
            "complete",
            lastWeek,
            lastWeek, lastWeek, clock.GetUtcNow(), "Receipt reconciled today", new("x", "published", "old-post"),
            snapshot);
        db.PublicationOperations.Add(
            new PublicationOperationRecord
            {
                Id=operation.Id,
                ScheduleId=operation.ScheduleId,
                Provider="x",
                ConnectionId="x",
                Status="published",
                Json=MarketingJson.Write(operation)
            });
        await db.SaveChangesAsync();
        var overview = await service.OverviewAsync(default);
        (overview.Channels.Single(x => x.Strategy.Channel == "x").ConfirmedReceiptsThisWeek).Should().Be(1);
        (overview.Channels.Single(x => x.Strategy.Channel == "x").ManualPostsThisWeek).Should().Be(0);
        // DTO and UI explicitly call this confirmations; no derived publication date exists for this receipt.
        MarketingJson.Write(overview).Should().NotContain("publishedThisWeek");
    }

    [Fact]
    public async Task Overview_ShouldWarnOfNoSlotDeficit_WhenAChannelIsManualOnly()
    {
        await using var db = database.NewContext(); var service = Service(db);
        var whatsapp = (await service.ChannelsAsync(default)).Single(x => x.Channel == "whatsapp");
        await service.SaveChannelAsync(
            "whatsapp",
            whatsapp with { Active = true, WeeklyTarget = 2, Owner = "Operator" },
            default);
        var result = (await service.OverviewAsync(default)).Channels.Single(x => x.Strategy.Channel == "whatsapp");
        result.Warnings.Should().NotContain(x => x.Contains("slot count"));
    }

    [Fact]
    public void WorkerHealth_ShouldStayUnobservedUntilChecksAndTurnStale_WhenChecksStop()
    {
        var health = new WorkerHealthService(clock);
        health.Read().Should().AllSatisfy(x => x.Status.Should().Be("unobserved"));
        health.Started("automation"); health.Completed("automation");
        (health.Read().Single(x => x.Worker == "automation").Status).Should().Be("healthy");
        clock.Advance(TimeSpan.FromMinutes(12));
        (health.Read().Single(x => x.Worker == "automation").Status).Should().Be("stale");
        health.Failed("automation"); (health.Read().Single(x => x.Worker == "automation").Status).Should().Be("failed");
        health.Completed("automation");
        (health.Read().Single(x => x.Worker == "automation").LastError).Should().BeNull();
    }

    private EngagementInput Observation() =>
        new(Guid.NewGuid().ToString(), "instagram", "https://example.test/posts/one", "Synthetic post", null,
        clock.GetUtcNow().AddHours(-2), clock.GetUtcNow().AddHours(-1), 500, 100, 4, 2, 1, 3, null, null, null,
        "Synthetic manual Insights evidence");

    private OperationsService Service(OcharoMarketingDbContext db)
    {
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(
            repository,
            new LocalFileBlobRepository(blobs),
            Options.Create(new MarketingOptions()));
        var marketing = new MarketingService(repository, assets, new Cards(), clock);
        var automation = new AutomationService(repository, marketing, new Cards(), clock);
        var catalogue = new CatalogueSyncService(
            repository,
            assets,
            new HttpClient(new NeverHttp()),
            Options.Create(new MarketingOptions()));
        return new(new OperationsRepository(db), repository, new DeliveryRepository(db), automation,
            new AutomationRunner(catalogue, automation, repository, clock, new WorkerHealthService(clock),
                NullLogger<AutomationRunner>.Instance, MarketingPostgres.NoStore()),
            TestDeliveryPolicy.Create(new ConfigurationBuilder().Build(), new TelegramProviderOptions(),
                new XProviderOptions()),
            new WorkerHealthService(clock), clock);
    }

    private sealed class NeverHttp : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new InvalidOperationException("Operations may not contact providers.");
    }
    private sealed class Cards : ICardRenderingService {
        public IReadOnlyList<CardPreset> GetPresets() =>
            [new("t02-bone", "Bone", "T02", "bone", "", false, 1080, 1350)];
        public CardPreview Compose(CardCompositionInput input) => throw new NotSupportedException();
        public Task<byte[]> RenderPngAsync(string html, CancellationToken ct) => throw new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
