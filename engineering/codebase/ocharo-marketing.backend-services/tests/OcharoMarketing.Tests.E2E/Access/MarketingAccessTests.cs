using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AwesomeAssertions;
using OcharoMarketing.Persistence.Access;
using WoW.Two.Sdk.Beta.Be.Caching.Postgres;
using Xunit;
using static OcharoMarketing.Tests.Support.MarketingTestIdentity;

namespace OcharoMarketing.Tests.E2E.Access;

/// <summary>
/// Marketing signs in through Ocharo's Identity BFF: technical and <c>/api/public/</c> routes answer anonymously, the
/// inquiry form with or without a session; every other API route and <c>/preview</c> needs a session of the bound
/// organization, writes
/// <c>marketing:edit</c> and approval writes <c>marketing:approve</c>.
/// </summary>
public sealed class MarketingAccessTests
{
    private const string Inquiries = "/api/public/inquiries";
    private const string Approve = "/api/marketing/drafts/missing/approve";

    [Theory]
    [InlineData("/health", HttpStatusCode.OK)]
    [InlineData("/HEALTH/", HttpStatusCode.OK)]
    [InlineData("/api/runtime-config", HttpStatusCode.OK)]
    [InlineData("/api/system/status", HttpStatusCode.OK)]
    [InlineData("/API/SYSTEM/STATUS/", HttpStatusCode.OK)]
    // Marketing maps no read under /api/public: the request passes sign-in and the API fallback answers it.
    [InlineData("/api/public/anything", HttpStatusCode.NotFound)]
    [InlineData("/API/PUBLIC/ANYTHING", HttpStatusCode.NotFound)]
    public async Task PublicRoutes_ShouldAnswerWithoutASession(string path, HttpStatusCode expected)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(expected);
        (response.Headers.GetValues("X-Content-Type-Options").Single()).Should().Be("nosniff");
    }

    [Fact]
    public async Task SignalRoute_ShouldReturn401_WhenAnUnsignedPostHasNoSession()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>
        {
            ["Identity:Client:SignalUrl"] = "https://marketing.test/api/identity/signal",
        });
        using var client = host.Client();

        using var response = await client.PostAsJsonAsync("/api/identity/signal", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicInquiry_ShouldBeAcceptedFromAnAnonymousVisitor()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();

        using var response = await client.PostAsJsonAsync(Inquiries, Inquiry());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Theory]
    [InlineData("/api/public/inquiries")]
    [InlineData("/API/PUBLIC/INQUIRIES")]
    public async Task PublicInquiry_ShouldReturn202_WhenASignedInVisitorSendsNoCsrfToken(string path)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var visitor = await SignedIn(host.Factory, Owner, csrf: false);

        using var response = await visitor.PostAsJsonAsync(path, Inquiry());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        // The same session and no token: a private write still answers the BFF's antiforgery check.
        using var privateWrite = await visitor.PostAsJsonAsync(Approve, new { });
        privateWrite.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(privateWrite)).Should().Be("AntiforgeryValidationFailed");
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/public")]
    [InlineData("/api/marketing/workspace")]
    [InlineData("/Api/Marketing/Workspace")]
    [InlineData("/api/blog/articles")]
    [InlineData("/API/BLOG/ARTICLES")]
    [InlineData("/api/marketing/inquiries")]
    [InlineData("/api/marketing/inquiries/example/activity")]
    [InlineData("/api/marketing/operations")]
    [InlineData("/API/MARKETING/OPERATIONS/CHANNELS")]
    [InlineData("/api/system/status/details")]
    [InlineData("/api/missing-route")]
    [InlineData("/preview")]
    [InlineData("/PREVIEW/123")]
    [InlineData("/API/BLOG/EXPORT/SITE.ZIP")]
    public async Task PrivateRoutes_ShouldNeedASession(string path)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await CodeAsync(response)).Should().Be("sign_in_required");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Theory]
    [InlineData("POST", "/api/runtime-config")]
    [InlineData("POST", "/api/system/status")]
    [InlineData("PUT", "/api/marketing/automation")]
    [InlineData("POST", Approve)]
    public async Task AnonymousWrites_ShouldNeedASession(string method, string path)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodeAsync(response)).Should().Be("sign_in_required");
    }

    [Fact]
    public async Task ForgedCookie_ShouldStayAnonymous_WhenTheRealCookieSchemeReadsIt()
    {
        await using var host = await MarketingHost.CreateAsync(
            new Dictionary<string, string?>(), realCookieScheme: true);
        using var client = host.Client();
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieName}=forged");

        using var read = await client.GetAsync("/api/marketing/workspace");
        using var write = await client.PostAsJsonAsync("/api/marketing/drafts", new { });

        read.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await CodeAsync(read)).Should().Be("sign_in_required");
        write.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean()).Should().BeFalse();
        ((await client.GetAsync("/health")).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SpaShell_ShouldLoadWithoutASession()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = host.Client();

        using var shell = await client.GetAsync("/inquiries");

        shell.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        shell.StatusCode.Should().NotBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task SessionRoute_ShouldAnswerSignedOut_WhenThereIsNoSession()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync("/api/identity/session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        (
            (await response.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("data")
                .GetProperty("isAuthenticated")
                .GetBoolean())
                .Should()
                .BeFalse();
    }

    [Fact]
    public async Task SessionRoute_ShouldNameThePersonOrganizationPermissionsAndCsrfToken()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await SignedIn(host.Factory, Owner, csrf: false);

        var session = await SessionAsync(client);

        (session.GetProperty("isAuthenticated").GetBoolean()).Should().BeTrue();
        (session.GetProperty("person").GetProperty("sub").GetString()).Should().Be("owner-sub");
        (session.GetProperty("organization").GetProperty("id").GetGuid()).Should().Be(Organization);
        (session.GetProperty("organization").GetProperty("role").GetString()).Should().Be("owner");
        (session.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()))
            .Should()
            .Equal(new string?[] { "marketing:edit", "marketing:approve" });
        (string.IsNullOrEmpty(session.GetProperty("csrfToken").GetString())).Should().BeFalse();
    }

    [Fact]
    public async Task SessionRoutes_ShouldNeedHttps()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory, "http://localhost");

        using var response = await client.GetAsync("/api/identity/session");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(response)).Should().Be("https_required");
    }

    [Fact]
    public async Task Owner_ShouldReadAndWriteAsTheirIdentitySubject()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var anonymous = host.Client();
        using var owner = await SignedIn(host.Factory, Owner);
        var id = await SubmitAsync(anonymous);

        ((await owner.GetAsync("/api/marketing/inquiries")).StatusCode).Should().Be(HttpStatusCode.OK);
        using var update = await owner.PutAsJsonAsync(
            $"/api/marketing/inquiries/{id}",
            new { expectedRevision = 1, status = "contacted" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var activity = await owner.GetFromJsonAsync<JsonElement>($"/api/marketing/inquiries/{id}/activity");
        (activity.GetProperty("items")[0].GetProperty("actor").GetString()).Should().Be("owner-sub");
        using var approve = await owner.PostAsJsonAsync(Approve, new { expectedRevision = 1 });
        approve.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        approve.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PrivateWrites_ShouldNeedTheSessionsCsrfToken()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var anonymous = host.Client();
        using var forger = await SignedIn(host.Factory, Owner, csrf: false);
        using var owner = await SignedIn(host.Factory, Owner);
        var id = await SubmitAsync(anonymous);
        object update = new { expectedRevision = 1, status = "contacted" };

        using var forged = await forger.PutAsJsonAsync($"/api/marketing/inquiries/{id}", update);
        forger.DefaultRequestHeaders.Add("X-Csrf-Token", "not-a-token");
        using var wrong = await forger.PutAsJsonAsync($"/api/marketing/inquiries/{id}", update);

        forged.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(forged)).Should().Be("AntiforgeryValidationFailed");
        wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(wrong)).Should().Be("AntiforgeryValidationFailed");
        ((await forger.GetAsync("/api/marketing/inquiries")).StatusCode).Should().Be(HttpStatusCode.OK);
        ((await owner.PutAsJsonAsync($"/api/marketing/inquiries/{id}", update)).StatusCode)
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Editor_ShouldWriteButNotApprove()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var anonymous = host.Client();
        using var editor = await SignedIn(host.Factory, Editor);
        var id = await SubmitAsync(anonymous);

        using var write = await editor.PutAsJsonAsync(
            $"/api/marketing/inquiries/{id}",
            new { expectedRevision = 1, status = "contacted" });
        using var approve = await editor.PostAsJsonAsync(Approve, new { expectedRevision = 1 });

        write.StatusCode.Should().Be(HttpStatusCode.OK);
        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeAsync(approve)).Should().Be("approval_permission_required");
    }

    [Fact]
    public async Task Viewer_ShouldReadButNotWrite()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var viewer = await SignedIn(host.Factory, Viewer);

        using var read = await viewer.GetAsync("/api/marketing/inquiries");
        using var write = await viewer.PutAsJsonAsync(
            "/api/marketing/inquiries/any",
            new { expectedRevision = 1, status = "contacted" });

        read.StatusCode.Should().Be(HttpStatusCode.OK);
        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeAsync(write)).Should().Be("edit_permission_required");
    }

    [Fact]
    public async Task AnotherOrganization_ShouldBeRefusedEverywhereButThePublicAndSessionRoutes()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        foreach (var person in new[] { Outsider, Unaffiliated })
        {
            using var client = await SignedIn(host.Factory, person);

            using var read = await client.GetAsync("/api/marketing/workspace");
            using var write = await client.PostAsJsonAsync("/api/marketing/drafts", new { });

            read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeAsync(read)).Should().Be("organization_access_required");
            write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeAsync(write)).Should().Be("organization_access_required");
            ((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean()).Should().BeTrue();
            ((await client.GetAsync("/api/runtime-config")).StatusCode).Should().Be(HttpStatusCode.OK);
        }
    }

    [Theory]
    [InlineData("GET", "/api/integrations/session")]
    [InlineData("POST", "/api/integrations/connect")]
    [InlineData("DELETE", "/api/integrations/connect")]
    public async Task RetiredSignInRoutes_ShouldNoLongerExist(string method, string path)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var anonymous = host.Client();
        using var owner = await SignedIn(host.Factory, Owner);

        using var unknown = await owner.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        using var refused = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        refused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublicInquiry_ShouldReturn403AndStoreNothing_WhenSentFromAnotherOrigin()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        using var owner = await SignedIn(host.Factory, Owner);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            Inquiries) { Content = JsonContent.Create(Inquiry()) };
        request.Headers.Add("Origin", "https://evil.example");

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await CodeAsync(response)).Should().Be("cross_origin_write");
        (response.Headers.GetValues("X-Content-Type-Options").Single()).Should().Be("nosniff");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var stored = await owner.GetFromJsonAsync<JsonElement>("/api/marketing/inquiries");
        (stored.GetProperty("total").GetInt32()).Should().Be(0);
    }

    [Theory]
    [InlineData("https://evil.example", true)]
    [InlineData("http://localhost", true)]
    [InlineData("https://localhost:8242", true)]
    [InlineData("http://localhost:8358", true)]
    [InlineData("http://127.0.0.1:8358", true)]
    [InlineData("http://[::1]:8358", true)]
    [InlineData("https://localhost.evil.example", true)]
    [InlineData("https://localhost.evil.example:8358", true)]
    [InlineData("https://localhost@evil.example", true)]
    [InlineData("https://localhost:8358@evil.example", true)]
    [InlineData("https://user@localhost", true)]
    [InlineData("https://localhost/path", true)]
    [InlineData("https://localhost/?query=1", true)]
    [InlineData("null", true)]
    [InlineData("https://localhost", false)]
    [InlineData("https://localhost:8358", false)]
    [InlineData("https://127.0.0.1:8358", false)]
    [InlineData("https://[::1]:8358", false)]
    public async Task Writes_ShouldComeFromThisOrTheViteOrigin_BeforeSignInRuns(string origin, bool refused)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        foreach (var path in new[] { Inquiries, "/api/marketing/drafts" })
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                path) { Content = JsonContent.Create(Inquiry()) };
            request.Headers.Add("Origin", origin);

            using var response = await client.SendAsync(request);

            if (refused)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await CodeAsync(response)).Should().Be("cross_origin_write");
            }
            else
            {
                response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
            }
        }
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WriteMethods_ShouldReturn403OnEveryRoute_WhenSentFromAnotherOrigin(string method)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = host.Client();
        foreach (var path in new[] { Inquiries, "/api/marketing/drafts", "/health", "/" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Headers.Add("Origin", "https://evil.example");

            using var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await CodeAsync(response)).Should().Be("cross_origin_write");
        }
    }

    [Theory]
    [InlineData("cross-site", true)]
    [InlineData("same-site", false)]
    [InlineData("same-origin", false)]
    [InlineData("none", false)]
    public async Task Writes_ShouldReturn403OnlyCrossSite_WhenTheyCarryNoOrigin(string site, bool refused)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        using var publicWrite = new HttpRequestMessage(
            HttpMethod.Post,
            Inquiries) { Content = JsonContent.Create(Inquiry()) };
        publicWrite.Headers.Add("Sec-Fetch-Site", site);
        using var privateWrite = new HttpRequestMessage(HttpMethod.Post, "/api/marketing/drafts");
        privateWrite.Headers.Add("Sec-Fetch-Site", site);

        using var publicResponse = await client.SendAsync(publicWrite);
        using var privateResponse = await client.SendAsync(privateWrite);

        publicResponse.StatusCode.Should().Be(refused ? HttpStatusCode.Forbidden : HttpStatusCode.Accepted);
        privateResponse.StatusCode.Should().Be(refused ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized);
    }

    [Theory]
    // The boot bundle's health endpoint is never cached.
    [InlineData("GET", "/health", true)]
    [InlineData("GET", "/api", true)]
    [InlineData("GET", "/API/RUNTIME-CONFIG", true)]
    [InlineData("GET", "/api/system/status", true)]
    [InlineData("GET", "/api/public/anything", true)]
    [InlineData("GET", "/api/marketing/workspace", true)]
    [InlineData("GET", "/api/missing-route", true)]
    [InlineData("GET", "/preview", true)]
    [InlineData("POST", Inquiries, true)]
    public async Task EveryResponse_ShouldBeNosniffAndTheApiNoStore(string method, string path, bool noStore)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST") request.Content = JsonContent.Create(Inquiry());

        using var response = await client.SendAsync(request);

        (response.Headers.GetValues("X-Content-Type-Options").Single()).Should().Be("nosniff");
        (response.Headers.CacheControl?.NoStore == true).Should().Be(noStore);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task Host_ShouldRefuseToBoot_WhenNoOrganizationIsBound(string? organization)
    {
        await using var host = await MarketingHost.CreateAsync(
            new Dictionary<string, string?> { ["Marketing:OrganizationId"] = organization });

        FluentActions.Invoking(() => host.Client()).Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("Identity:Client:Authority")]
    [InlineData("Identity:Client:ClientId")]
    public async Task Host_ShouldRefuseToBoot_WhenNoIdentityClientIsConfigured(string key)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?> { [key] = null });

        FluentActions.Invoking(() => host.Client()).Should().Throw<Exception>();
    }

    [Fact]
    public async Task KeyRingAndSessions_ShouldKeepAMemberSignedIn_WhenTheHostRestarts()
    {
        await using var first = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        var cookie = first.Factory.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("session")
            .Protect("member");
        await first.Factory.Services.GetRequiredService<IDistributedCache>().SetStringAsync("session-1", "ticket");

        // A restart: another host on the same database, with a data folder of its own.
        await using var restarted = await MarketingHost.CreateAsync(
            new Dictionary<string, string?>(), connectionString: first.ConnectionString);
        var services = restarted.Factory.Services;

        var keys = services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
        var protection = services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("session");

        keys.XmlRepository.Should().BeOfType<PostgresXmlRepository>();
        protection.ApplicationDiscriminator.Should().Be("ocharo-marketing");
        protector.Unprotect(cookie).Should().Be("member");
        (services.GetRequiredService<IDistributedCache>()).Should().BeOfType<PostgresDistributedCache>();
        (await services.GetRequiredService<IDistributedCache>().GetStringAsync("session-1")).Should().Be("ticket");
    }

    [Fact]
    public async Task KeyRing_ShouldTakeTheConfiguredApplicationName()
    {
        await using var host = await MarketingHost.CreateAsync(
            new Dictionary<string, string?> { ["DataProtection:ApplicationName"] = "ocharo-marketing-staging" });

        (host.Factory.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator)
            .Should()
            .Be("ocharo-marketing-staging");
    }

    private static object Inquiry() => new
    {
        submissionId = Guid.NewGuid(), name = "Reader", contactMethod = "email", contact = "reader@example.test",
        message = "Question", sourcePath = "/catalogue/", productId = (string?)null, consent = true,
    };

    private static async Task<string> SubmitAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(Inquiries, Inquiry());
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("reference").GetString()!;
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
