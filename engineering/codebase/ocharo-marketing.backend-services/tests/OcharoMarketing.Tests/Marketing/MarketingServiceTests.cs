using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Infrastructure.Marketing.BackgroundServices;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Persistence;
using OcharoMarketing.Persistence.Marketing.Repositories;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Verifies complete editable recipes and readable atlas records across durable boundaries.</summary>
public sealed class MarketingServiceTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(Path.GetTempPath(), "ocharo-service-tests", Guid.NewGuid().ToString("N"));
    private readonly CapturingCards cards = new();
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII=");

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();
        await using var db = database.NewContext();
        var (repository, assets, _) = Services(db);
        await assets.SaveAsync(Png, "front.png", "Test front", "front", default, "front");
        await assets.SaveAsync(Png, "fabric.png", "Test fabric", "fabric", default, "fabric");
        await repository.SeedAsync(new BrandModel("Ocharo", "Asia/Tashkent", null),
            [new ProductModel("nimcha", "Nimcha vest", "Vest", "", "", "front", ["front"], [], null, "UZS", "review", "test", [])], default);
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public async Task ReopenedDraftAndImmutableRevisionPreserveTheCompleteRecipe()
    {
        var recipe = new CardRecipeModel("Nimcha", "Hand-finished vest", "fabric", 0.92, 0.04, -0.03);
        DraftModel saved;
        await using (var db = database.NewContext())
            saved = await Services(db).Service.SaveDraftAsync(null, Input() with { Composition = recipe }, default);
        await using var fresh = database.NewContext();
        var (repository, _, service) = Services(fresh);
        Assert.Equal(recipe, (await repository.FindDraftAsync(saved.Id, default))!.Composition);
        var changedRecipe = recipe with { Subtitle = "Updated supporting text", OffsetX = -0.04 };
        await service.SaveDraftAsync(saved.Id, Input() with { ExpectedRevision = 1, Composition = changedRecipe }, default);
        Assert.Equal(recipe, (await repository.FindRevisionAsync(saved.Id, 1, default))!.Composition);
        Assert.Equal(changedRecipe, (await repository.FindDraftAsync(saved.Id, default))!.Composition);
    }

    [Fact]
    public async Task RecipeBindingIsExplicitPersistedAndCannotNameUnselectedAssets()
    {
        await using var db = database.NewContext();
        var service = Services(db).Service;
        var bound = await service.SaveDraftAsync(null, Input() with { Composition = new("Title", "", null, BoundMediaIds: ["front"]) }, default);
        await using var reopened = database.NewContext();
        Assert.Equal(["front"], (await new MarketingRepository(reopened).FindDraftAsync(bound.Id, default))!.Composition!.BoundMediaIds);
        await Assert.ThrowsAsync<MarketingException>(() => service.SaveDraftAsync(null, Input() with { Composition = new("Title", "", null, BoundMediaIds: ["not-selected"]) }, default));
    }

    [Fact]
    public async Task IncompleteT01FabricCanBeSavedButInvalidRecipeCannot()
    {
        await using var db = database.NewContext();
        var service = Services(db).Service;
        var recipe = new CardRecipeModel("Nimcha", "", null);
        var input = Input() with { PresetId = "t01-fabric-right", Composition = recipe };
        Assert.NotNull(await service.SaveDraftAsync(null, input, default));
        await Assert.ThrowsAsync<MarketingException>(() => service.SaveDraftAsync(null, input with { Composition = recipe with { Scale = 2 } }, default));
        await Assert.ThrowsAsync<MarketingException>(() => service.SaveDraftAsync(null, input with { Composition = recipe with { FabricAssetId = "missing" } }, default));
    }

    [Fact]
    public async Task AtlasUsesCompletedJobTitleProductAndPresetWithoutInternalIdentifiers()
    {
        await using var db = database.NewContext();
        var (repository, assets, service) = Services(db);
        await assets.SaveAsync(Png, "card-secret-job.png", "Internal provenance secret-job", "card", default, "rendered");
        var now = DateTimeOffset.UtcNow;
        await repository.EnqueueAsync(new("secret-job", "card", "local", "Nimcha in bone", "nimcha", "queued", now, [], null),
            MarketingJson.Write(new CardRenderPayload("<html></html>", "t02-bone")), default);
        var claim = await repository.ClaimAsync(now, TimeSpan.FromMinutes(1), default);
        await repository.CompleteAsync(claim!, ["rendered"], default);
        await service.ExportAtlasAsync(["rendered"], default);
        var item = Assert.Single(cards.AtlasItems!);
        Assert.Equal("Nimcha in bone", item.Title);
        Assert.Equal("Bone studio", item.PresetName);
        Assert.Contains("Nimcha vest", item.Notes);
        Assert.DoesNotContain("secret-job", item.Title + item.PresetName + item.Notes);
    }

    [Fact]
    public async Task AtlasRejectsMoreThanTwentyFourSelectionsBeforeRendering()
    {
        await using var db = database.NewContext();
        await Assert.ThrowsAsync<MarketingException>(() => Services(db).Service.ExportAtlasAsync(Enumerable.Range(0, 25).Select(x => x.ToString()).ToArray(), default));
        Assert.Null(cards.AtlasItems);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WorkerPersistsControlledFailuresAndHidesUnexpectedDetails(bool controlled)
    {
        const string guidance = "Card text exceeds its safe area. Shorten the title and render again.";
        cards.Failure = controlled ? new CardRenderingException(guidance) : new InvalidOperationException("private-local-path");
        await using (var db = database.NewContext())
            await Services(db).Repository.EnqueueAsync(new("worker-job", "card", "local", "Title", "nimcha", "queued", DateTimeOffset.UtcNow, [], null),
                MarketingJson.Write(new CardRenderPayload("<html></html>", "t02-bone")), default);
        using var provider = new ServiceCollection()
            .AddScoped(_ => database.NewContext())
            .AddScoped<IMarketingRepository, MarketingRepository>()
            .AddScoped<IAssetService>(services => Services(services.GetRequiredService<OcharoMarketingDbContext>()).Assets)
            .AddSingleton<ICardRenderingService>(cards).BuildServiceProvider();
        using var worker = new GenerationWorker(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<GenerationWorker>.Instance, new WorkerHealthService(TimeProvider.System));
        await worker.StartAsync(default);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                await using var check = database.NewContext();
                var job = Assert.Single(await new MarketingRepository(check).GetJobsAsync(timeout.Token));
                if (job.Status == "failed")
                {
                    Assert.Equal(controlled ? guidance : "Local rendering failed. Check renderer setup and retry the card.", job.Message);
                    break;
                }
                await Task.Delay(20, timeout.Token);
            }
        }
        finally { await worker.StopAsync(default); }
    }

    private (MarketingRepository Repository, LocalAssetService Assets, MarketingService Service) Services(OcharoMarketingDbContext db)
    {
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        return (repository, assets, new MarketingService(repository, assets, cards));
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
        public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken) => throw Failure ?? new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken cancellationToken)
        {
            AtlasItems = items;
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
