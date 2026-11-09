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

namespace OcharoBlog.Tests;

public sealed class BrandKitTests
{
    [Fact]
    public async Task Published_brand_logo_and_links_replace_the_blog_settings_on_live_pages_and_export()
    {
        var logo = BlogHarness.Png(200);
        var sha = Convert.ToHexStringLower(SHA256.HashData(logo));
        var kit = new FakeBrandKit(new BrandSnapshot(4, sha, "http://brand.test/api/public/brand/media/" + sha,
            [new SiteLink("Instagram", "https://www.instagram.com/ocharostile/"), new SiteLink("Telegram", "https://t.me/Ocharo_Style")]), logo);
        await using var harness = new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        var archive = await client.GetStringAsync("/journal/");
        var status = await client.GetFromJsonAsync<JsonElement>("/api/blog/brand");
        using var export = new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/site.zip")));

        Assert.Contains($"/media/{sha}/preview", home);
        Assert.Contains($"/media/{sha}/preview", archive);
        Assert.Contains(">Telegram <svg", home);
        Assert.DoesNotContain("Contact on Telegram", home);
        Assert.Equal(1, kit.Downloads);
        Assert.Contains(export.Entries, e => e.FullName.Contains(sha, StringComparison.Ordinal));
        Assert.True(status.GetProperty("connected").GetBoolean());
        Assert.Equal(4, status.GetProperty("release").GetInt32());
    }

    [Fact]
    public async Task Active_event_badge_shows_on_live_pages_but_not_in_the_static_export()
    {
        var kit = new FakeBrandKit(new BrandSnapshot(5, null, null, [],
            new BrandEvent("Navro‘z", "Navro‘z collection", "https://t.me/Ocharo_Style", DateTimeOffset.UtcNow.AddYears(1))), null);
        await using var harness = new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        using var export = new ZipArchive(new MemoryStream(await client.GetByteArrayAsync("/api/blog/export/site.zip")));
        using var reader = new StreamReader(export.GetEntry("index.html")!.Open());
        var exported = await reader.ReadToEndAsync();

        Assert.Contains("class=\"event-badge\" href=\"https://t.me/Ocharo_Style\"", home);
        Assert.Contains("Navro‘z collection", home);
        Assert.DoesNotContain("event-badge", exported);
    }

    [Fact]
    public async Task Unreachable_brand_service_keeps_the_blog_settings()
    {
        var kit = new FakeBrandKit(null, null);
        await using var harness = new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        var status = await client.GetFromJsonAsync<JsonElement>("/api/blog/brand");

        Assert.Contains("Contact on Telegram", home);
        Assert.False(status.GetProperty("connected").GetBoolean());
        Assert.Equal(0, kit.Downloads);
    }

    [Fact]
    public async Task The_published_brand_logo_opens_anonymously_as_a_local_copy_without_files()
    {
        var logo = BlogHarness.Png(220);
        var sha = Convert.ToHexStringLower(SHA256.HashData(logo));
        var kit = new FakeBrandKit(new BrandSnapshot(6, sha, "http://brand.test/api/public/brand/media/" + sha, [], LogoFileId: FileId.New().ToString()), logo);
        await using var harness = new BlogHarness(services: s => s.Replace(ServiceDescriptor.Singleton<IBrandKit>(kit)));
        using var anonymous = harness.Anonymous();

        var home = await anonymous.GetStringAsync("/");
        using var original = await anonymous.GetAsync($"/media/{sha}");
        using var preview = await anonymous.GetAsync($"/media/{sha}/preview");

        Assert.Contains($"/media/{sha}/preview", home);
        Assert.Equal(1, kit.Downloads);
        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal(logo, await original.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal("image/webp", preview.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_brand_feed_gives_the_logos_files_id_and_none_before_files()
    {
        var sha = new string('c', 64);
        var file = FileId.New().ToString();

        var inFiles = await ReadAsync(Feed(sha, file));
        var local = await ReadAsync(Feed(sha, null));

        Assert.Equal(sha, inFiles?.LogoSha256);
        Assert.Equal($"https://brand.example.test/api/public/brand/media/{sha}", inFiles?.LogoUrl);
        Assert.Equal(file, inFiles?.LogoFileId);
        Assert.Equal(sha, local?.LogoSha256);
        Assert.Null(local?.LogoFileId);
    }

    /// <summary>Brand's public feed (<c>/api/public/brand</c>) as it names one on-light logo.</summary>
    private static object Feed(string sha, string? fileId) => new
    {
        release = 3,
        links = Array.Empty<object>(),
        logos = new Dictionary<string, object>
        {
            ["on-light"] = new { url = "/api/public/brand/logo/on-light", fileUrl = $"/api/public/brand/media/{sha}", sha256 = sha, fileId },
        },
    };

    private static Task<BrandSnapshot?> ReadAsync(object feed) =>
        new BrandKit(new HttpClient(new FeedHandler(feed)), Options.Create(new BlogOptions { BrandBaseUrl = "https://brand.example.test" }),
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
