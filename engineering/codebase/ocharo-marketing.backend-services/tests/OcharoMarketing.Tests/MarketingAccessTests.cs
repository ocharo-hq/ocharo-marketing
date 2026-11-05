using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static OcharoMarketing.Tests.MarketingTestIdentity;

namespace OcharoMarketing.Tests;

/// <summary>
/// Marketing signs in through Ocharo's Identity BFF: technical and <c>/api/public/</c> routes answer anonymously, the inquiry
/// form with or without a session; every other API route and <c>/preview</c> needs a session of the bound organization, writes
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
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task PublicInquiry_ShouldBeAcceptedFromAnAnonymousVisitor()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();

        using var response = await client.PostAsJsonAsync(Inquiries, Inquiry());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/public/inquiries")]
    [InlineData("/API/PUBLIC/INQUIRIES")]
    public async Task PublicInquiry_ShouldBeAcceptedFromASignedInVisitor_WithoutACsrfToken(string path)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var visitor = await SignedIn(host.Factory, Owner, csrf: false);

        using var response = await visitor.PostAsJsonAsync(path, Inquiry());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        // The same session and no token: a private write still answers the BFF's antiforgery check.
        using var privateWrite = await visitor.PostAsJsonAsync(Approve, new { });
        Assert.Equal(HttpStatusCode.BadRequest, privateWrite.StatusCode);
        Assert.Equal("AntiforgeryValidationFailed", await CodeAsync(privateWrite));
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
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("sign_in_required", await CodeAsync(response));
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("POST", "/api/runtime-config")]
    [InlineData("POST", "/api/system/status")]
    [InlineData("PUT", "/api/marketing/automation")]
    [InlineData("POST", Approve)]
    public async Task AnonymousWrites_ShouldNeedASession(string method, string path)
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("sign_in_required", await CodeAsync(response));
    }

    [Fact]
    public async Task ForgedCookie_ShouldStayAnonymous_UnderTheRealCookieScheme()
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>(), realCookieScheme: true);
        using var client = host.Client();
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieName}=forged");

        using var read = await client.GetAsync("/api/marketing/workspace");
        using var write = await client.PostAsJsonAsync("/api/marketing/drafts", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        Assert.Equal("sign_in_required", await CodeAsync(read));
        Assert.Equal(HttpStatusCode.Unauthorized, write.StatusCode);
        Assert.False((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task SpaShell_ShouldLoadWithoutASession()
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();

        using var shell = await client.GetAsync("/inquiries");

        Assert.NotEqual(HttpStatusCode.Unauthorized, shell.StatusCode);
        Assert.NotEqual(HttpStatusCode.Redirect, shell.StatusCode);
    }

    [Fact]
    public async Task SessionRoute_ShouldAnswerSignedOut_WithoutASession()
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync("/api/identity/session");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.False((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("isAuthenticated").GetBoolean());
    }

    [Fact]
    public async Task SessionRoute_ShouldNameThePersonOrganizationPermissionsAndCsrfToken()
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = await SignedIn(host.Factory, Owner, csrf: false);

        var session = await SessionAsync(client);

        Assert.True(session.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal("owner-sub", session.GetProperty("person").GetProperty("sub").GetString());
        Assert.Equal(Organization, session.GetProperty("organization").GetProperty("id").GetGuid());
        Assert.Equal("owner", session.GetProperty("organization").GetProperty("role").GetString());
        Assert.Equal(new string?[] { "marketing:edit", "marketing:approve" }, session.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.False(string.IsNullOrEmpty(session.GetProperty("csrfToken").GetString()));
    }

    [Fact]
    public async Task SessionRoutes_ShouldNeedHttps()
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = Anonymous(host.Factory, "http://localhost");

        using var response = await client.GetAsync("/api/identity/session");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("https_required", await CodeAsync(response));
    }

    [Fact]
    public async Task Owner_ShouldReadAndWrite_AsTheirIdentitySubject()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var anonymous = host.Client();
        using var owner = await SignedIn(host.Factory, Owner);
        var id = await SubmitAsync(anonymous);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/marketing/inquiries")).StatusCode);
        using var update = await owner.PutAsJsonAsync($"/api/marketing/inquiries/{id}", new { expectedRevision = 1, status = "contacted" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var activity = await owner.GetFromJsonAsync<JsonElement>($"/api/marketing/inquiries/{id}/activity");
        Assert.Equal("owner-sub", activity.GetProperty("items")[0].GetProperty("actor").GetString());
        using var approve = await owner.PostAsJsonAsync(Approve, new { expectedRevision = 1 });
        Assert.NotEqual(HttpStatusCode.Unauthorized, approve.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, approve.StatusCode);
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

        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal("AntiforgeryValidationFailed", await CodeAsync(forged));
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("AntiforgeryValidationFailed", await CodeAsync(wrong));
        Assert.Equal(HttpStatusCode.OK, (await forger.GetAsync("/api/marketing/inquiries")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/marketing/inquiries/{id}", update)).StatusCode);
    }

    [Fact]
    public async Task Editor_ShouldWrite_ButNotApprove()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var anonymous = host.Client();
        using var editor = await SignedIn(host.Factory, Editor);
        var id = await SubmitAsync(anonymous);

        using var write = await editor.PutAsJsonAsync($"/api/marketing/inquiries/{id}", new { expectedRevision = 1, status = "contacted" });
        using var approve = await editor.PostAsJsonAsync(Approve, new { expectedRevision = 1 });

        Assert.Equal(HttpStatusCode.OK, write.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal("approval_permission_required", await CodeAsync(approve));
    }

    [Fact]
    public async Task Viewer_ShouldRead_ButNotWrite()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var viewer = await SignedIn(host.Factory, Viewer);

        using var read = await viewer.GetAsync("/api/marketing/inquiries");
        using var write = await viewer.PutAsJsonAsync("/api/marketing/inquiries/any", new { expectedRevision = 1, status = "contacted" });

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Equal("edit_permission_required", await CodeAsync(write));
    }

    [Fact]
    public async Task AnotherOrganization_ShouldBeRefusedEverywhereButThePublicAndSessionRoutes()
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        foreach (var person in new[] { Outsider, Unaffiliated })
        {
            using var client = await SignedIn(host.Factory, person);

            using var read = await client.GetAsync("/api/marketing/workspace");
            using var write = await client.PostAsJsonAsync("/api/marketing/drafts", new { });

            Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
            Assert.Equal("organization_access_required", await CodeAsync(read));
            Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
            Assert.Equal("organization_access_required", await CodeAsync(write));
            Assert.True((await SessionAsync(client)).GetProperty("isAuthenticated").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/runtime-config")).StatusCode);
        }
    }

    [Theory]
    [InlineData("GET", "/api/sso/session")]
    [InlineData("GET", "/api/sso/login")]
    [InlineData("GET", "/api/integrations/session")]
    [InlineData("POST", "/api/integrations/connect")]
    [InlineData("DELETE", "/api/integrations/connect")]
    public async Task RetiredSignInRoutes_ShouldNoLongerExist(string method, string path)
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var anonymous = host.Client();
        using var owner = await SignedIn(host.Factory, Owner);

        using var unknown = await owner.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        using var refused = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
    }

    [Fact]
    public async Task PublicInquiry_ShouldBeRefusedFromAnotherOrigin_AndStoreNothing()
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        using var owner = await SignedIn(host.Factory, Owner);
        using var request = new HttpRequestMessage(HttpMethod.Post, Inquiries) { Content = JsonContent.Create(Inquiry()) };
        request.Headers.Add("Origin", "https://evil.example");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("cross_origin_write", await CodeAsync(response));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.True(response.Headers.CacheControl!.NoStore);
        var stored = await owner.GetFromJsonAsync<JsonElement>("/api/marketing/inquiries");
        Assert.Equal(0, stored.GetProperty("total").GetInt32());
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
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(Inquiry()) };
            request.Headers.Add("Origin", origin);

            using var response = await client.SendAsync(request);

            if (refused)
            {
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Equal("cross_origin_write", await CodeAsync(response));
            }
            else
            {
                Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
            }
        }
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task EveryWriteMethod_ShouldBeRefusedFromAnotherOrigin_OnEveryRoute(string method)
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();
        foreach (var path in new[] { Inquiries, "/api/marketing/drafts", "/health", "/" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            request.Headers.Add("Origin", "https://evil.example");

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("cross_origin_write", await CodeAsync(response));
        }
    }

    [Theory]
    [InlineData("cross-site", true)]
    [InlineData("same-site", false)]
    [InlineData("same-origin", false)]
    [InlineData("none", false)]
    public async Task Writes_WithoutAnOrigin_ShouldBeRefusedOnlyWhenCrossSite(string site, bool refused)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        using var publicWrite = new HttpRequestMessage(HttpMethod.Post, Inquiries) { Content = JsonContent.Create(Inquiry()) };
        publicWrite.Headers.Add("Sec-Fetch-Site", site);
        using var privateWrite = new HttpRequestMessage(HttpMethod.Post, "/api/marketing/drafts");
        privateWrite.Headers.Add("Sec-Fetch-Site", site);

        using var publicResponse = await client.SendAsync(publicWrite);
        using var privateResponse = await client.SendAsync(privateWrite);

        Assert.Equal(refused ? HttpStatusCode.Forbidden : HttpStatusCode.Accepted, publicResponse.StatusCode);
        Assert.Equal(refused ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized, privateResponse.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/health", false)]
    [InlineData("GET", "/api", true)]
    [InlineData("GET", "/API/RUNTIME-CONFIG", true)]
    [InlineData("GET", "/api/system/status", true)]
    [InlineData("GET", "/api/public/anything", true)]
    [InlineData("GET", "/api/marketing/workspace", true)]
    [InlineData("GET", "/api/missing-route", true)]
    [InlineData("GET", "/preview", true)]
    [InlineData("POST", Inquiries, true)]
    public async Task EveryResponse_ShouldBeNosniff_AndTheApiNoStore(string method, string path, bool noStore)
    {
        await using var host = await MarketingHost.CreateWithDatabaseAsync();
        using var client = host.Client();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST") request.Content = JsonContent.Create(Inquiry());

        using var response = await client.SendAsync(request);

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(noStore, response.Headers.CacheControl?.NoStore == true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public async Task Host_ShouldRefuseToBoot_WithoutABoundOrganization(string? organization)
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?> { ["Marketing:OrganizationId"] = organization });

        Assert.ThrowsAny<Exception>(() => host.Client());
    }

    [Theory]
    [InlineData("Identity:Client:Authority")]
    [InlineData("Identity:Client:ClientId")]
    public async Task Host_ShouldRefuseToBoot_WithoutAnIdentityClient(string key)
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?> { [key] = null });

        Assert.ThrowsAny<Exception>(() => host.Client());
    }

    [Fact]
    public async Task KeyRing_ShouldLiveUnderTheDataDirectory_UnlessKeysPathNamesAnother()
    {
        await using var byDefault = MarketingHost.Create(new Dictionary<string, string?>());
        var elsewhere = Path.Combine(Path.GetTempPath(), "ocharo-marketing-keys", Guid.NewGuid().ToString("N"));
        await using var named = MarketingHost.Create(new Dictionary<string, string?> { ["DataProtection:KeysPath"] = elsewhere });

        Assert.Equal(Path.Combine(byDefault.Root, "keys"), KeyRingFolder(byDefault));
        Assert.Equal(elsewhere, KeyRingFolder(named));
    }

    private static string KeyRingFolder(MarketingHost host)
    {
        var repository = host.Factory.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository;
        return Assert.IsType<FileSystemXmlRepository>(repository).Directory.FullName;
    }

    private static object Inquiry() => new
    {
        submissionId = Guid.NewGuid(), name = "Reader", contactMethod = "email", contact = "reader@example.test",
        message = "Question", sourcePath = "/catalogue/", productId = (string?)null, consent = true,
    };

    private static async Task<string> SubmitAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(Inquiries, Inquiry());
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("reference").GetString()!;
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
