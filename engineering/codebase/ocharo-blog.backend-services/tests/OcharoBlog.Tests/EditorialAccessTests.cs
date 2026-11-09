using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Npgsql;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Caching.Postgres;
using static OcharoBlog.Tests.EditorialSignIn;
using Xunit;

namespace OcharoBlog.Tests;

/// <summary>
/// Editorial signs in through Ocharo's Identity BFF: technical routes, the public API, the public site, public media and the
/// Studio's files answer anonymously; the rest of <c>/api</c>, previews and other media need a session of the bound
/// organization, writes <c>editorial:edit</c> and approval actions <c>editorial:approve</c>.
/// </summary>
public sealed class EditorialAccessTests
{
    private static readonly object Guide = new
    {
        slug = "a-wear-guide", topic = "style", publication = "wear", kind = "guide", title = "A considered occasion", excerpt = "A guide", intro = "Start here",
        occasions = new[] { "Everyday" }, sections = new[] { new { heading = "Try this", paragraphs = new[] { "Try the piece with what you already own." } } },
    };

    [Theory]
    [InlineData("/api/blog/articles")]
    [InlineData("/API/BLOG/ARTICLES")]
    [InlineData("/Api/Marketing/Workspace")]
    [InlineData("/API/BLOG/EXPORT/SITE.ZIP")]
    [InlineData("/api")]
    [InlineData("/api/public")]
    [InlineData("/api/publicity")]
    [InlineData("/api/system/status/details")]
    [InlineData("/preview")]
    [InlineData("/PREVIEW/123")]
    [InlineData("/preview/apps/atlas/gallery/")]
    public async Task PrivateRoutes_ShouldNeedASession(string path)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("sign_in_required", await CodeAsync(response));
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/HEALTH/")]
    [InlineData("/api/runtime-config")]
    [InlineData("/api/system/status")]
    public async Task TechnicalRoutes_ShouldAnswerAnonymously(string path)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task PublicApi_ShouldNeverAskForASession()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory);

        // No GET route lives there yet: the API's own 404, not the access middleware's 401.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/public/anything")).StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/journal/")]
    [InlineData("/journal/let-one-piece-lead/")]
    [InlineData("/topics/pieces/")]
    [InlineData("/atlas/")]
    [InlineData("/wear/")]
    [InlineData("/catalogue/")]
    [InlineData("/site.css")]
    [InlineData("/boot-splash.css")]
    [InlineData("/feed.xml")]
    [InlineData("/sitemap.xml")]
    [InlineData("/robots.txt")]
    public async Task PublicSite_ShouldAnswerAnonymously(string path)
    {
        await using var harness = new BlogHarness();
        using var client = harness.Anonymous();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Media_ShouldBePublicOnlyWhilePublishedContentUsesIt()
    {
        await using var harness = new BlogHarness();
        using var anonymous = harness.Anonymous();
        using var owner = harness.Client();
        using var viewer = Client(harness.Factory, Viewer);
        using var outsider = Client(harness.Factory, Outsider);

        var published = Regex.Matches(await anonymous.GetStringAsync("/"), "/media/[0-9a-f]{64}").Select(match => match.Value).Distinct().ToArray();
        Assert.NotEmpty(published);
        foreach (var path in published)
        {
            using var response = await anonymous.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl!.NoStore);
        }

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(BlogHarness.Png(200));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "draft.png");
        using var upload = await owner.PostAsync("/api/blog/media", form);
        upload.EnsureSuccessStatusCode();
        var draft = "/media/" + (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(draft)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(draft + "/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/media/" + new string('a', 64))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync(draft)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync(draft)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(draft)).StatusCode);
    }

    [Fact]
    public async Task Studio_ShouldLoadAnonymously()
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-blog-studio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "studio", "assets"));
        File.WriteAllText(Path.Combine(root, "studio", "index.html"), "<!doctype html><title>Studio</title>");
        File.WriteAllText(Path.Combine(root, "studio", "assets", "app.js"), "export {};");
        try
        {
            await using var host = EditorialHost.Create(new Dictionary<string, string?>(), webRoot: root);
            using var client = Anonymous(host.Factory);

            using var bare = await client.GetAsync("/studio");
            Assert.Equal(HttpStatusCode.MovedPermanently, bare.StatusCode);
            foreach (var path in new[] { "/studio/", "/studio/stories/123", "/studio/assets/app.js" })
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }

    [Fact]
    public async Task SessionRoute_ShouldNameThePersonAndHandOutTheCsrfToken()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var anonymous = Anonymous(host.Factory);
        using var owner = Client(host.Factory, Owner);
        using var plaintext = host.Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });

        Assert.False((await SessionAsync(anonymous)).GetProperty("isAuthenticated").GetBoolean());
        var session = await SessionAsync(owner);
        Assert.True(session.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(Owner.Subject, session.GetProperty("person").GetProperty("sub").GetString());
        Assert.Equal(Organization, session.GetProperty("organization").GetProperty("id").GetGuid());
        Assert.Equal(new string?[] { "editorial:edit", "editorial:approve" }, session.GetProperty("permissions").EnumerateArray().Select(permission => permission.GetString()));
        Assert.False(string.IsNullOrEmpty(session.GetProperty("csrfToken").GetString()));
        using var refused = await plaintext.GetAsync("/api/identity/session");
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("https_required", await CodeAsync(refused));
    }

    [Fact]
    public async Task Owner_ShouldReadWriteAndApprove()
    {
        await using var harness = new BlogHarness();
        using var owner = harness.Client();

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/blog/articles")).StatusCode);
        using var created = await owner.PostAsJsonAsync("/api/blog/articles", Guide);
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        (await owner.PostAsJsonAsync($"/api/blog/articles/{id}/publish", new { expectedRevision = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await harness.Anonymous().GetAsync("/wear/guides/a-wear-guide/")).StatusCode);
    }

    [Fact]
    public async Task Writes_ShouldNeedTheSessionsCsrfToken()
    {
        await using var harness = new BlogHarness(seed: false);
        using var forger = Client(harness.Factory, Owner, csrf: false);

        using var forged = await forger.PostAsJsonAsync("/api/blog/articles", Guide);
        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal("AntiforgeryValidationFailed", await CodeAsync(forged));
        Assert.True(forged.Headers.CacheControl!.NoStore);

        forger.DefaultRequestHeaders.Add("X-Csrf-Token", "not-a-token");
        using var wrong = await forger.PostAsJsonAsync("/api/blog/articles", Guide);
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("AntiforgeryValidationFailed", await CodeAsync(wrong));

        Assert.Equal(HttpStatusCode.OK, (await forger.GetAsync("/api/blog/articles")).StatusCode);
    }

    [Fact]
    public async Task Editor_ShouldWriteButNotChangeWhatIsPublished()
    {
        await using var harness = new BlogHarness();
        using var owner = harness.Client();
        using var editor = Client(harness.Factory, Editor);

        using var created = await editor.PostAsJsonAsync("/api/blog/articles", Guide);
        Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        using var refused = await editor.PostAsJsonAsync($"/api/blog/articles/{id}/publish", new { expectedRevision = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("approval_permission_required", await CodeAsync(refused));

        (await owner.PostAsJsonAsync($"/api/blog/articles/{id}/publish", new { expectedRevision = 1 })).EnsureSuccessStatusCode();
        // Ocharo.CanApprove still reaches the controller: an editor no longer changes the published story.
        Assert.Equal(HttpStatusCode.Forbidden, (await editor.PutAsJsonAsync($"/api/blog/articles/{id}", Guide)).StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/api/blog/articles/{id}", Guide)).StatusCode);
    }

    [Theory]
    [InlineData("publish")]
    [InlineData("unpublish")]
    [InlineData("archive")]
    [InlineData("schedule")]
    [InlineData("approve")]
    [InlineData("restore")]
    [InlineData("queue")]
    [InlineData("retry")]
    [InlineData("dispatch")]
    [InlineData("settings")]
    [InlineData("cancel")]
    [InlineData("reconcile")]
    [InlineData("PUBLISH")]
    [InlineData("publish/")]
    public async Task ApprovalPaths_ShouldNeedTheApprovePermission(string segment)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var editor = Client(host.Factory, Editor);
        using var owner = Client(host.Factory, Owner);
        var path = "/api/blog/example/" + segment;

        using var refused = await editor.PostAsJsonAsync(path, new { });
        using var passed = await owner.PostAsJsonAsync(path, new { });

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("approval_permission_required", await CodeAsync(refused));
        Assert.Equal(HttpStatusCode.NotFound, passed.StatusCode);
    }

    [Fact]
    public async Task Viewer_ShouldReadButNotWrite()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var viewer = Client(host.Factory, Viewer);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/blog/articles")).StatusCode);
        using var refused = await viewer.PostAsJsonAsync("/api/blog/articles", Guide);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("edit_permission_required", await CodeAsync(refused));
        using var approval = await viewer.PostAsJsonAsync("/api/blog/articles/abc/publish", new { });
        Assert.Equal("edit_permission_required", await CodeAsync(approval));
    }

    [Fact]
    public async Task AnotherOrganization_ShouldBeRefusedEverywherePrivate()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        foreach (var person in new[] { Outsider, Unaffiliated })
        {
            using var client = Client(host.Factory, person);

            using var read = await client.GetAsync("/api/blog/articles");
            using var write = await client.PostAsJsonAsync("/api/blog/articles", Guide);
            using var preview = await client.GetAsync("/preview/" + Guid.NewGuid());

            Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
            Assert.Equal("organization_access_required", await CodeAsync(read));
            Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
            Assert.Equal("organization_access_required", await CodeAsync(write));
            Assert.Equal(HttpStatusCode.Forbidden, preview.StatusCode);
            Assert.True((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/runtime-config")).StatusCode);
        }
    }

    [Theory]
    [InlineData("/api/integrations/session")]
    [InlineData("/api/integrations/connect")]
    public async Task RetiredSessionRoutes_ShouldNoLongerExist(string path)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var anonymous = Anonymous(host.Factory);
        using var owner = Client(host.Factory, Owner);

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example", true)]
    [InlineData("http://localhost", true)]
    [InlineData("https://user@localhost", true)]
    [InlineData("null", true)]
    [InlineData("https://localhost", false)]
    [InlineData("https://studio.example.test", false)]
    public async Task PrivateWrites_ShouldComeFromThisOriginOrATrustedOne(string origin, bool refused)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { ["Blog:TrustedOrigins:0"] = "https://studio.example.test" });
        using var owner = Client(host.Factory, Owner);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles") { Content = JsonContent.Create(Guide) };
        request.Headers.Add("Origin", origin);

        using var response = await owner.SendAsync(request);

        Assert.Equal(refused, await CodeAsync(response) == "cross_origin_write");
        Assert.Equal(refused, response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CrossSiteWrite_WithoutAnOrigin_ShouldBeRefused()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var owner = Client(host.Factory, Owner);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles") { Content = JsonContent.Create(Guide) };
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        using var response = await owner.SendAsync(request);

        Assert.Equal("cross_origin_write", await CodeAsync(response));
    }

    [Theory]
    [InlineData("/health", true)]
    [InlineData("/", false)]
    [InlineData("/api/runtime-config", true)]
    [InlineData("/api/public/anything", true)]
    [InlineData("/api/blog/articles", true)]
    [InlineData("/API/BLOG/ARTICLES", true)]
    [InlineData("/preview/123", true)]
    [InlineData("/media/missing", true)]
    public async Task ResponseHeaders_ShouldHoldOnEveryRoute(string path, bool noStore)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory);

        using var response = await client.GetAsync(path);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(noStore, response.Headers.CacheControl?.NoStore == true);
    }

    [Fact]
    public async Task RealCookieScheme_ShouldBootAndTreatAForgedCookieAsAnonymous()
    {
        const string body = "{\"reference\":\"receipt-42\"}";
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { ["Blog:MarketingBaseUrl"] = "https://receiver.test" },
            services: services => services.AddHttpClient("ocharo-inquiries").ConfigurePrimaryHttpMessageHandler(() => new AcceptingHandler(body)),
            realCookie: true);
        using var client = Anonymous(host.Factory);
        client.DefaultRequestHeaders.Add("Cookie", "__Host-ocharo-editorial=forged");

        Assert.False((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/blog/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/blog/articles", Guide)).StatusCode);
        using var enquiry = await client.PostAsJsonAsync("/api/public/inquiries", new { submissionId = "test" });
        Assert.Equal(HttpStatusCode.Accepted, enquiry.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task Host_ShouldRefuseToBoot_WithoutABoundOrganization(string? organization)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { ["Blog:OrganizationId"] = organization });

        var failure = Record.Exception(() => host.Factory.CreateClient());

        Assert.NotNull(failure);
        Assert.Contains("OrganizationId", failure.ToString());
    }

    [Theory]
    [InlineData("Blog:PublicAccess", "true")]
    [InlineData("PlatformIntegration:Mode", "local")]
    [InlineData("Identity:Sso:Enabled", "false")]
    [InlineData("DEPLOY_ENVIRONMENT", "prod")]
    public async Task RetiredSettings_ShouldNotOpenThePrivateApi(string key, string value)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { [key] = value });
        using var client = Anonymous(host.Factory);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/blog/articles")).StatusCode);
    }

    [Fact]
    public async Task KeyRingAndSessions_ShouldLiveInPostgreSql_SoARestartKeepsAnEditorSignedIn()
    {
        // Two hosts over one database stand for a restart: the second opens the cookie the first sealed and reads the
        // session the first stored, under the one application name every environment shares.
        var database = await BlogDatabase.CreateAsync();
        try
        {
            string sealedCookie;
            await using (var before = EditorialHost.Create(new Dictionary<string, string?>(), database: database))
            {
                using var client = Client(before.Factory, Owner);
                await SessionAsync(client);
                var services = before.Factory.Services;
                var sealer = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("restart");
                sealedCookie = sealer.Protect("session");
                await services.GetRequiredService<IDistributedCache>().SetStringAsync("restart-session", "signed-in");
            }

            await using var after = EditorialHost.Create(new Dictionary<string, string?>(), database: database);
            var restarted = after.Factory.Services;
            var protector = restarted.GetRequiredService<IDataProtectionProvider>().CreateProtector("restart");
            Assert.Equal("session", protector.Unprotect(sealedCookie));
            var sessions = restarted.GetRequiredService<IDistributedCache>();
            Assert.IsType<PostgresDistributedCache>(sessions);
            Assert.Equal("signed-in", await sessions.GetStringAsync("restart-session"));
            var protection = restarted.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
            Assert.Equal("ocharo-editorial", protection.ApplicationDiscriminator);
            Assert.False(Directory.Exists(Path.Combine(after.DataRoot, "keys")));
            await using var keys = new NpgsqlConnection(database);
            await keys.OpenAsync();
            await using var count = new NpgsqlCommand("SELECT COUNT(*) FROM blog.data_protection_keys", keys);
            Assert.True((long)(await count.ExecuteScalarAsync())! > 0);
        }
        finally { await BlogDatabase.DropAsync(database); }
    }

    [Fact]
    public async Task KeyRingName_ShouldFollowDataProtectionApplicationName_WhenSet()
    {
        await using var host = EditorialHost.Create(
            new Dictionary<string, string?> { ["DataProtection:ApplicationName"] = "editorial-x" });

        var protection = host.Factory.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        Assert.Equal("editorial-x", protection.ApplicationDiscriminator);
    }

    [Fact]
    public async Task SeedingAndScheduledPublishing_ShouldRunWithoutARequest()
    {
        await using var harness = new BlogHarness();
        using var anonymous = harness.Anonymous();
        // The seed ran at boot, before any request.
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/journal/let-one-piece-lead/")).StatusCode);

        using var created = await harness.Client().PostAsJsonAsync("/api/blog/articles", Guide);
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var publishAt = harness.Clock.GetUtcNow().AddHours(2);
        await harness.Get<ArticleService>().ScheduleAsync(id, 1, publishAt, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/wear/guides/a-wear-guide/")).StatusCode);
        harness.Clock.Advance(TimeSpan.FromHours(3));

        // The periodic job runs this pass every Blog:SchedulerSeconds; the harness turns the job off and runs it once.
        await harness.Get<ScheduledPublisher>().PublishDueAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/wear/guides/a-wear-guide/")).StatusCode);
    }

    private sealed class AcceptingHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent(body) });
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var document = JsonDocument.Parse(text);
        return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("code", out var code)
            ? code.GetString() : null;
    }
}
