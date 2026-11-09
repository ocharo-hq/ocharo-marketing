using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;

namespace OcharoBlog.Tests.E2E;

public sealed class StudioHostingTests
{
    [Fact]
    public async Task Framing_ShouldAllowOnlyThisOriginForPreviews_WhenPagesAreServed()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        var id = (await client.GetFromJsonAsync<JsonElement>("/api/blog/articles"))[0].GetProperty("id").GetGuid();

        using var preview = await client.GetAsync($"/preview/{id}");
        using var page = await client.GetAsync("/");

        preview.StatusCode.Should().Be(HttpStatusCode.OK);
        preview.Headers.GetValues("X-Frame-Options").Single().Should().Be("SAMEORIGIN");
        preview.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-ancestors 'self'");
        page.Headers.GetValues("X-Frame-Options").Single().Should().Be("DENY");
        page.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task StudioPath_ShouldRedirectOnceWithoutLooping_WhenTheSlashIsMissing()
    {
        using var studio = new StudioBundle();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: studio.Root);
        using var client = host.Client();

        var bare = await client.GetAsync("/studio");
        var slash = await client.GetAsync("/studio/");

        bare.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        (bare.Headers.Location?.OriginalString).Should().EndWith("/studio/");
        slash.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StudioAsset_ShouldReturn404_WhenTheAssetIsMissing()
    {
        using var studio = new StudioBundle();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: studio.Root);
        using var client = host.Client();

        var missingAsset = await client.GetAsync("/studio/assets/missing-file.js");
        var deepLink = await client.GetAsync("/studio/stories/123");

        missingAsset.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (missingAsset.Content.Headers.ContentType?.MediaType).Should().NotBe("text/html");
        deepLink.StatusCode.Should().Be(HttpStatusCode.OK);
        (deepLink.Content.Headers.ContentType?.MediaType).Should().Be("text/html");
    }

    [Theory]
    [InlineData("/no-such-page")]
    [InlineData("/journal/a/b")]
    [InlineData("/studios")]
    public async Task PublicMiss_ShouldReturn404_WhenNoPageMatches(string path)
    {
        using var studio = new StudioBundle();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: studio.Root);
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("application/problem+json");
    }

    [Fact]
    public async Task StudioRoute_ShouldServeTheShell_WhenNoDocumentMatches()
    {
        using var studio = new StudioBundle();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: studio.Root);
        using var client = host.Client();

        using var shell = await client.GetAsync("/studio/");
        using var response = await client.GetAsync("/studio/x");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("text/html");
        (await response.Content.ReadAsStringAsync()).Should().Be(await shell.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task StudioAsset_ShouldCacheAsImmutable_WhenItIsUnderTheStudioAssets()
    {
        using var studio = new StudioBundle();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: studio.Root);
        using var client = host.Client();

        using var response = await client.GetAsync("/studio/assets/app.js");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.ToString().Should().Contain("immutable");
    }

    [Fact]
    public async Task PublicStory_ShouldReturn200_WhenTheStudioIsServedUnderItsPrefix()
    {
        await using var harness = new BlogHarness();
        using var client = harness.Anonymous();

        using var response = await client.GetAsync("/journal/the-nimcha-in-a-new-light/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("text/html");
    }

    [Fact]
    public async Task UnknownApiRoute_ShouldReturn401_WhenNoSessionComes()
    {
        using var studio = new StudioBundle();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: studio.Root);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/x");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>A built Studio in a temporary web root: its shell and one hashed asset.</summary>
    private sealed class StudioBundle : IDisposable
    {
        public string Root { get; } =
            Path.Combine(Path.GetTempPath(), "ocharo-blog-studio", Guid.NewGuid().ToString("N"));

        public StudioBundle()
        {
            Directory.CreateDirectory(Path.Combine(Root, "studio", "assets"));
            File.WriteAllText(Path.Combine(Root, "studio", "index.html"), "<!doctype html><title>Studio</title>");
            File.WriteAllText(Path.Combine(Root, "studio", "assets", "app.js"), "export {};");
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, true); } catch (IOException) { }
        }
    }
}
