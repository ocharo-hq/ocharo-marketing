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
using static OcharoBlog.Tests.Support.EditorialSignIn;
using Xunit;

namespace OcharoBlog.Tests.E2E;

/// <summary>
/// Editorial signs in through Ocharo's Identity BFF: technical routes, the public API, the public site, public media
/// and the
/// Studio's files answer anonymously; the rest of <c>/api</c>, previews and other media need a session of the bound
/// organization, writes <c>editorial:edit</c> and approval actions <c>editorial:approve</c>.
/// </summary>
public sealed class EditorialAccessTests
{
    private static readonly object Guide = new
    {
        slug =
            "a-wear-guide", topic =
                "style", publication =
                    "wear", kind = "guide", title = "A considered occasion", excerpt = "A guide", intro = "Start here",
        occasions = new[]
        {
            "Everyday"
        }, sections = new[]
        {
            new { heading = "Try this", paragraphs = new[] { "Try the piece with what you already own." } }
        },
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

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodeAsync(response)).Should().Be("sign_in_required");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
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

        ((await client.GetAsync(path)).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicApi_ShouldNeverAskForASession()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory);

        // No GET route lives there yet: the API's own 404, not the access middleware's 401.
        ((await client.GetAsync("/api/public/anything")).StatusCode).Should().Be(HttpStatusCode.NotFound);
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

        ((await client.GetAsync(path)).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Media_ShouldBePublicOnlyWhilePublishedContentUsesIt()
    {
        await using var harness = new BlogHarness();
        using var anonymous = harness.Anonymous();
        using var owner = harness.Client();
        using var viewer = Client(harness.Factory, Viewer);
        using var outsider = Client(harness.Factory, Outsider);

        var published =
            Regex.Matches(await anonymous.GetStringAsync("/"), "/media/[0-9a-f]{64}")
                .Select(match => match.Value)
                .Distinct()
                .ToArray();
        published.Should().NotBeEmpty();
        foreach (var path in published)
        {
            using var response = await anonymous.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.CacheControl!.NoStore.Should().BeTrue();
        }

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(BlogHarness.Png(200));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "draft.png");
        using var upload = await owner.PostAsync("/api/blog/media", form);
        upload.EnsureSuccessStatusCode();
        var draft =
            "/media/" + (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString();

        ((await anonymous.GetAsync(draft)).StatusCode).Should().Be(HttpStatusCode.Unauthorized);
        ((await anonymous.GetAsync(draft + "/preview")).StatusCode).Should().Be(HttpStatusCode.Unauthorized);
        ((await anonymous.GetAsync("/media/" + new string('a', 64))).StatusCode)
            .Should()
            .Be(HttpStatusCode.Unauthorized);
        ((await outsider.GetAsync(draft)).StatusCode).Should().Be(HttpStatusCode.Forbidden);
        ((await viewer.GetAsync(draft)).StatusCode).Should().Be(HttpStatusCode.OK);
        ((await owner.GetAsync(draft)).StatusCode).Should().Be(HttpStatusCode.OK);
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
            bare.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
            foreach (var path in new[] { "/studio/", "/studio/stories/123", "/studio/assets/app.js" })
                ((await client.GetAsync(path)).StatusCode).Should().Be(HttpStatusCode.OK);
        }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }

    [Fact]
    public async Task SessionRoute_ShouldNameThePersonAndHandOutTheCsrfToken()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var anonymous = Anonymous(host.Factory);
        using var owner = Client(host.Factory, Owner);
        using var plaintext =
            host.Factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("http://localhost")
                });

        ((await SessionAsync(anonymous)).GetProperty("isAuthenticated").GetBoolean()).Should().BeFalse();
        var session = await SessionAsync(owner);
        session.GetProperty("isAuthenticated").GetBoolean().Should().BeTrue();
        session.GetProperty("person").GetProperty("sub").GetString().Should().Be(Owner.Subject);
        session.GetProperty("organization").GetProperty("id").GetGuid().Should().Be(Organization);
        session.GetProperty("permissions")
            .EnumerateArray()
            .Select(permission => permission.GetString())
            .Should()
            .Equal(new string?[] { "editorial:edit", "editorial:approve" });
        string.IsNullOrEmpty(session.GetProperty("csrfToken").GetString()).Should().BeFalse();
        using var refused = await plaintext.GetAsync("/api/identity/session");
        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(refused)).Should().Be("https_required");
    }

    [Fact]
    public async Task Owner_ShouldReadWriteAndApprove()
    {
        await using var harness = new BlogHarness();
        using var owner = harness.Client();

        ((await owner.GetAsync("/api/blog/articles")).StatusCode).Should().Be(HttpStatusCode.OK);
        using var created = await owner.PostAsJsonAsync("/api/blog/articles", Guide);
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        (await owner.PostAsJsonAsync($"/api/blog/articles/{id}/publish", new { expectedRevision = 1 }))
            .EnsureSuccessStatusCode();
        ((await harness.Anonymous().GetAsync("/wear/guides/a-wear-guide/")).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Writes_ShouldNeedTheSessionsCsrfToken()
    {
        await using var harness = new BlogHarness(seed: false);
        using var forger = Client(harness.Factory, Owner, csrf: false);

        using var forged = await forger.PostAsJsonAsync("/api/blog/articles", Guide);
        forged.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(forged)).Should().Be("AntiforgeryValidationFailed");
        forged.Headers.CacheControl!.NoStore.Should().BeTrue();

        forger.DefaultRequestHeaders.Add("X-Csrf-Token", "not-a-token");
        using var wrong = await forger.PostAsJsonAsync("/api/blog/articles", Guide);
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(wrong)).Should().Be("AntiforgeryValidationFailed");

        ((await forger.GetAsync("/api/blog/articles")).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Editor_ShouldWriteButNotChangeWhatIsPublished()
    {
        await using var harness = new BlogHarness();
        using var owner = harness.Client();
        using var editor = Client(harness.Factory, Editor);

        using var created = await editor.PostAsJsonAsync("/api/blog/articles", Guide);
        created.IsSuccessStatusCode.Should().BeTrue(await created.Content.ReadAsStringAsync());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        using var refused =
            await editor.PostAsJsonAsync($"/api/blog/articles/{id}/publish", new { expectedRevision = 1 });
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeAsync(refused)).Should().Be("approval_permission_required");

        (await owner.PostAsJsonAsync($"/api/blog/articles/{id}/publish", new { expectedRevision = 1 }))
            .EnsureSuccessStatusCode();
        // Ocharo.CanApprove still reaches the controller: an editor no longer changes the published story.
        ((await editor.PutAsJsonAsync($"/api/blog/articles/{id}", Guide)).StatusCode)
            .Should()
            .Be(HttpStatusCode.Forbidden);
        ((await owner.PutAsJsonAsync($"/api/blog/articles/{id}", Guide)).StatusCode)
            .Should()
            .NotBe(HttpStatusCode.Forbidden);
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

        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeAsync(refused)).Should().Be("approval_permission_required");
        passed.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Viewer_ShouldReadButNotWrite()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var viewer = Client(host.Factory, Viewer);

        ((await viewer.GetAsync("/api/blog/articles")).StatusCode).Should().Be(HttpStatusCode.OK);
        using var refused = await viewer.PostAsJsonAsync("/api/blog/articles", Guide);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeAsync(refused)).Should().Be("edit_permission_required");
        using var approval = await viewer.PostAsJsonAsync("/api/blog/articles/abc/publish", new { });
        (await CodeAsync(approval)).Should().Be("edit_permission_required");
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

            read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeAsync(read)).Should().Be("organization_access_required");
            write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeAsync(write)).Should().Be("organization_access_required");
            preview.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean()).Should().BeTrue();
            ((await client.GetAsync("/api/runtime-config")).StatusCode).Should().Be(HttpStatusCode.OK);
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

        ((await anonymous.GetAsync(path)).StatusCode).Should().Be(HttpStatusCode.Unauthorized);
        ((await owner.GetAsync(path)).StatusCode).Should().Be(HttpStatusCode.NotFound);
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
        await using var host =
            EditorialHost.Create(new Dictionary<string, string?>
                {
                    ["Blog:TrustedOrigins:0"] = "https://studio.example.test"
                });
        using var owner = Client(host.Factory, Owner);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles")
            { Content = JsonContent.Create(Guide) };
        request.Headers.Add("Origin", origin);

        using var response = await owner.SendAsync(request);

        (await CodeAsync(response) == "cross_origin_write").Should().Be(refused);
        (response.StatusCode == HttpStatusCode.Forbidden).Should().Be(refused);
    }

    [Fact]
    public async Task CrossSiteWrite_ShouldBeRefusedAsCrossOrigin_WhenItCarriesNoOrigin()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var owner = Client(host.Factory, Owner);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles")
            { Content = JsonContent.Create(Guide) };
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        using var response = await owner.SendAsync(request);

        (await CodeAsync(response)).Should().Be("cross_origin_write");
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

        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        (response.Headers.CacheControl?.NoStore == true).Should().Be(noStore);
    }

    [Fact]
    public async Task RealCookieScheme_ShouldBootAndTreatAForgedCookieAsAnonymous()
    {
        const string body = "{\"reference\":\"receipt-42\"}";
        await using var host = EditorialHost.Create(new Dictionary<string, string?>
        {
            ["Blog:MarketingBaseUrl"] = "https://receiver.test"
        },
            services: services =>
                services.AddHttpClient("ocharo-inquiries")
                    .ConfigurePrimaryHttpMessageHandler(() => new AcceptingHandler(body)),
            realCookie: true);
        using var client = Anonymous(host.Factory);
        client.DefaultRequestHeaders.Add("Cookie", "__Host-ocharo-editorial=forged");

        ((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean()).Should().BeFalse();
        ((await client.GetAsync("/api/blog/articles")).StatusCode).Should().Be(HttpStatusCode.Unauthorized);
        ((await client.PostAsJsonAsync("/api/blog/articles", Guide)).StatusCode)
            .Should()
            .Be(HttpStatusCode.Unauthorized);
        using var enquiry = await client.PostAsJsonAsync("/api/public/inquiries", new { submissionId = "test" });
        enquiry.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task Host_ShouldRefuseToBoot_WhenNoOrganizationIsBound(string? organization)
    {
        await using var host =
            EditorialHost.Create(new Dictionary<string, string?> { ["Blog:OrganizationId"] = organization });

        var failure = Record.Exception(() => host.Factory.CreateClient());

        failure.Should().NotBeNull();
        failure.ToString().Should().Contain("OrganizationId");
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

        ((await client.GetAsync("/api/blog/articles")).StatusCode).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task KeyRingAndSessions_ShouldKeepAnEditorSignedIn_WhenTheHostRestarts()
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
            protector.Unprotect(sealedCookie).Should().Be("session");
            var sessions = restarted.GetRequiredService<IDistributedCache>();
            sessions.Should().BeOfType<PostgresDistributedCache>();
            (await sessions.GetStringAsync("restart-session")).Should().Be("signed-in");
            var protection = restarted.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
            protection.ApplicationDiscriminator.Should().Be("ocharo-editorial");
            Directory.Exists(Path.Combine(after.DataRoot, "keys")).Should().BeFalse();
            await using var keys = new NpgsqlConnection(database);
            await keys.OpenAsync();
            await using var count = new NpgsqlCommand("SELECT COUNT(*) FROM blog.data_protection_keys", keys);
            ((long)(await count.ExecuteScalarAsync())! > 0).Should().BeTrue();
        }
        finally { await BlogDatabase.DropAsync(database); }
    }

    [Fact]
    public async Task KeyRingName_ShouldFollowDataProtectionApplicationName_WhenSet()
    {
        await using var host = EditorialHost.Create(
            new Dictionary<string, string?> { ["DataProtection:ApplicationName"] = "editorial-x" });

        var protection = host.Factory.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        protection.ApplicationDiscriminator.Should().Be("editorial-x");
    }

    [Fact]
    public async Task SeedingAndScheduledPublishing_ShouldRunWithoutARequest()
    {
        await using var harness = new BlogHarness();
        using var anonymous = harness.Anonymous();
        // The seed ran at boot, before any request.
        ((await anonymous.GetAsync("/journal/let-one-piece-lead/")).StatusCode).Should().Be(HttpStatusCode.OK);

        using var created = await harness.Client().PostAsJsonAsync("/api/blog/articles", Guide);
        created.EnsureSuccessStatusCode();
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var publishAt = harness.Clock.GetUtcNow().AddHours(2);
        await harness.Get<ArticleService>().ScheduleAsync(id, 1, publishAt, CancellationToken.None);
        ((await anonymous.GetAsync("/wear/guides/a-wear-guide/")).StatusCode).Should().Be(HttpStatusCode.NotFound);
        harness.Clock.Advance(TimeSpan.FromHours(3));

        // The periodic job runs this pass every Blog:SchedulerSeconds; the harness turns the job off and runs it once.
        await harness.Get<ScheduledPublisher>().PublishDueAsync(CancellationToken.None);

        ((await anonymous.GetAsync("/wear/guides/a-wear-guide/")).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    private sealed class AcceptingHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent(body) });
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return null;
        using var document = JsonDocument.Parse(text);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("code", out var code)
            ? code.GetString() : null;
    }
}
