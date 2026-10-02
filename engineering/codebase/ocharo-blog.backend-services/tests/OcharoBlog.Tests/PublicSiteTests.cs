using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;

namespace OcharoBlog.Tests;

public sealed class PublicSiteTests
{
    [Theory]
    [InlineData("/", "/boot-splash.css", "#f8f7f3")]
    [InlineData("/wear/", "/wear/boot-splash.css", "#faf5ed")]
    [InlineData("/catalogue/", "/catalogue/boot-splash.css", "#f6f0e5")]
    public async Task PublicDocumentsLoadTheirExternalInitialSurfaceBeforeTheFullTheme(string path, string initialCss, string color)
    {
        await using var harness = new BlogHarness();
        using var client = harness.Client();
        var html = await client.GetStringAsync(path);
        var early = html.IndexOf(initialCss, StringComparison.Ordinal);
        var full = html.IndexOf("/site.css", StringComparison.Ordinal);
        Assert.True(early >= 0 && early < full);
        var css = await client.GetStringAsync(initialCss);
        Assert.Contains(color, css);
        Assert.Contains("color-scheme:light", css);
    }

    [Fact]
    public async Task Seeds_images_relative_to_api_content_root_after_relocation()
    {
        await using var harness = new BlogHarness(relativeSeedAssets: true);
        var client = harness.Client();
        var home = await client.GetStringAsync("/");
        var story = await client.GetStringAsync("/journal/let-one-piece-lead/");

        static string MediaPath(byte shade) => "/media/" + Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(BlogHarness.Png(shade)));
        Assert.Contains(MediaPath(40), home);  // Logo.
        Assert.Contains(MediaPath(100), home); // Collection hero.
        Assert.Contains(MediaPath(130), story); // Story cover.
    }

    [Fact]
    public async Task Renders_home_archive_topic_and_story_from_live_data()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var home = await client.GetStringAsync("/");
        var archive = await client.GetStringAsync("/journal/");
        var topic = await client.GetStringAsync("/topics/cloth/");
        var story = await client.GetStringAsync("/journal/the-nimcha-in-a-new-light/");
        var missing = await client.GetAsync("/journal/not-a-story/");
        var redirect = await client.GetAsync("/journal/the-nimcha-in-a-new-light");

        Assert.Contains("Ocharo Blog", home);
        Assert.Contains("Browse all 30 stories", home);
        Assert.Contains("noindex,nofollow", home);
        Assert.Contains("30 stories", archive);
        Assert.Contains("Cloth &amp; craft", topic);
        Assert.Contains("Why do we love clothes we never wear?", story);
        Assert.Contains("Keep reading", story);
        Assert.Contains("/media/", home);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.MovedPermanently, redirect.StatusCode);
    }

    [Fact]
    public async Task Drafts_stay_private_but_preview_renders_them_noindexed_and_encoded()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        var created = await client.PostAsJsonAsync("/api/blog/articles", new
        {
            slug = "draft-story", topic = "style", title = "<script>alert(1)</script> Draft", excerpt = "x", intro = "y",
            sections = new[] { new { heading = "H", paragraphs = new[] { "P" } } },
        });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var hidden = await client.GetAsync("/journal/draft-story/");
        var preview = await client.GetStringAsync($"/preview/{id}");

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Contains("Preview — this page is not public", preview);
        Assert.Contains("&lt;script&gt;", preview);
        Assert.DoesNotContain("<script>alert", preview);
    }

    [Fact]
    public async Task Feed_sitemap_and_robots_follow_settings()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var feed = XDocument.Parse(await client.GetStringAsync("/feed.xml"));
        var robotsBefore = await client.GetStringAsync("/robots.txt");
        var settings = await client.GetFromJsonAsync<JsonElement>("/api/blog/settings");
        var changed = JsonSerializer.Deserialize<Dictionary<string, object?>>(settings.GetProperty("settings").GetRawText())!;
        changed["noIndex"] = false;
        changed["baseUrl"] = "https://blog.ocharo.example";
        var saved = await client.PutAsJsonAsync("/api/blog/settings", new { settings = changed, expectedRevision = settings.GetProperty("revision").GetInt32() });
        var robotsAfter = await client.GetStringAsync("/robots.txt");
        var sitemap = await client.GetStringAsync("/sitemap.xml");
        var home = await client.GetStringAsync("/");

        Assert.Equal(30, feed.Descendants("item").Count());
        Assert.Contains("Disallow: /", robotsBefore);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Contains("Sitemap: https://blog.ocharo.example/sitemap.xml", robotsAfter);
        Assert.Contains("https://blog.ocharo.example/journal/the-nimcha-in-a-new-light/", sitemap);
        Assert.DoesNotContain("noindex", home);
    }

    [Fact]
    public async Task Static_export_contains_every_page_and_referenced_image()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var bytes = await client.GetByteArrayAsync("/api/blog/export/site.zip");
        using var zip = new ZipArchive(new MemoryStream(bytes));
        var names = zip.Entries.Select(e => e.FullName).ToHashSet();

        Assert.Contains("index.html", names);
        Assert.Contains("journal/index.html", names);
        Assert.Contains("topics/pieces/index.html", names);
        Assert.Contains("journal/let-one-piece-lead/index.html", names);
        Assert.Contains("boot-splash.css", names);
        Assert.Contains("site.css", names);
        Assert.Contains("feed.xml", names);
        Assert.True(names.Count(n => n.StartsWith("media/", StringComparison.Ordinal)) >= 3);
        using var home = new StreamReader(zip.GetEntry("index.html")!.Open());
        Assert.Contains("/media/", await home.ReadToEndAsync());
    }

    [Fact]
    public async Task Rejects_cross_origin_writes()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles") { Content = JsonContent.Create(new { slug = "x" }) };
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed class StudioHostingTests
{
    [Fact]
    public async Task Bare_studio_path_redirects_once_and_the_slash_path_does_not_loop()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var bare = await client.GetAsync("/studio");
        var slash = await client.GetAsync("/studio/");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, bare.StatusCode);
        Assert.Equal("/studio/", bare.Headers.Location?.ToString());
        Assert.NotEqual(System.Net.HttpStatusCode.Redirect, slash.StatusCode);
    }

    [Fact]
    public async Task Studio_asset_paths_never_fall_back_to_the_spa_shell()
    {
        await using var harness = new BlogHarness();
        var client = harness.Client();

        var missingAsset = await client.GetAsync("/studio/assets/missing-file.js");
        var deepLink = await client.GetAsync("/studio/stories/123");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, missingAsset.StatusCode);
        Assert.NotEqual("text/html", missingAsset.Content.Headers.ContentType?.MediaType);
        Assert.True(deepLink.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.NotFound);
    }
}
