using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Automation;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Persistence;
using OcharoMarketing.Persistence.Marketing.Repositories;
using OcharoMarketing.Tests.Marketing;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Automation;

/// <summary>Covers catalogue sync, caption templates, posting slots and blog-story drafts against real SQLite.</summary>
public sealed class AutomationTests : IAsyncLifetime
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII=");
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(Path.GetTempPath(), "ocharo-automation-tests", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();
        await using var db = database.NewContext();
        var (repository, assets, _, _) = Services(db);
        await assets.SaveAsync(Png, "front.png", "Test front", "front", default, "front");
        await repository.SeedAsync(new BrandModel("OCHARO", "Asia/Tashkent", null),
            [new ProductModel("nimcha", "Nimcha", "nimcha", "Sintetik", "", "front", ["front"], ["S", "M"], 130000m, "UZS", "review", "test", [],
                new Dictionary<string, ProductCopyModel> { ["ru"] = new("draft", "Жилет", "Синтетика", null, null) }, 100000m)], default);
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public void Templates_fill_locale_copy_and_drop_empty_lines()
    {
        var product = new ProductModel("nimcha", "Nimcha", "nimcha", "Sintetik mato", "", "front", ["front"], ["S", "M"], 130000m, "UZS", "r", "t", [],
            new Dictionary<string, ProductCopyModel> { ["ru"] = new("draft", "Жилет", null, null, null) });
        var brand = new BrandModel("OCHARO", "Asia/Tashkent", null);

        var uz = AutomationService.Render(AutomationService.DefaultTemplates["uz"], product, "uz", brand);
        var ru = AutomationService.Render(AutomationService.DefaultTemplates["ru"], product, "ru", brand);

        Assert.StartsWith("Nimcha\n\nMato: Sintetik mato\nO‘lchamlar: S · M\nNarx: 130 000 so‘m", uz);
        Assert.DoesNotContain("Bezak", uz);
        Assert.StartsWith("Жилет\n\nРазмеры: S · M\nЦена: 130 000 сум", ru);
        Assert.DoesNotContain("Ткань", ru);
        Assert.Equal("130,000 UZS", AutomationService.Price(130000m, "en"));
        Assert.DoesNotContain("Mavjud", uz);
    }

    [Fact]
    public void Available_sizes_come_from_catalogue_stock_or_made_to_order()
    {
        var brand = new BrandModel("OCHARO", "Asia/Tashkent", null);
        var inStock = new ProductModel("n", "Nimcha", "nimcha", "", "", "front", ["front"], ["S", "M", "L"], null, "UZS", "r", "t", [],
            Availability: "in-stock", AvailableSizes: ["S", "L"]);
        var toOrder = inStock with { Availability = "made-to-order", AvailableSizes = ["S", "M", "L"] };

        Assert.Contains("Mavjud: S · L", AutomationService.Render(AutomationService.DefaultTemplates["uz"], inStock, "uz", brand));
        Assert.Contains("В наличии: под заказ", AutomationService.Render(AutomationService.DefaultTemplates["ru"], toOrder, "ru", brand));
    }

    [Fact]
    public void Slot_occurrences_use_tashkent_time_weekdays_and_horizon()
    {
        var slot = new PostingSlotModel("s", "telegram", [1, 3], "19:00", true);
        var monday0900Utc = new DateTimeOffset(2026, 9, 28, 9, 0, 0, TimeSpan.Zero); // Monday 14:00 in Tashkent

        var times = AutomationService.Occurrences(slot, monday0900Utc, 7).ToList();

        Assert.Equal([new DateTimeOffset(2026, 9, 28, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 30, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 5, 14, 0, 0, TimeSpan.Zero)], times);
    }

    [Fact]
    public async Task Fill_plans_approved_drafts_into_free_slots_once_per_channel()
    {
        await using var db = database.NewContext();
        var (repository, _, service, automation) = Services(db);
        var draft = await service.SaveDraftAsync(null, new DraftInput("Nimcha", "nimcha", "t02-bone", "uz", "Caption", ["telegram", "instagram", "whatsapp"]), default);
        await service.ApproveAsync(draft.Id, draft.Revision, default);
        var unapproved = await service.SaveDraftAsync(null, new DraftInput("Draft only", "nimcha", "t02-bone", "uz", "Caption", ["telegram"]), default);
        await automation.SaveAsync(AutomationService.Default() with { Slots = [new PostingSlotModel("", "telegram", [1, 2, 3, 4, 5, 6, 7], "10:00", true)] }, default);

        var first = await automation.FillAsync(14, default);
        var second = await automation.FillAsync(14, default);
        var schedules = await repository.GetSchedulesAsync(default);

        var plan = Assert.Single(first.Planned);
        Assert.Equal("telegram", plan.Channel);
        Assert.Equal("planned", plan.Status);
        Assert.Contains(first.Skipped, s => s.Contains("instagram"));
        Assert.Empty(second.Planned);
        Assert.DoesNotContain(schedules, s => s.DraftId == unapproved.Id);
        Assert.Equal(10, TimeZoneInfo.ConvertTime(plan.ScheduledAt, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent")).Hour);
    }

    [Fact]
    public async Task Bulk_drafts_use_the_locale_template_and_hero_image()
    {
        await using var db = database.NewContext();
        var automation = Services(db).Automation;

        var drafts = await automation.CreateDraftsAsync(new BulkDraftInput(["nimcha"], "ru", "t02-bone", ["telegram"], null), default);

        var draft = Assert.Single(drafts);
        Assert.StartsWith("Жилет", draft.Caption);
        Assert.Contains("Цена: 130 000 сум", draft.Caption);
        Assert.Equal(["front"], draft.MediaIds);
        Assert.Equal("draft", draft.Status);
    }

    [Fact]
    public async Task Catalogue_sync_imports_copy_prices_and_hash_verified_images()
    {
        var sha = Convert.ToHexStringLower(SHA256.HashData(Png));
        var feed = new
        {
            products = new object[]
            {
                new
                {
                    id = "baska-v02", name = "Baska", garmentType = "baska", fabric = "Atlas", description = (string?)null, sizes = new[] { "S" }, revision = 3,
                    pricing = new { retail = 180000, wholesale = 130000 },
                    locales = new { uz = new { status = "draft", name = "Baska", fabric = "Atlas" }, ru = new { status = "draft", name = "Баска" }, en = new { status = "approved", name = "Peplum" } },
                    images = new[] { new { role = "cutout", url = "/api/catalogue/media/" + sha, sha256 = sha } },
                },
                new
                {
                    id = "broken", name = "Broken", garmentType = "baska", fabric = (string?)null, description = (string?)null, sizes = Array.Empty<string>(), revision = 1,
                    pricing = (object?)null, locales = new { uz = new { status = "draft", name = "Broken" } },
                    images = new[] { new { role = "cutout", url = "/api/catalogue/media/bad", sha256 = new string('0', 64) } },
                },
            },
        };
        var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/public/catalogue" => Json(feed),
            _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Png) },
        }));
        await using var db = database.NewContext();
        var (repository, assets, _, _) = Services(db);
        var sync = new CatalogueSyncService(repository, assets, http, Options.Create(new MarketingOptions { CatalogueBaseUrl = "http://catalogue.test" }));

        var report = await sync.SyncAsync(default);
        var product = await repository.FindProductAsync("baska-v02", default);

        Assert.Equal(2, report.Received);
        Assert.Equal(["baska-v02"], report.NewProductIds);
        Assert.Contains(report.Warnings, w => w.StartsWith("broken"));
        Assert.Equal(180000m, product!.Price);
        Assert.Equal(130000m, product.WholesalePrice);
        Assert.Equal("Peplum", product.Copy!["en"].Name);
        Assert.Equal("catalogue:baska-v02:r3", product.SourceRevision);
        Assert.NotNull(await repository.FindAssetAsync(product.HeroAssetId, default));
    }

    [Fact]
    public async Task Blog_story_draft_links_the_story_and_is_remembered()
    {
        const string rss = """<?xml version="1.0"?><rss version="2.0"><channel><title>Ocharo Blog</title><item><title>Let one piece lead</title><link>/journal/let-one-piece-lead/</link><description>One statement piece.</description><category>Style</category><pubDate>Mon, 28 Sep 2026 09:00:00 GMT</pubDate></item></channel></rss>""";
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(rss, Encoding.UTF8, "application/rss+xml") }));
        await using var db = database.NewContext();
        var (repository, _, service, _) = Services(db);
        var blog = new BlogStoryService(repository, service, http, Options.Create(new MarketingOptions { BlogBaseUrl = "http://blog.test" }));

        var stories = await blog.ListAsync(default);
        var draft = await blog.DraftAsync(new StoryDraftInput(stories[0].Link, "nimcha", ["telegram"], "uz", "t02-bone"), default);
        var again = await blog.ListAsync(default);

        Assert.Equal("http://blog.test/journal/let-one-piece-lead/", stories[0].Link);
        Assert.Contains("O‘qish: http://blog.test/journal/let-one-piece-lead/", draft.Caption);
        Assert.Equal(draft.Id, again[0].DraftId);
    }

    private (MarketingRepository Repository, LocalAssetService Assets, MarketingService Service, AutomationService Automation) Services(OcharoMarketingDbContext db)
    {
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        var cards = new Presets();
        var service = new MarketingService(repository, assets, cards);
        return (repository, assets, service, new AutomationService(repository, service, cards, TimeProvider.System));
    }

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json"),
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(reply(request));
    }

    private sealed class Presets : ICardRenderingService
    {
        public IReadOnlyList<CardPreset> GetPresets() => [new("t02-bone", "Bone studio", "T02", "bone", "", false, 1080, 1350)];
        public CardPreview Compose(CardCompositionInput input) => throw new NotSupportedException();
        public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

/// <summary>Unattended passes: due checks, error recording and plans-only behaviour.</summary>
public sealed class AutomationRunnerTests : IAsyncLifetime
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII=");
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(Path.GetTempPath(), "ocharo-runner-tests", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        await assets.SaveAsync(Png, "front.png", "Test front", "front", default, "front");
        await repository.SeedAsync(new BrandModel("OCHARO", "Asia/Tashkent", null),
            [new ProductModel("nimcha", "Nimcha", "nimcha", "", "", "front", ["front"], [], null, "UZS", "review", "test", [])], default);
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public async Task Nothing_runs_until_enabled_then_failures_are_recorded_and_planning_still_runs()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        var service = new MarketingService(repository, assets, new NoCards());
        var automation = new AutomationService(repository, service, new NoCards(), TimeProvider.System);
        var offline = new HttpClient(new Offline());
        var runner = new AutomationRunner(new CatalogueSyncService(repository, assets, offline, Options.Create(new MarketingOptions { CatalogueBaseUrl = "http://offline.test" })),
            automation, repository, TimeProvider.System);
        var draft = await service.SaveDraftAsync(null, new DraftInput("Nimcha", "nimcha", "t02-bone", "uz", "Caption", ["telegram"]), default);
        await service.ApproveAsync(draft.Id, draft.Revision, default);

        var idle = await runner.RunDueAsync(default);
        await automation.SaveAsync(AutomationService.Default() with
        {
            Slots = [new PostingSlotModel("", "telegram", [1, 2, 3, 4, 5, 6, 7], "09:00", true)],
            AutoSyncHours = 6,
            AutoFillDays = 7,
        }, default);
        await runner.RunDueAsync(default);
        var status = await runner.StatusAsync(default);
        var schedules = await repository.GetSchedulesAsync(default);

        Assert.False(idle);
        Assert.Contains("Katalog", status.LastError);
        Assert.NotNull(status.LastSyncAt);
        Assert.Equal(1, status.LastFillPlanned);
        Assert.All(schedules, s => Assert.Equal("planned", s.Status));
    }

    private sealed class Offline : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }

    private sealed class NoCards : ICardRenderingService
    {
        public IReadOnlyList<CardPreset> GetPresets() => [new("t02-bone", "Bone studio", "T02", "bone", "", false, 1080, 1350)];
        public CardPreview Compose(CardCompositionInput input) => throw new NotSupportedException();
        public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
