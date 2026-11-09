using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using AwesomeAssertions;
using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Application.Generations.Services;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using OcharoMarketing.Persistence.Core.Repositories;
using OcharoMarketing.Persistence.Deliveries.Repositories;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Persistence;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Deliveries;

/// <summary>Exercises durable authorizations and paid-read guards using PostgreSQL and provider fakes only.</summary>
public sealed class DeliveryWorkflowTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
    private readonly IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> {
        ["Marketing:EnableLivePublishing"] = "true", ["Marketing:EnableAccountVerification"] = "true",
        ["Providers:Telegram:Enabled"] = "true",
        ["Providers:X:Enabled"] = "true",
        ["Providers:X:EnableMetricReads"] = "true",
        ["Marketing:EnableMetricReads"] = "true",
        ["Marketing:MaximumMetricReadsPerHour"] = "10",
        ["Marketing:MetricRefreshCooldownSeconds"] = "1" }).Build();
    private readonly TelegramProviderOptions telegram = new()
    {
        Enabled = true,
        BotToken = "fake-local-only",
        BotId = "123",
        DestinationChatId = "-1001"
    };
    private readonly XProviderOptions x = new()
    {
        Enabled = true,
        EnableMetricReads = true,
        AccessToken = "fake-local-only",
        UserId = "456"
    };
    private readonly FakeAssets assets = new();
    private readonly FakePublisher telegramPublisher = new("telegram");
    private readonly FakePublisher xPublisher = new("x");
    private readonly FakeMetrics metrics = new();
    private readonly FakeVerifier telegramVerifier = new("telegram", "123", "-1001");
    private readonly FakeVerifier xVerifier = new("x", "456", "456");

    public Task InitializeAsync() => database.InitializeAsync();
    public Task DisposeAsync() => database.DisposeAsync();

    private DeliveryPolicy Policy => TestDeliveryPolicy.Create(configuration, telegram, x);
    private PublicationService Service(OcharoMarketingDbContext db) =>
        new(new DeliveryRepository(db), new MarketingRepository(db), assets,
        new FakeExternal(), Policy, [telegramVerifier, xVerifier], [telegramPublisher, xPublisher], clock);

    private async Task<ScheduleModel> PlanAsync(OcharoMarketingDbContext db, string provider = "telegram")
    {
        var id = Guid.NewGuid().ToString("N");
        var repository = new MarketingRepository(db);
        var draft = new DraftModel(
            id,
            "Reviewed vest",
            "nimcha",
            "t02-bone",
            "en",
            "Reviewed product caption",
            [provider],
            ["front"],
            1,
            null,
            "draft",
            clock.GetUtcNow(),
            clock.GetUtcNow(),
            new("Vest", "Supporting text", null));
        await repository.SaveDraftAsync(draft, null, default);
        await repository.ApproveDraftAsync(id, 1, default);
        return await repository.ScheduleAsync(
            new(
                Guid.NewGuid().ToString("N"),
                id,
                1,
                provider,
                clock.GetUtcNow().AddMinutes(1),
                "planned",
                "Manual plan",
                null,
                draft.Title),
            default);
    }

    private async Task<PublicationOperationModel> QueueAsync(OcharoMarketingDbContext db, ScheduleModel plan)
    {
        var repository = new DeliveryRepository(db);
        if (await repository.FindConnectionAsync(plan.Channel, default) is null)
            await Service(db).VerifyAsync(plan.Channel, default);
        var revision = (await repository.FindConnectionAsync(plan.Channel, default))!.Revision;
        return await Service(
            db).QueueAsync(plan.Id, new(1, plan.Channel, revision, Guid.NewGuid().ToString()), default);
    }

    [Fact]
    public async Task Queue_ShouldNotActivateOldPlans_WhenCredentialsAppearWhilePublishingIsDisabled()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db);
        await Service(db).VerifyAsync("telegram", default);
        configuration["Marketing:EnableLivePublishing"] = "false";
        await FluentActions
            .Awaiting(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        configuration["Marketing:EnableLivePublishing"] = "true";
        clock.Advance(TimeSpan.FromMinutes(2));
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
        ((await new DeliveryRepository(db).FindScheduleAsync(plan.Id, default))!.Status).Should().Be("planned");
        telegramPublisher.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Queue_ShouldBeIdempotentAndFreezeTheApprovedRecipeAndMedia()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db);
        var operation = await QueueAsync(db, plan);
        var same = await Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, operation.Id), default);
        same.Id.Should().Be(operation.Id);
        await FluentActions
            .Awaiting(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        var draft = (await new MarketingRepository(db).FindDraftAsync(plan.DraftId, default))!;
        await new MarketingRepository(
            db).SaveDraftAsync(
                draft with
                {
                    Revision = 2,
                    Caption = "New unrelated copy",
                    Composition = draft.Composition! with { Subtitle = "Changed" }
                },
                1,
                default);
        await using var reopened = database.NewContext();
        var frozen = (await new DeliveryRepository(reopened).FindOperationAsync(plan.Id, default))!;
        frozen.Snapshot.Draft.Caption.Should().Be("Reviewed product caption");
        frozen.Snapshot.Draft.Composition!.Subtitle.Should().Be("Supporting text");
        frozen.Snapshot.Media.Should().ContainSingle().Which.Sha256.Should().Be(assets.Asset.Sha256);
        (await new DeliveryRepository(reopened).GetOperationsAsync(default)).Should().ContainSingle();
    }

    [Fact]
    public async Task Queue_ShouldRefuseAHistoricalPlan_WhenTheDraftApprovalChanged()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db);
        await Service(db).VerifyAsync("telegram", default);
        var repository = new MarketingRepository(db);
        var draft = (await repository.FindDraftAsync(plan.DraftId, default))!;
        await repository.SaveDraftAsync(draft with { Revision = 2 }, 1, default);
        await repository.ApproveDraftAsync(draft.Id, 2, default);
        await FluentActions
            .Awaiting(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }

    [Fact]
    public async Task ClaimDue_ShouldClaimOnceAndRefuseAStaleDispatch_WhenThePlanIsCancelled()
    {
        await using var first = database.NewContext();
        var plan = await PlanAsync(first);
        await QueueAsync(first, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(first);
        var claim = await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(1), default);
        claim.Should().NotBeNull();
        await using var second = database.NewContext();
        (await new DeliveryRepository(second).ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(1), default))
            .Should()
            .BeNull();
        await new DeliveryRepository(second).CancelAsync(plan.Id, clock.GetUtcNow(), default);
        await FluentActions
            .Awaiting(() => repository.MarkDispatchAsync(claim!, clock.GetUtcNow(), default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        ((await repository.FindOperationAsync(plan.Id, default))!.Status).Should().Be("cancelled");
    }

    [Fact]
    public async Task ProcessNext_ShouldMarkDispatchBeforeTheProviderAndSendTheExactSnapshot()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        telegramPublisher.BeforeReturn = async request => {
            await using var check = database.NewContext();
            var operation = (await new DeliveryRepository(check).FindOperationAsync(plan.Id, default))!;
            operation.DispatchStartedAt.Should().NotBeNull();
            request.DestinationId.Should().Be("-1001"); request.AccountId.Should().Be("123");
            request.Text.Should().Be("Reviewed product caption");
            request.Media!.Should().ContainSingle().Which.Data.Should().Equal(assets.Bytes);
        };
        (await Service(db).ProcessNextAsync(default)).Should().BeTrue();
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("published");
        telegramPublisher.Calls.Should().Be(1);
    }

    [Fact]
    public async Task RecoverExpired_ShouldMarkUnknownWithoutRepost_WhenADispatchWasInterrupted()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(db);
        var claim = (await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromSeconds(1), default))!;
        await repository.MarkDispatchAsync(claim, clock.GetUtcNow(), default);
        await FluentActions
            .Awaiting(() => repository.CancelAsync(plan.Id, clock.GetUtcNow(), default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        clock.Advance(TimeSpan.FromMinutes(11)); configuration["Marketing:EnableLivePublishing"] = "false";
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
        ((await repository.FindOperationAsync(plan.Id, default))!.Status).Should().Be("unknown");
        configuration["Marketing:EnableLivePublishing"] = "true";
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
        telegramPublisher.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessNext_ShouldRevokeUnstartedAuthorization_WhenVerificationChanges()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan);
        await Service(db).VerifyAsync("telegram", default); clock.Advance(TimeSpan.FromMinutes(2));
        (await Service(db).ProcessNextAsync(default)).Should().BeTrue();
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("held");
        telegramPublisher.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessNext_ShouldRevokeWithoutResuming_WhenTheKillSwitchFlips()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan);
        configuration["Marketing:EnableLivePublishing"] = "false";
        await Service(db).ProcessNextAsync(default);
        configuration["Marketing:EnableLivePublishing"] = "true"; clock.Advance(TimeSpan.FromMinutes(2));
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("held");
        await FluentActions
            .Awaiting(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }

    [Fact]
    public async Task ProcessNext_ShouldReadTheProviderKillSwitchBeforeDispatch()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        configuration["Providers:Telegram:Enabled"] = "false";
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("held");
        telegramPublisher.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("Providers:Telegram:BotId", "987")]
    [InlineData("Providers:Telegram:DestinationChatId", "-1987")]
    [InlineData("Providers:Telegram:BotToken", "changed-local-fake")]
    public async Task ProcessNext_ShouldHoldTheLoadedBroker_WhenTheIdentityIsHotReloaded(string key, string value)
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        configuration[key] = value;
        var connection = (await Service(db).ConnectionsAsync(default)).Single(x => x.Id == "telegram");
        connection.VerificationStatus.Should().Be("stale");
        connection.ExecutionEnabled.Should().BeFalse();
        connection.CanVerify.Should().BeFalse();
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("held");
        telegramPublisher.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessNext_ShouldMarkUnknownWithoutRetry_WhenTheProviderReturnsAnotherDestination()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        telegramPublisher.WrongIdentity = true;
        await Service(db).ProcessNextAsync(default);
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("unknown");
        (await Service(db).ProcessNextAsync(default)).Should().BeFalse(); telegramPublisher.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Verify_ShouldNotOverwriteTheSameRevision_WhenRunConcurrently()
    {
        await using var first = database.NewContext();
        await Service(first).VerifyAsync("telegram", default);
        await using var stale = database.NewContext();
        await stale.DeliveryConnections.SingleAsync(x => x.Id == "telegram");
        await Service(first).VerifyAsync("telegram", default);
        var exception = (
            await FluentActions
                .Awaiting(() => Service(stale).VerifyAsync("telegram", default))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which;
        ErrorStatus.Of(exception).Should().Be(409);
        ((await new DeliveryRepository(first).FindConnectionAsync("telegram", default))!.Revision).Should().Be(2);
    }

    [Fact]
    public async Task Refresh_ShouldNeedOptInAndKeepObservedZero_WhenReadsFailOrArePartial()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db, "x"); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        await Service(db).ProcessNextAsync(default);
        var reader = new MetricObservationService(new DeliveryRepository(db), Policy, [metrics], clock);
        configuration["Marketing:EnableMetricReads"] = "false";
        await FluentActions
            .Awaiting(() => reader.RefreshAsync(plan.Id, default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        metrics.Calls.Should().Be(0);
        configuration["Marketing:EnableMetricReads"] = "true";
        var observedAt = clock.GetUtcNow();
        metrics.Result = new(
            BrokerOutcome.Succeeded,
            "x",
            "observed",
            "Owned observation",
            [new("likes", 0, "observed", observedAt), new("impressions", null, "not_returned", null)]);
        var cache = await reader.RefreshAsync(plan.Id, default);
        (cache.Observations.Single(x => x.Name == "likes").Value).Should().Be(0);
        (cache.Observations.Single(x => x.Name == "impressions").Value).Should().BeNull();
        clock.Advance(TimeSpan.FromSeconds(2));
        metrics.Result = new(BrokerOutcome.Rejected, "x", "failed", "Read failed", []);
        cache = await reader.RefreshAsync(plan.Id, default);
        (cache.Observations.Single(x => x.Name == "likes").Value).Should().Be(0);
        (cache.Observations.Single(x => x.Name == "likes").ObservedAt).Should().Be(observedAt);
        cache.LastAttempt!.Status.Should().Be("failed");
        clock.Advance(TimeSpan.FromSeconds(2));
        metrics.Result = new(
            BrokerOutcome.Succeeded,
            "x",
            "partial",
            "Field absent",
            [new("likes", null, "not_returned", null)],
            true);
        cache = await reader.RefreshAsync(plan.Id, default);
        (cache.Observations.Single(x => x.Name == "likes").Value).Should().Be(0);
        cache.LastAttempt!.Status.Should().Be("partial");
        cache.History.Length.Should().Be(3);
        cache.History.Should().Contain(item => item.Name == "likes" && item.Value == null);
        cache.History.Should().Contain(item => item.Name == "likes" && item.Value == 0);
        var priorCalls = metrics.Calls;
        await new DeliveryRepository(db).GetCachedObservationsAsync(default);
        metrics.Calls.Should().Be(priorCalls);
    }

    [Fact]
    public async Task ReserveMetricRead_ShouldPersistTheBudgetAndCooldownAcrossContexts()
    {
        await using var db = database.NewContext();
        await Service(db).VerifyAsync("x", default);
        var repository = new DeliveryRepository(db);
        await repository.ReserveMetricReadAsync("plan-a", "x", 1, clock.GetUtcNow(), 1, 60, default);
        await using var fresh = database.NewContext();
        var blocked = (
            await FluentActions
                .Awaiting(
                    () =>
                        new DeliveryRepository(
                            fresh).ReserveMetricReadAsync("plan-b", "x", 1, clock.GetUtcNow(), 1, 60, default))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which;
        ErrorStatus.Of(blocked).Should().Be(429);
        (await db.MetricReadAttempts.ToArrayAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task Verify_ShouldRevokeThePriorIdentityAndQueue_WhenReverificationFails()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan);
        telegramVerifier.Fail = true;
        await FluentActions
            .Awaiting(() => Service(db).VerifyAsync("telegram", default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        var connection = (await Service(db).ConnectionsAsync(default)).Single(x => x.Id == "telegram");
        connection.VerificationStatus.Should().Be("verification-failed");
        connection.ExecutionEnabled.Should().BeFalse();
        clock.Advance(TimeSpan.FromMinutes(2)); await Service(db).ProcessNextAsync(default);
        ((await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status).Should().Be("held");
        telegramPublisher.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Reconcile_ShouldReconcileAKnownReceiptOnlyThroughAnOptedInOwnedRead()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db, "x"); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(db);
        var claim = (await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(5), default))!;
        await repository.MarkDispatchAsync(claim, clock.GetUtcNow(), default);
        var candidate = new ProviderReceipt(
            "x",
            "publication_unknown",
            "9001",
            new Dictionary<string, string> { ["post_id"] = "9001", ["account_id"] = "456" });
        await repository.FinishAsync(
            claim,
            "unknown",
            "Candidate final receipt",
            candidate,
            clock.GetUtcNow(),
            default);
        var broker = new FakeReconciliation();
        var service = new ReceiptReconciliationService(repository, Policy, [broker], clock);
        configuration["Marketing:EnableMetricReads"] = "false";
        await FluentActions
            .Awaiting(() => service.ReconcileAsync(plan.Id, default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        broker.Calls.Should().Be(0);
        configuration["Marketing:EnableMetricReads"] = "true";
        var reconciled = await service.ReconcileAsync(plan.Id, default);
        reconciled.Status.Should().Be("published"); broker.Calls.Should().Be(1); xPublisher.Calls.Should().Be(0);
        ((await repository.GetMetricsAsync(plan.Id, default)).LastAttempt).Should().BeNull();
        ((await db.MetricReadAttempts.ToListAsync()).Should().ContainSingle().Which.Purpose)
            .Should()
            .Be("reconciliation");
    }

    [Fact]
    public async Task Reconcile_ShouldOfferNoReconciliationOrRetry_WhenTheFinalReceiptIsMissing()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db, "x"); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(db);
        var claim = (await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(5), default))!;
        await repository.MarkDispatchAsync(claim, clock.GetUtcNow(), default);
        await repository.FinishAsync(
            claim,
            "unknown",
            "Interrupted",
            new("x", "media_uploaded", "8001"),
            clock.GetUtcNow(),
            default);
        var broker = new FakeReconciliation();
        var service = new ReceiptReconciliationService(repository, Policy, [broker], clock);
        ((await repository.FindOperationAsync(plan.Id, default))!.CanReconcile).Should().BeFalse();
        await FluentActions
            .Awaiting(() => service.ReconcileAsync(plan.Id, default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        broker.Calls.Should().Be(0); (await Service(db).ProcessNextAsync(default)).Should().BeFalse();
    }

    private sealed class FakeReconciliation : IReceiptReconciliationBroker
    {
        public string Provider => "x";
        public int Calls { get; private set; }
        public Task<ReceiptReconciliationResult> ReconcileAsync(
            ReceiptReconciliationRequest input,
            CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(
                new ReceiptReconciliationResult(BrokerOutcome.Succeeded, "x", "verified", "Owned final receipt",
                input.Receipt with { Stage = "published" }));
        }
    }

    private sealed class FakePublisher(string provider) : ISocialPublishingBroker
    {
        public string Provider => provider;
        public IReadOnlySet<string> Capabilities { get; } = new HashSet<string> { "text", "photo" };
        public int Calls { get; private set; }
        public bool WrongIdentity { get; set; }
        public Func<SocialPublishRequest, Task>? BeforeReturn { get; set; }
        public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest request, CancellationToken ct)
        {
            Calls++;
            if (BeforeReturn is not null) await BeforeReturn(request);
            return new(
                BrokerOutcome.Succeeded,
                provider,
                "published",
                "Fake publication",
                new(provider, "published", "9001",
                new Dictionary<string, string>
                {
                    [provider == "telegram" ? "chat_id" : "account_id"] = WrongIdentity
                        ? "999"
                        : provider == "telegram" ? request.DestinationId : request.AccountId!
                }));
        }
    }

    private sealed class FakeVerifier(string provider, string account, string destination) : IProviderVerificationBroker
    {
        public string Provider => provider;
        public bool Fail { get; set; }
        public Task<ProviderVerificationResult> VerifyAsync(ProviderVerificationRequest input, CancellationToken ct) =>
            Fail
                ? Task.FromResult(
                    new ProviderVerificationResult(
                        BrokerOutcome.Rejected,
                        provider,
                        "permission_revoked",
                        "Posting permission is no longer verified."))
                : Task.FromResult(
                    new ProviderVerificationResult(BrokerOutcome.Succeeded, provider, "verified", "Fake identity",
                new(account, destination, "Test account", ["text", "photo", "public-metrics"], DateTimeOffset.UtcNow)));
    }

    private sealed class FakeMetrics : ISocialMetricsBroker
    {
        public string Provider => "x";
        public int Calls { get; private set; }
        public SocialMetricsResult Result
        {
            get; set;
        } = new(BrokerOutcome.Rejected, "x", "unset", "No fake observation configured", []);
        public Task<SocialMetricsResult> ReadAsync(SocialMetricsRequest input, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAssets : IAssetService
    {
        public byte[] Bytes { get; } = [137, 80, 78, 71, 13, 10, 26, 10];
        public AssetModel Asset
        {
            get;
        } = new(
            "front",
            "front.png",
            "image/png",
            8,
            new string('a', 64),
            "Test source",
            "/api/marketing/assets/front",
            "front");
        public Task<PrivateAssetContent> ReadAsync(string id, CancellationToken ct) =>
            Task.FromResult(new PrivateAssetContent(Asset, Bytes));
        public Task<AssetContent> OpenAsync(string id, CancellationToken ct) =>
            Task.FromResult(new AssetContent(Asset, null, Bytes));
        public Task<AssetModel> SaveAsync(
            byte[] bytes,
            string fileName,
            string provenance,
            string role,
            CancellationToken ct,
            string? id = null) => throw new NotSupportedException();
        public Task<byte[]> ExportDraftAsync(string id, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeExternal : IExternalMarketingService
    {
        public IReadOnlyList<ConnectionModel> GetConnections() =>
            [
                new("telegram", "Telegram", "social", "configured", [], "", ""),
                new("x", "X", "social", "configured", [], "", "")];
        public bool CanGenerate(string provider) => false;
        public Task<object> AssistAsync(string prompt, string operation, string locale, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task ExecuteGenerationAsync(JobClaim claim, CancellationToken ct) => throw new NotSupportedException();
    }
}
