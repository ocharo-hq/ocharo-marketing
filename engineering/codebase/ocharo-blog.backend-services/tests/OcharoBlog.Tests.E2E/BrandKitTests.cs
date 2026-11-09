using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Files.Store;

namespace OcharoBlog.Tests.E2E;

public sealed class BrandKitTests
{
    [Fact]
    public async Task BrandKit_ShouldReplaceTheBlogLogoAndLinks_WhenBrandPublishesThem()
    {
        var logo = BlogHarness.Png(200);
        var sha = Convert.ToHexStringLower(SHA256.HashData(logo));
        var kit = new FakeBrandKit(new BrandSnapshot(4, sha, "http://brand.test/api/public/brand/media/" + sha,
            [
                new SiteLink("Instagram", "https://www.instagram.com/ocharostile/"),
                new SiteLink("Telegram", "https://t.me/Ocharo_Style")]), logo);
        await using var harness =
            new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        var archive = await client.GetStringAsync("/journal/");
        var status = await client.GetFromJsonAsync<JsonElement>("/api/blog/brand");
        using var export =
            new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/site.zip")));

        home.Should().Contain($"/media/{sha}/preview");
        archive.Should().Contain($"/media/{sha}/preview");
        home.Should().Contain(">Telegram <svg");
        home.Should().NotContain("Contact on Telegram");
        kit.Downloads.Should().Be(1);
        export.Entries.Should().Contain(e => e.FullName.Contains(sha, StringComparison.Ordinal));
        status.GetProperty("connected").GetBoolean().Should().BeTrue();
        status.GetProperty("release").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task EventBadge_ShouldShowOnLivePagesOnly_WhenAnEventIsActive()
    {
        var kit = new FakeBrandKit(new BrandSnapshot(5, null, null, [],
            new BrandEvent(
                "Navro‘z",
                "Navro‘z collection",
                "https://t.me/Ocharo_Style",
                BlogHarness.Start.AddYears(1))), null);
        await using var harness =
            new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        using var export =
            new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/site.zip")));
        using var reader = new StreamReader(export.GetEntry("index.html")!.Open());
        var exported = await reader.ReadToEndAsync();

        home.Should().Contain("class=\"event-badge\" href=\"https://t.me/Ocharo_Style\"");
        home.Should().Contain("Navro‘z collection");
        exported.Should().NotContain("event-badge");
    }

    [Fact]
    public async Task BrandKit_ShouldKeepTheBlogSettings_WhenBrandIsUnreachable()
    {
        var kit = new FakeBrandKit(null, null);
        await using var harness =
            new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        var status = await client.GetFromJsonAsync<JsonElement>("/api/blog/brand");

        home.Should().Contain("Contact on Telegram");
        status.GetProperty("connected").GetBoolean().Should().BeFalse();
        kit.Downloads.Should().Be(0);
    }

    [Fact]
    public async Task BrandLogo_ShouldOpenAnonymouslyAsALocalCopy_WhenFilesIsOff()
    {
        var logo = BlogHarness.Png(220);
        var sha = Convert.ToHexStringLower(SHA256.HashData(logo));
        var kit =
            new FakeBrandKit(
                new BrandSnapshot(
                    6,
                    sha,
                    "http://brand.test/api/public/brand/media/" + sha,
                    [],
                    LogoFileId: FileId.New().ToString()),
                logo);
        await using var harness =
            new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        using var anonymous = harness.Anonymous();

        var home = await anonymous.GetStringAsync("/");
        using var original = await anonymous.GetAsync($"/media/{sha}");
        using var preview = await anonymous.GetAsync($"/media/{sha}/preview");

        home.Should().Contain($"/media/{sha}/preview");
        kit.Downloads.Should().Be(1);
        original.StatusCode.Should().Be(HttpStatusCode.OK);
        (await original.Content.ReadAsByteArrayAsync()).Should().Equal(logo);
        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        (preview.Content.Headers.ContentType?.MediaType).Should().Be("image/webp");
    }

    [Fact]
    public async Task BrandFeed_ShouldGiveTheLogoFileId_WhenBrandKeepsItInFiles()
    {
        var sha = new string('c', 64);
        var file = FileId.New().ToString();

        var inFiles = await ReadAsync(Feed(sha, file));
        var local = await ReadAsync(Feed(sha, null));

        (inFiles?.LogoSha256).Should().Be(sha);
        (inFiles?.LogoUrl).Should().Be($"https://brand.example.test/api/public/brand/media/{sha}");
        (inFiles?.LogoFileId).Should().Be(file);
        (local?.LogoSha256).Should().Be(sha);
        (local?.LogoFileId).Should().BeNull();
    }

    /// <summary>Brand's public feed (<c>/api/public/brand</c>) as it names one on-light logo.</summary>
    private static object Feed(string sha, string? fileId) => new
    {
        release = 3,
        links = Array.Empty<object>(),
        logos = new Dictionary<string, object>
        {
            ["on-light"] = new
            {
                url = "/api/public/brand/logo/on-light",
                fileUrl = $"/api/public/brand/media/{sha}",
                sha256 = sha,
                fileId
            },
        },
    };

    private static Task<BrandSnapshot?> ReadAsync(object feed) =>
        new BrandKit(new HttpClient(new FeedHandler(feed)), Options.Create(new BlogOptions
            {
                BrandBaseUrl = "https://brand.example.test"
            }),
            NullLogger<BrandKit>.Instance).GetAsync(default);

    private sealed class FeedHandler(object payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(payload) });
    }

    private sealed class FakeBrandKit(BrandSnapshot? snapshot, byte[]? logo) : IBrandKit
    {
        public int Downloads { get; private set; }

        public Task<BrandSnapshot?> GetAsync(CancellationToken ct) => Task.FromResult(snapshot);

        public Task<byte[]?> DownloadAsync(string url, CancellationToken ct)
        {
            Downloads++;
            return Task.FromResult(logo);
        }
    }
}
