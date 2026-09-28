using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Automation;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Persistence.Marketing.Repositories;
using OcharoMarketing.Tests.Marketing;
using WoW.Two.Sdk.Backend.Beta.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Automation;

/// <summary>
/// Opt-in contract checks against running sibling platforms (set OCHARO_CATALOGUE_URL / OCHARO_BLOG_URL).
/// They read the real feeds into a throwaway database; without the variables they return immediately.
/// </summary>
public sealed class LivePlatformContractTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(Path.GetTempPath(), "ocharo-live-contract", Guid.NewGuid().ToString("N"));

    public Task InitializeAsync() => database.InitializeAsync();

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public async Task Syncs_the_live_catalogue_feed()
    {
        var url = Environment.GetEnvironmentVariable("OCHARO_CATALOGUE_URL");
        if (string.IsNullOrWhiteSpace(url)) return;
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        await repository.SeedAsync(new BrandModel("OCHARO", "Asia/Tashkent", null), [], default);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        var sync = new CatalogueSyncService(repository, assets, new HttpClient(), Options.Create(new MarketingOptions { CatalogueBaseUrl = url }));

        var report = await sync.SyncAsync(default);
        var again = await sync.SyncAsync(default);
        var products = await repository.GetProductsAsync(default);

        Assert.True(report.Created >= 25, $"created {report.Created}; warnings: {string.Join("; ", report.Warnings)}");
        Assert.Equal(0, again.Created);
        Assert.Equal(0, again.ImagesStored);
        Assert.All(products, p => Assert.False(string.IsNullOrWhiteSpace(p.HeroAssetId)));
        Assert.Contains(products, p => p.Copy?.ContainsKey("ru") == true && p.Price > 0);
        Console.WriteLine($"LIVE CATALOGUE: {report.Received} received, {report.Created} created, {report.ImagesStored} images");
    }

    [Fact]
    public async Task Reads_the_live_blog_feed_and_drafts_an_announcement()
    {
        var url = Environment.GetEnvironmentVariable("OCHARO_BLOG_URL");
        if (string.IsNullOrWhiteSpace(url)) return;
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        await assets.SaveAsync(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII="),
            "front.png", "Live contract fixture", "front", default, "front");
        await repository.SeedAsync(new BrandModel("OCHARO", "Asia/Tashkent", null),
            [new ProductModel("nimcha", "Nimcha", "nimcha", "", "", "front", ["front"], [], null, "UZS", "review", "test", [])], default);
        var cards = new Presets();
        var marketing = new MarketingService(repository, assets, cards);
        var blog = new BlogStoryService(repository, marketing, new HttpClient(), Options.Create(new MarketingOptions { BlogBaseUrl = url }));

        var stories = await blog.ListAsync(default);
        var draft = await blog.DraftAsync(new StoryDraftInput(stories[0].Link, "nimcha", ["telegram"], "uz", "t02-bone"), default);

        Assert.True(stories.Length >= 30, $"stories {stories.Length}");
        Assert.All(stories, s => Assert.StartsWith("http", s.Link));
        Assert.Contains(stories[0].Link, draft.Caption);
        Console.WriteLine($"LIVE BLOG: {stories.Length} stories; first {stories[0].Link}");
    }

    private sealed class Presets : OcharoMarketing.Application.Cards.Services.ICardRenderingService
    {
        public IReadOnlyList<OcharoMarketing.Application.Cards.Models.CardPreset> GetPresets() => [new("t02-bone", "Bone studio", "T02", "bone", "", false, 1080, 1350)];
        public OcharoMarketing.Application.Cards.Models.CardPreview Compose(OcharoMarketing.Application.Cards.Models.CardCompositionInput input) => throw new NotSupportedException();
        public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<OcharoMarketing.Application.Cards.Models.CardAtlasItem> items, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
