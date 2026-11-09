using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using AwesomeAssertions;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Core.Models;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Application.Products.Models;
using OcharoMarketing.Infrastructure.Assets.Services;
using OcharoMarketing.Infrastructure.Cards.Services;
using OcharoMarketing.Infrastructure.Core;
using OcharoMarketing.Infrastructure.Generations.BackgroundServices;
using OcharoMarketing.Infrastructure.Operations.Services;
using OcharoMarketing.Infrastructure.Posts.Services;
using OcharoMarketing.Persistence.Core.Repositories;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using OcharoMarketing.Persistence;
using WoW.Two.Sdk.Beta.Be.Jobs.Core.PersonOperations;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Testing;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Posts;

/// <summary>Verifies complete editable recipes and readable atlas records across durable boundaries.</summary>
public sealed class MarketingServiceTests : IAsyncLifetime
{
    // The requester of a job keys into Identity: an account and an organization the test database's Identity holds.
    private static readonly string Person = MarketingPostgres.Member.ToString("D");
    private static readonly string Organization = MarketingTestIdentity.Organization.ToString("D");
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(
        Path.GetTempPath(),
        "ocharo-service-tests",
        Guid.NewGuid().ToString("N"));
    private readonly CapturingCards cards = new();
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII=");

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();
        await using var db = database.NewContext();
        var (repository, assets, _) = Services(db);
        await assets.SaveAsync(Png, "front.png", "Test front", "front", default, "front");
        await assets.SaveAsync(Png, "fabric.png", "Test fabric", "fabric", default, "fabric");
        await repository.SeedAsync(new BrandModel("Ocharo", "Asia/Tashkent", null),
            [
                new ProductModel(
                    "nimcha",
                    "Nimcha vest",
                    "Vest",
                    "",
                    "",
                    "front",
                    ["front"],
                    [],
                    null,
                    "UZS",
                    "review",
                    "test",
                    [])],
            default);
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public async Task SaveDraft_ShouldPreserveTheCompleteRecipe_WhenTheDraftIsReopened()
    {
        var recipe = new CardRecipeModel("Nimcha", "Hand-finished vest", "fabric", 0.92, 0.04, -0.03);
        DraftModel saved;
        await using (var db = database.NewContext())
            saved = await Services(db).Service.SaveDraftAsync(null, Input() with { Composition = recipe }, default);
        await using var fresh = database.NewContext();
        var (repository, _, service) = Services(fresh);
        ((await repository.FindDraftAsync(saved.Id, default))!.Composition).Should().Be(recipe);
        var changedRecipe = recipe with { Subtitle = "Updated supporting text", OffsetX = -0.04 };
        await service.SaveDraftAsync(
            saved.Id,
            Input() with { ExpectedRevision = 1, Composition = changedRecipe },
            default);
        ((await repository.FindRevisionAsync(saved.Id, 1, default))!.Composition).Should().Be(recipe);
        ((await repository.FindDraftAsync(saved.Id, default))!.Composition).Should().Be(changedRecipe);
    }

    [Fact]
    public async Task SaveDraft_ShouldPersistExplicitBindingsAndRefuseUnselectedAssets()
    {
        await using var db = database.NewContext();
        var service = Services(db).Service;
        var bound = await service.SaveDraftAsync(
            null,
            Input() with { Composition = new("Title", "", null, BoundMediaIds: ["front"]) },
            default);
        await using var reopened = database.NewContext();
        ((await new MarketingRepository(reopened).FindDraftAsync(bound.Id, default))!.Composition!.BoundMediaIds!)
            .Should()
            .Equal(["front"]);
        await FluentActions
            .Awaiting(
                () =>
                    service.SaveDraftAsync(
                        null,
                        Input() with { Composition = new("Title", "", null, BoundMediaIds: ["not-selected"]) },
                        default))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }

    [Fact]
    public async Task SaveDraft_ShouldSaveAnIncompleteFabricButRefuseAnInvalidRecipe()
    {
        await using var db = database.NewContext();
        var service = Services(db).Service;
        var recipe = new CardRecipeModel("Nimcha", "", null);
        var input = Input() with { PresetId = "t01-fabric-right", Composition = recipe };
        (await service.SaveDraftAsync(null, input, default)).Should().NotBeNull();
        await FluentActions
            .Awaiting(
                () => service.SaveDraftAsync(null, input with { Composition = recipe with { Scale = 2 } }, default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        await FluentActions
            .Awaiting(
                () =>
                    service.SaveDraftAsync(
                        null,
                        input with { Composition = recipe with { FabricAssetId = "missing" } },
                        default))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }

    [Fact]
    public async Task ExportAtlas_ShouldUseJobTitleProductAndPresetWithoutInternalIds()
    {
        await using var db = database.NewContext();
        var (repository, assets, service) = Services(db);
        await assets.SaveAsync(
            Png,
            "card-secret-job.png",
            "Internal provenance secret-job",
            "card",
            default,
            "rendered");
        var now = DateTimeOffset.UtcNow;
        await repository.EnqueueAsync(
            new("secret-job", "card", "local", "Nimcha in bone", "nimcha", "queued", now, [], null),
            MarketingJson.Write(new CardRenderPayload("<html></html>", "t02-bone")), null, default);
        var claim = await repository.ClaimAsync(now, TimeSpan.FromMinutes(1), default);
        await repository.CompleteAsync(claim!, ["rendered"], default);
        await service.ExportAtlasAsync(["rendered"], default);
        var item = cards.AtlasItems!.Should().ContainSingle().Which;
        item.Title.Should().Be("Nimcha in bone");
        item.PresetName.Should().Be("Bone studio");
        item.Notes.Should().Contain("Nimcha vest");
        (item.Title + item.PresetName + item.Notes).Should().NotContain("secret-job");
    }

    [Fact]
    public async Task ExportAtlas_ShouldRefuseBeforeRendering_WhenOverTwentyFourAreSelected()
    {
        await using var db = database.NewContext();
        await FluentActions
            .Awaiting(
                () =>
                    Services(
                        db).Service.ExportAtlasAsync(
                            Enumerable.Range(0, 25).Select(x => x.ToString()).ToArray(),
                            default))
            .Should()
            .ThrowExactlyAsync<AppException>();
        cards.AtlasItems.Should().BeNull();
    }

    [Fact]
    public async Task Worker_ShouldStopAQueuedJob_WhenThePersonWhoAskedLostAccess()
    {
        var operations = Operations();
        await using (var db = database.NewContext())
            await Services(
                db).Repository.EnqueueAsync(
                    new(
                        "access-job",
                        "card",
                        "local",
                        "Title",
                        "nimcha",
                        "queued",
                        DateTimeOffset.UtcNow.AddSeconds(-1),
                        [],
                        null),
                MarketingJson.Write(new CardRenderPayload("<html></html>", "t02-bone")), new(Person, Organization),
                default);
        operations.AccessLost(Person, Organization);
        cards.Failure = new InvalidOperationException("the render ran");

        var job = await RunWorkerUntilFinishedAsync(operations);

        job.Status.Should().Be("failed");
        job.Message.Should().Contain("no longer has access");
    }

    [Fact]
    public async Task Worker_ShouldRunAQueuedJob_WhenAnotherPersonLostAccessOrThePersonSignedOut()
    {
        var operations = Operations();
        await using (var db = database.NewContext())
            await Services(
                db).Repository.EnqueueAsync(
                    new(
                        "kept-job",
                        "card",
                        "local",
                        "Title",
                        "nimcha",
                        "queued",
                        DateTimeOffset.UtcNow.AddSeconds(-1),
                        [],
                        null),
                MarketingJson.Write(new CardRenderPayload("<html></html>", "t02-bone")), new(Person, Organization),
                default);
        operations.AccessLost("someone-else", Organization);
        operations.AccessLost(Person, "another-organization");
        operations.SessionEnded("session", Person);
        cards.Failure = new CardRenderingException("the render ran");

        var job = await RunWorkerUntilFinishedAsync(operations);

        job.Message.Should().Be("the render ran");
    }

    private static IPersonOperationTracker Operations() =>
        new ServiceCollection()
            .AddLogging()
            .AddPersonOperations()
            .BuildServiceProvider()
            .GetRequiredService<IPersonOperationTracker>();

    private async Task<GenerationJobModel> RunWorkerUntilFinishedAsync(IPersonOperationTracker operations)
    {
        using var provider = new ServiceCollection()
            .AddScoped(_ => database.NewContext())
            .AddScoped<IMarketingRepository, MarketingRepository>()
            .AddScoped<IAssetService>(
                services => Services(services.GetRequiredService<OcharoMarketingDbContext>()).Assets)
            .AddSingleton<ICardRenderingService>(cards).BuildServiceProvider();
        using var worker = new GenerationWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<GenerationWorker>.Instance, new WorkerHealthService(TimeProvider.System), operations,
            TimeProvider.System, MarketingPostgres.NoStore());
        await worker.StartAsync(default);
        try
        {
            return await PollingExtensions.UntilAsync(
                SingleJobAsync,
                job => job.Status is not ("queued" or "running"),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(25));
        }
        finally { await worker.StopAsync(default); }
    }

    /// <summary>Reads the one job the test queued, from a fresh context.</summary>
    private async Task<GenerationJobModel> SingleJobAsync()
    {
        await using var check = database.NewContext();
        return (await new MarketingRepository(check).GetJobsAsync(default)).Should().ContainSingle().Which;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Worker_ShouldSaveControlledFailuresAndHideUnexpectedDetails(bool controlled)
    {
        const string guidance = "Card text exceeds its safe area. Shorten the title and render again.";
        cards.Failure = controlled
            ? new CardRenderingException(guidance)
            : new InvalidOperationException("private-local-path");
        await using (var db = database.NewContext())
            await Services(
                db).Repository.EnqueueAsync(
                    new("worker-job", "card", "local", "Title", "nimcha", "queued", DateTimeOffset.UtcNow, [], null),
                MarketingJson.Write(new CardRenderPayload("<html></html>", "t02-bone")), null, default);
        using var provider = new ServiceCollection()
            .AddScoped(_ => database.NewContext())
            .AddScoped<IMarketingRepository, MarketingRepository>()
            .AddScoped<IAssetService>(
                services => Services(services.GetRequiredService<OcharoMarketingDbContext>()).Assets)
            .AddSingleton<ICardRenderingService>(cards).BuildServiceProvider();
        using var worker = new GenerationWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<GenerationWorker>.Instance, new WorkerHealthService(TimeProvider.System), Operations(),
            TimeProvider.System, MarketingPostgres.NoStore());
        await worker.StartAsync(default);
        try
        {
            var job = await PollingExtensions.UntilAsync(
                SingleJobAsync,
                job => job.Status == "failed",
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(20));
            job.Status.Should().Be("failed");
            job.Message
                .Should()
                .Be(controlled ? guidance : "Local rendering failed. Check renderer setup and retry the card.");
        }
        finally { await worker.StopAsync(default); }
    }

    private (
        MarketingRepository Repository,
        LocalAssetService Assets,
        MarketingService Service) Services(OcharoMarketingDbContext db)
    {
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(
            repository,
            new LocalFileBlobRepository(blobRoot),
            Options.Create(new MarketingOptions()));
        return (repository, assets, new MarketingService(repository, assets, cards, TimeProvider.System));
    }

    private static DraftInput Input() => new("Nimcha", "nimcha", "t02-bone", "en", "Caption", ["instagram"]);

    private sealed class CapturingCards : ICardRenderingService
    {
        public IReadOnlyList<CardAtlasItem>? AtlasItems { get; private set; }
        public Exception? Failure { get; set; }
        public IReadOnlyList<CardPreset> GetPresets() => [
            new("t02-bone", "Bone studio", "T02", "bone", "", false, 1080, 1350),
            new("t01-fabric-right", "Fabric right", "T01", "bone", "", true, 1080, 1350)];
        public CardPreview Compose(CardCompositionInput input) => throw new NotSupportedException();
        public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken) =>
            throw Failure ?? new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken cancellationToken)
        {
            AtlasItems = items;
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
