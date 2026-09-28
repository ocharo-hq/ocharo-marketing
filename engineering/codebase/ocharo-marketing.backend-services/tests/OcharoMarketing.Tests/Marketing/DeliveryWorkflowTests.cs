using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Persistence;
using OcharoMarketing.Persistence.Marketing.Repositories;
using Xunit;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Exercises durable authorizations and paid-read guards using SQLite and provider fakes only.</summary>
public sealed class DeliveryWorkflowTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly TestClock clock = new();
    private readonly IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["Marketing:EnableLivePublishing"] = "true", ["Marketing:EnableAccountVerification"] = "true",
        ["Providers:Telegram:Enabled"] = "true", ["Providers:X:Enabled"] = "true", ["Providers:X:EnableMetricReads"] = "true",
        ["Marketing:EnableMetricReads"] = "true", ["Marketing:MaximumMetricReadsPerHour"] = "10", ["Marketing:MetricRefreshCooldownSeconds"] = "1" }).Build();
    private readonly TelegramProviderOptions telegram = new() { Enabled = true, BotToken = "fake-local-only", BotId = "123", DestinationChatId = "-1001" };
    private readonly XProviderOptions x = new() { Enabled = true, EnableMetricReads = true, AccessToken = "fake-local-only", UserId = "456" };
    private readonly FakeAssets assets = new();
    private readonly FakePublisher telegramPublisher = new("telegram");
    private readonly FakePublisher xPublisher = new("x");
    private readonly FakeMetrics metrics = new();
    private readonly FakeVerifier telegramVerifier = new("telegram", "123", "-1001");
    private readonly FakeVerifier xVerifier = new("x", "456", "456");

    public Task InitializeAsync() => database.InitializeAsync();
    public Task DisposeAsync() => database.DisposeAsync();

    private DeliveryPolicy Policy => new(configuration, telegram, x);
    private PublicationService Service(OcharoMarketingDbContext db) => new(new DeliveryRepository(db), new MarketingRepository(db), assets,
        new FakeExternal(), Policy, [telegramVerifier, xVerifier], [telegramPublisher, xPublisher], clock);

    private async Task<ScheduleModel> PlanAsync(OcharoMarketingDbContext db, string provider = "telegram")
    {
        var id = Guid.NewGuid().ToString("N");
        var repository = new MarketingRepository(db);
        var draft = new DraftModel(id, "Reviewed vest", "nimcha", "t02-bone", "en", "Reviewed product caption", [provider], ["front"], 1, null, "draft", clock.GetUtcNow(), clock.GetUtcNow(), new("Vest", "Supporting text", null));
        await repository.SaveDraftAsync(draft, null, default);
        await repository.ApproveDraftAsync(id, 1, default);
        return await repository.ScheduleAsync(new(Guid.NewGuid().ToString("N"), id, 1, provider, clock.GetUtcNow().AddMinutes(1), "planned", "Manual plan", null, draft.Title), default);
    }

    private async Task<PublicationOperationModel> QueueAsync(OcharoMarketingDbContext db, ScheduleModel plan)
    {
        var repository = new DeliveryRepository(db);
        if (await repository.FindConnectionAsync(plan.Channel, default) is null) await Service(db).VerifyAsync(plan.Channel, default);
        var revision = (await repository.FindConnectionAsync(plan.Channel, default))!.Revision;
        return await Service(db).QueueAsync(plan.Id, new(1, plan.Channel, revision, Guid.NewGuid().ToString()), default);
    }

    [Fact]
    public async Task DisabledPublishingNeverActivatesOldPlansWhenCredentialsAppear()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db);
        await Service(db).VerifyAsync("telegram", default);
        configuration["Marketing:EnableLivePublishing"] = "false";
        await Assert.ThrowsAsync<MarketingException>(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default));
        configuration["Marketing:EnableLivePublishing"] = "true";
        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.False(await Service(db).ProcessNextAsync(default));
        Assert.Equal("planned", (await new DeliveryRepository(db).FindScheduleAsync(plan.Id, default))!.Status);
        Assert.Equal(0, telegramPublisher.Calls);
    }

    [Fact]
    public async Task QueueCommandIsIdempotentAndFreezesTheApprovedRecipeAndMedia()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db);
        var operation = await QueueAsync(db, plan);
        var same = await Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, operation.Id), default);
        Assert.Equal(operation.Id, same.Id);
        await Assert.ThrowsAsync<MarketingException>(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default));
        var draft = (await new MarketingRepository(db).FindDraftAsync(plan.DraftId, default))!;
        await new MarketingRepository(db).SaveDraftAsync(draft with { Revision = 2, Caption = "New unrelated copy", Composition = draft.Composition! with { Subtitle = "Changed" } }, 1, default);
        await using var reopened = database.NewContext();
        var frozen = (await new DeliveryRepository(reopened).FindOperationAsync(plan.Id, default))!;
        Assert.Equal("Reviewed product caption", frozen.Snapshot.Draft.Caption);
        Assert.Equal("Supporting text", frozen.Snapshot.Draft.Composition!.Subtitle);
        Assert.Equal(assets.Asset.Sha256, Assert.Single(frozen.Snapshot.Media).Sha256);
        Assert.Single(await new DeliveryRepository(reopened).GetOperationsAsync(default));
    }

    [Fact]
    public async Task HistoricalPlanCannotQueueAfterDraftApprovalChanges()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db);
        await Service(db).VerifyAsync("telegram", default);
        var repository = new MarketingRepository(db);
        var draft = (await repository.FindDraftAsync(plan.DraftId, default))!;
        await repository.SaveDraftAsync(draft with { Revision = 2 }, 1, default);
        await repository.ApproveDraftAsync(draft.Id, 2, default);
        await Assert.ThrowsAsync<MarketingException>(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default));
    }

    [Fact]
    public async Task TwoContextsClaimOnlyOnceAndStaleClaimCannotDispatchAfterCancellation()
    {
        await using var first = database.NewContext();
        var plan = await PlanAsync(first);
        await QueueAsync(first, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(first);
        var claim = await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(1), default);
        Assert.NotNull(claim);
        await using var second = database.NewContext();
        Assert.Null(await new DeliveryRepository(second).ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(1), default));
        await new DeliveryRepository(second).CancelAsync(plan.Id, clock.GetUtcNow(), default);
        await Assert.ThrowsAsync<MarketingException>(() => repository.MarkDispatchAsync(claim!, clock.GetUtcNow(), default));
        Assert.Equal("cancelled", (await repository.FindOperationAsync(plan.Id, default))!.Status);
    }

    [Fact]
    public async Task DispatchMarkerPrecedesProviderEffectAndExactSnapshotIsSent()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        telegramPublisher.BeforeReturn = async request => {
            await using var check = database.NewContext();
            var operation = (await new DeliveryRepository(check).FindOperationAsync(plan.Id, default))!;
            Assert.NotNull(operation.DispatchStartedAt);
            Assert.Equal("-1001", request.DestinationId); Assert.Equal("123", request.AccountId);
            Assert.Equal("Reviewed product caption", request.Text);
            Assert.Equal(assets.Bytes, Assert.Single(request.Media!).Data);
        };
        Assert.True(await Service(db).ProcessNextAsync(default));
        Assert.Equal("published", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status);
        Assert.Equal(1, telegramPublisher.Calls);
    }

    [Fact]
    public async Task InterruptedDispatchBecomesUnknownEvenWithPublishingDisabledAndCannotRepost()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(db);
        var claim = (await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromSeconds(1), default))!;
        await repository.MarkDispatchAsync(claim, clock.GetUtcNow(), default);
        await Assert.ThrowsAsync<MarketingException>(() => repository.CancelAsync(plan.Id, clock.GetUtcNow(), default));
        clock.Advance(TimeSpan.FromMinutes(11)); configuration["Marketing:EnableLivePublishing"] = "false";
        Assert.False(await Service(db).ProcessNextAsync(default));
        Assert.Equal("unknown", (await repository.FindOperationAsync(plan.Id, default))!.Status);
        configuration["Marketing:EnableLivePublishing"] = "true";
        Assert.False(await Service(db).ProcessNextAsync(default));
        Assert.Equal(0, telegramPublisher.Calls);
    }

    [Fact]
    public async Task ChangedVerificationRevokesUnstartedFrozenAuthorization()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan);
        await Service(db).VerifyAsync("telegram", default); clock.Advance(TimeSpan.FromMinutes(2));
        Assert.True(await Service(db).ProcessNextAsync(default));
        Assert.Equal("held", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status);
        Assert.Equal(0, telegramPublisher.Calls);
    }

    [Fact]
    public async Task KillSwitchRevokesQueuedOperationAndReenableDoesNotResumeIt()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan);
        configuration["Marketing:EnableLivePublishing"] = "false";
        await Service(db).ProcessNextAsync(default);
        configuration["Marketing:EnableLivePublishing"] = "true"; clock.Advance(TimeSpan.FromMinutes(2));
        Assert.False(await Service(db).ProcessNextAsync(default));
        Assert.Equal("held", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status);
        await Assert.ThrowsAsync<MarketingException>(() => Service(db).QueueAsync(plan.Id, new(1, "telegram", 1, Guid.NewGuid().ToString()), default));
    }

    [Fact]
    public async Task ProviderKillSwitchIsReadDynamicallyBeforeDispatch()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        configuration["Providers:Telegram:Enabled"] = "false";
        Assert.False(await Service(db).ProcessNextAsync(default));
        Assert.Equal("held", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status);
        Assert.Equal(0, telegramPublisher.Calls);
    }

    [Theory]
    [InlineData("Providers:Telegram:BotId", "987")]
    [InlineData("Providers:Telegram:DestinationChatId", "-1987")]
    [InlineData("Providers:Telegram:BotToken", "changed-local-fake")]
    public async Task HotReloadedIdentityChangesHoldLoadedBrokerUntilRestart(string key, string value)
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        configuration[key] = value;
        var connection = (await Service(db).ConnectionsAsync(default)).Single(x => x.Id == "telegram");
        Assert.Equal("stale", connection.VerificationStatus); Assert.False(connection.ExecutionEnabled); Assert.False(connection.CanVerify);
        Assert.False(await Service(db).ProcessNextAsync(default));
        Assert.Equal("held", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status);
        Assert.Equal(0, telegramPublisher.Calls);
    }

    [Fact]
    public async Task WrongReturnedDestinationIsUnknownAndNeverRetries()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        telegramPublisher.WrongIdentity = true;
        await Service(db).ProcessNextAsync(default);
        Assert.Equal("unknown", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status);
        Assert.False(await Service(db).ProcessNextAsync(default)); Assert.Equal(1, telegramPublisher.Calls);
    }

    [Fact]
    public async Task ConcurrentVerificationCannotOverwriteTheSameRevision()
    {
        await using var first = database.NewContext();
        await Service(first).VerifyAsync("telegram", default);
        await using var stale = database.NewContext();
        await stale.DeliveryConnections.SingleAsync(x => x.Id == "telegram");
        await Service(first).VerifyAsync("telegram", default);
        var exception = await Assert.ThrowsAsync<MarketingException>(() => Service(stale).VerifyAsync("telegram", default));
        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(2, (await new DeliveryRepository(first).FindConnectionAsync("telegram", default))!.Revision);
    }

    [Fact]
    public async Task MetricsRequireExplicitOptInAndPreserveObservedZeroAcrossFailedAndPartialReads()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db, "x"); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        await Service(db).ProcessNextAsync(default);
        var reader = new MetricObservationService(new DeliveryRepository(db), Policy, [metrics], clock);
        configuration["Marketing:EnableMetricReads"] = "false";
        await Assert.ThrowsAsync<MarketingException>(() => reader.RefreshAsync(plan.Id, default)); Assert.Equal(0, metrics.Calls);
        configuration["Marketing:EnableMetricReads"] = "true";
        var observedAt = clock.GetUtcNow();
        metrics.Result = new(BrokerOutcome.Succeeded, "x", "observed", "Owned observation", [new("likes", 0, "observed", observedAt), new("impressions", null, "not_returned", null)]);
        var cache = await reader.RefreshAsync(plan.Id, default);
        Assert.Equal(0, cache.Observations.Single(x => x.Name == "likes").Value); Assert.Null(cache.Observations.Single(x => x.Name == "impressions").Value);
        clock.Advance(TimeSpan.FromSeconds(2)); metrics.Result = new(BrokerOutcome.Rejected, "x", "failed", "Read failed", []);
        cache = await reader.RefreshAsync(plan.Id, default);
        Assert.Equal(0, cache.Observations.Single(x => x.Name == "likes").Value); Assert.Equal(observedAt, cache.Observations.Single(x => x.Name == "likes").ObservedAt);
        Assert.Equal("failed", cache.LastAttempt!.Status);
        clock.Advance(TimeSpan.FromSeconds(2)); metrics.Result = new(BrokerOutcome.Succeeded, "x", "partial", "Field absent", [new("likes", null, "not_returned", null)], true);
        cache = await reader.RefreshAsync(plan.Id, default);
        Assert.Equal(0, cache.Observations.Single(x => x.Name == "likes").Value);
        Assert.Equal("partial", cache.LastAttempt!.Status);
        Assert.Equal(3, cache.History.Length);
        Assert.Contains(cache.History, item => item.Name == "likes" && item.Value is null);
        Assert.Contains(cache.History, item => item.Name == "likes" && item.Value == 0);
        var priorCalls = metrics.Calls;
        await new DeliveryRepository(db).GetCachedObservationsAsync(default);
        Assert.Equal(priorCalls, metrics.Calls);
    }

    [Fact]
    public async Task PaidReadBudgetAndCooldownArePersistedAcrossContexts()
    {
        await using var db = database.NewContext();
        await Service(db).VerifyAsync("x", default);
        var repository = new DeliveryRepository(db);
        await repository.ReserveMetricReadAsync("plan-a", "x", 1, clock.GetUtcNow(), 1, 60, default);
        await using var fresh = database.NewContext();
        var blocked = await Assert.ThrowsAsync<MarketingException>(() => new DeliveryRepository(fresh).ReserveMetricReadAsync("plan-b", "x", 1, clock.GetUtcNow(), 1, 60, default));
        Assert.Equal(429, blocked.StatusCode);
        Assert.Single(await db.MetricReadAttempts.ToArrayAsync());
    }

    [Fact]
    public async Task AdditiveMigrationPreservesExistingDraftsAndPlans()
    {
        await using var db = database.NewContext();
        await db.Database.EnsureDeletedAsync();
        // The applied legacy ID predates EF's 14-digit target-name format. Recreate
        // its real operations/history without renaming an already-applied migration.
        var assembly = db.GetService<IMigrationsAssembly>();
        var legacyId = "202609270001_InitialMarketing";
        var migration = assembly.CreateMigration(assembly.Migrations[legacyId], db.Database.ProviderName!);
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var history = db.GetService<IHistoryRepository>();
        await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
        await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(legacyId, "10.0.3")));
        var plan = await PlanAsync(db);
        await new MarketingRepository(db).SaveAssetAsync(assets.Asset, "originals/front/test", default);
        await db.Database.MigrateAsync();
        Assert.NotNull(await new MarketingRepository(db).FindDraftAsync(plan.DraftId, default));
        Assert.Equal("planned", (await new DeliveryRepository(db).FindScheduleAsync(plan.Id, default))!.Status);
        Assert.NotNull(await new MarketingRepository(db).FindAssetAsync("front", default));
        Assert.Empty(await db.PublicationOperations.ToListAsync());
        Assert.Empty(await db.SourceAnalyses.ToListAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task FailedReverificationRevokesPriorIdentityAndQueueAuthorization()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db); await QueueAsync(db, plan);
        telegramVerifier.Fail = true;
        await Assert.ThrowsAsync<MarketingException>(() => Service(db).VerifyAsync("telegram", default));
        var connection = (await Service(db).ConnectionsAsync(default)).Single(x => x.Id == "telegram");
        Assert.Equal("verification-failed", connection.VerificationStatus); Assert.False(connection.ExecutionEnabled);
        clock.Advance(TimeSpan.FromMinutes(2)); await Service(db).ProcessNextAsync(default);
        Assert.Equal("held", (await new DeliveryRepository(db).FindOperationAsync(plan.Id, default))!.Status); Assert.Equal(0, telegramPublisher.Calls);
    }

    [Fact]
    public async Task KnownFinalReceiptCanBeReconciledOnlyThroughOptedInOwnedRead()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db, "x"); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(db);
        var claim = (await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(5), default))!;
        await repository.MarkDispatchAsync(claim, clock.GetUtcNow(), default);
        var candidate = new ProviderReceipt("x", "publication_unknown", "9001", new Dictionary<string, string> { ["post_id"] = "9001", ["account_id"] = "456" });
        await repository.FinishAsync(claim, "unknown", "Candidate final receipt", candidate, clock.GetUtcNow(), default);
        var broker = new FakeReconciliation();
        var service = new ReceiptReconciliationService(repository, Policy, [broker], clock);
        configuration["Marketing:EnableMetricReads"] = "false";
        await Assert.ThrowsAsync<MarketingException>(() => service.ReconcileAsync(plan.Id, default)); Assert.Equal(0, broker.Calls);
        configuration["Marketing:EnableMetricReads"] = "true";
        var reconciled = await service.ReconcileAsync(plan.Id, default);
        Assert.Equal("published", reconciled.Status); Assert.Equal(1, broker.Calls); Assert.Equal(0, xPublisher.Calls);
        Assert.Null((await repository.GetMetricsAsync(plan.Id, default)).LastAttempt);
        Assert.Equal("reconciliation", Assert.Single(await db.MetricReadAttempts.ToListAsync()).Purpose);
    }

    [Fact]
    public async Task MissingFinalReceiptOffersNoReconciliationOrPostingRetry()
    {
        await using var db = database.NewContext();
        var plan = await PlanAsync(db, "x"); await QueueAsync(db, plan); clock.Advance(TimeSpan.FromMinutes(2));
        var repository = new DeliveryRepository(db);
        var claim = (await repository.ClaimDueAsync(clock.GetUtcNow(), TimeSpan.FromMinutes(5), default))!;
        await repository.MarkDispatchAsync(claim, clock.GetUtcNow(), default);
        await repository.FinishAsync(claim, "unknown", "Interrupted", new("x", "media_uploaded", "8001"), clock.GetUtcNow(), default);
        var broker = new FakeReconciliation();
        var service = new ReceiptReconciliationService(repository, Policy, [broker], clock);
        Assert.False((await repository.FindOperationAsync(plan.Id, default))!.CanReconcile);
        await Assert.ThrowsAsync<MarketingException>(() => service.ReconcileAsync(plan.Id, default));
        Assert.Equal(0, broker.Calls); Assert.False(await Service(db).ProcessNextAsync(default));
    }

    private sealed class FakeReconciliation : IReceiptReconciliationBroker
    {
        public string Provider => "x";
        public int Calls { get; private set; }
        public Task<ReceiptReconciliationResult> ReconcileAsync(ReceiptReconciliationRequest input, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new ReceiptReconciliationResult(BrokerOutcome.Succeeded, "x", "verified", "Owned final receipt",
                input.Receipt with { Stage = "published" }));
        }
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now = now.Add(duration);
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
            return new(BrokerOutcome.Succeeded, provider, "published", "Fake publication", new(provider, "published", "9001",
                new Dictionary<string, string> { [provider == "telegram" ? "chat_id" : "account_id"] = WrongIdentity ? "999" : provider == "telegram" ? request.DestinationId : request.AccountId! }));
        }
    }

    private sealed class FakeVerifier(string provider, string account, string destination) : IProviderVerificationBroker
    {
        public string Provider => provider;
        public bool Fail { get; set; }
        public Task<ProviderVerificationResult> VerifyAsync(ProviderVerificationRequest input, CancellationToken ct) =>
            Fail ? Task.FromResult(new ProviderVerificationResult(BrokerOutcome.Rejected, provider, "permission_revoked", "Posting permission is no longer verified.")) : Task.FromResult(new ProviderVerificationResult(BrokerOutcome.Succeeded, provider, "verified", "Fake identity",
                new(account, destination, "Test account", ["text", "photo", "public-metrics"], DateTimeOffset.UtcNow)));
    }

    private sealed class FakeMetrics : ISocialMetricsBroker
    {
        public string Provider => "x";
        public int Calls { get; private set; }
        public SocialMetricsResult Result { get; set; } = new(BrokerOutcome.Rejected, "x", "unset", "No fake observation configured", []);
        public Task<SocialMetricsResult> ReadAsync(SocialMetricsRequest input, CancellationToken ct) { Calls++; return Task.FromResult(Result); }
    }

    private sealed class FakeAssets : IAssetService
    {
        public byte[] Bytes { get; } = [137, 80, 78, 71, 13, 10, 26, 10];
        public AssetModel Asset { get; } = new("front", "front.png", "image/png", 8, new string('a', 64), "Test source", "/api/marketing/assets/front", "front");
        public Task<PrivateAssetContent> ReadAsync(string id, CancellationToken ct) => Task.FromResult(new PrivateAssetContent(Asset, Bytes));
        public Task<AssetModel> SaveAsync(byte[] bytes, string fileName, string provenance, string role, CancellationToken ct, string? id = null) => throw new NotSupportedException();
        public Task<byte[]> ExportDraftAsync(string id, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeExternal : IExternalMarketingService
    {
        public IReadOnlyList<ConnectionModel> GetConnections() => [new("telegram", "Telegram", "social", "configured", [], "", ""), new("x", "X", "social", "configured", [], "", "")];
        public bool CanGenerate(string provider) => false;
        public Task<object> AssistAsync(string prompt, string operation, string locale, CancellationToken ct) => throw new NotSupportedException();
        public Task ExecuteGenerationAsync(JobClaim claim, CancellationToken ct) => throw new NotSupportedException();
    }
}
