using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;

namespace OcharoBlog.Tests.E2E;

public sealed class PublicSiteTests
{
    [Theory]
    [InlineData("/", "/boot-splash.css", "#f8f7f3")]
    [InlineData("/wear/", "/wear/boot-splash.css", "#faf5ed")]
    [InlineData("/catalogue/", "/catalogue/boot-splash.css", "#f6f0e5")]
    public async Task PublicDocuments_ShouldLoadTheInitialSurfaceFirst_WhenRendered(
        string path,
        string initialCss,
        string color)
    {
        await using var harness = new BlogHarness();
        using var client = harness.Client();
        var html = await client.GetStringAsync(path);
        var early = html.IndexOf(initialCss, StringComparison.Ordinal);
        var full = html.IndexOf("/site.css", StringComparison.Ordinal);
        (early >= 0 && early < full).Should().BeTrue();
        var css = await client.GetStringAsync(initialCss);
        css.Should().Contain(color);
        css.Should().Contain("color-scheme:light");
    }

    [Fact]
    public async Task Seed_ShouldFindImagesRelativeToTheContentRoot_WhenTheHostRelocates()
    {
        await using var harness = new BlogHarness(relativeSeedAssets: true);
        var client = harness.Client();
        var home = await client.GetStringAsync("/");
        var story = await client.GetStringAsync("/journal/let-one-piece-lead/");

        static string MediaPath(byte shade) => "/media/" + Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(BlogHarness.Png(shade)));
        home.Should().Contain(MediaPath(40));  // Logo.
        home.Should().Contain(MediaPath(100)); // Collection hero.
        story.Should().Contain(MediaPath(130)); // Story cover.
    }

    [Fact]
    public async Task PublicSite_ShouldRenderHomeArchiveTopicAndStory_WhenStoriesArePublished()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        var archive = await client.GetStringAsync("/journal/");
        var topic = await client.GetStringAsync("/topics/cloth/");
        var story = await client.GetStringAsync("/journal/the-nimcha-in-a-new-light/");
        var missing = await client.GetAsync("/journal/not-a-story/");
        var redirect = await client.GetAsync("/journal/the-nimcha-in-a-new-light");

        home.Should().Contain("Ocharo Blog");
        home.Should().Contain("Browse all 30 stories");
        home.Should().Contain("noindex,nofollow");
        archive.Should().Contain("30 stories");
        topic.Should().Contain("Cloth &amp; craft");
        story.Should().Contain("Why do we love clothes we never wear?");
        story.Should().Contain("Keep reading");
        home.Should().Contain("/media/");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        redirect.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Preview_ShouldRenderDraftsNoindexed_WhenThePublicSiteHidesThem()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        var created = await client.PostAsJsonAsync("/api/blog/articles", new
        {
            slug =
                "draft-story", topic = "style", title = "<script>alert(1)</script> Draft", excerpt = "x", intro = "y",
            sections = new[] { new { heading = "H", paragraphs = new[] { "P" } } },
        });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var hidden = await client.GetAsync("/journal/draft-story/");
        var preview = await client.GetStringAsync($"/preview/{id}");

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);
        preview.Should().Contain("Preview — this page is not public");
        preview.Should().Contain("&lt;script&gt;");
        preview.Should().NotContain("<script>alert");
    }

    [Fact]
    public async Task FeedSitemapAndRobots_ShouldFollowTheSettings_WhenRendered()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var feed = XDocument.Parse(await client.GetStringAsync("/feed.xml"));
        var robotsBefore = await client.GetStringAsync("/robots.txt");
        var settings = await client.GetFromJsonAsync<JsonElement>("/api/blog/settings");
        var changed =
            JsonSerializer.Deserialize<Dictionary<string, object?>>(settings.GetProperty("settings").GetRawText())!;
        changed["noIndex"] = false;
        changed["baseUrl"] = "https://blog.ocharo.example";
        var saved =
            await client.PutAsJsonAsync(
                "/api/blog/settings",
                new { settings = changed, expectedRevision = settings.GetProperty("revision").GetInt32() });
        var robotsAfter = await client.GetStringAsync("/robots.txt");
        var sitemap = await client.GetStringAsync("/sitemap.xml");
        var home = await client.GetStringAsync("/");

        feed.Descendants("item").Count().Should().Be(30);
        robotsBefore.Should().Contain("Disallow: /");
        saved.StatusCode.Should().Be(HttpStatusCode.OK);
        robotsAfter.Should().Contain("Sitemap: https://blog.ocharo.example/sitemap.xml");
        sitemap.Should().Contain("https://blog.ocharo.example/journal/the-nimcha-in-a-new-light/");
        home.Should().NotContain("noindex");
    }

    [Fact]
    public async Task SiteExport_ShouldHoldEveryPageAndImage_WhenExported()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var bytes = await client.GetByteArrayAsync("/api/blog/export/site.zip");
        using var zip = new ZipArchive(new MemoryStream(bytes));
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();

        names.Should().Contain("index.html");
        names.Should().Contain("journal/index.html");
        names.Should().Contain("topics/pieces/index.html");
        names.Should().Contain("journal/let-one-piece-lead/index.html");
        names.Should().Contain("boot-splash.css");
        names.Should().Contain("site.css");
        names.Should().Contain("feed.xml");
        (names.Count(n => n.StartsWith("media/", StringComparison.Ordinal)) >= 3).Should().BeTrue();
        using var home = new StreamReader(zip.GetEntry("index.html")!.Open());
        (await home.ReadToEndAsync()).Should().Contain("/media/");
    }

    [Fact]
    public async Task Write_ShouldReturn403_WhenItIsCrossOrigin()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles")
            { Content = JsonContent.Create(new { slug = "x" }) };
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
