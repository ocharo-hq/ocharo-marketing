using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

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
