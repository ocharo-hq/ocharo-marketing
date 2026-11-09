using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using OcharoBlog.Api.Access;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Signals;
using WoW.Two.Sdk.Beta.Be.Identity.Testing;
using static OcharoBlog.Tests.EditorialSignIn;
using Xunit;

namespace OcharoBlog.Tests;

/// <summary>
/// Editorial's two service paths, with no Identity running: as a caller it reaches Desk's background removal with its own
/// service token, and as a resource its live exports take a service token beside a session.
/// </summary>
[Collection(MediaEditingCapacity.Name)]
public sealed class EditorialServiceTokenTests
{
    private const string Desk = "https://desk.test/";
    private const string CapabilitiesPath = "api/integrations/media/background-removal/capabilities";
    private const string RemovalPath = "api/integrations/media/background-removal";
    private const string UnavailableMessage = "Background removal is unavailable. Your original image is unchanged.";
    private static readonly MediaEditRecipe Removal = new(new(0, 0, 20, 30), 90, false, false, null, new("png", 90), true);

    [Fact]
    public async Task DeskAccepting_ShouldReportAvailableAndProcessARemoval_AsEditorialItself()
    {
        await using var rig = new DeskRig();
        using var owner = Client(rig.Host.Factory, Owner);
        var sha = await UploadAsync(owner);

        var status = await StatusAsync(owner);
        Assert.True(status.GetProperty("available").GetBoolean());
        Assert.Equal("desk-model", status.GetProperty("model").GetString());
        using var preview = await owner.PostAsJsonAsync($"/api/blog/media/{sha}/edits/preview", Removal);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        using var saved = await owner.PostAsJsonAsync($"/api/blog/media/{sha}/edits", Removal);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var output = (await saved.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString();
        var history = await owner.GetFromJsonAsync<JsonElement[]>($"/api/blog/media/{output}/edits");
        Assert.Equal("desk-model", Assert.Single(history!).GetProperty("model").GetString());

        Assert.Equal(new[] { $"GET {Desk}{CapabilitiesPath}", $"POST {Desk}{RemovalPath}", $"POST {Desk}{RemovalPath}" },
            rig.DeskCalls.Select(call => $"{call.Method} {call.Uri}"));
        foreach (var call in rig.DeskCalls)
        {
            Assert.Equal("Bearer", call.Authorization?.Scheme);
            var token = new JsonWebToken(call.Authorization!.Parameter);
            Assert.Equal(new[] { Desk }, token.Audiences);
            Assert.Equal("editorial", token.Subject);
            Assert.Equal("editorial", token.GetClaim("client_id").Value);
            Assert.Equal("desk.media", token.GetClaim("scope").Value);
            foreach (var header in new[] { "Cookie", "X-Ocharo-Integration-Ticket", "X-Csrf-Token", PersonHeader })
                Assert.DoesNotContain(header, call.Headers, StringComparer.OrdinalIgnoreCase);
        }

        // One token per target, renewed near expiry: every call above shared it.
        var form = Assert.Single(rig.TokenRequests);
        Assert.Equal("https://identity.example.test/connect/token", form.Uri.AbsoluteUri);
        var fields = QueryHelpers.ParseQuery(form.Body);
        Assert.Equal("client_credentials", fields["grant_type"]);
        Assert.Equal("editorial", fields["client_id"]);
        Assert.Equal("test-only-secret", fields["client_secret"]);
        Assert.Equal(Desk, fields["resource"]);
        Assert.False(fields.ContainsKey("scope"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("desk.test")]
    [InlineData("http://desk.test/")]
    [InlineData("https://user@desk.test/")]
    [InlineData("https://desk.test/app")]
    [InlineData("https://desk.test/?a=1")]
    [InlineData("https://desk.test/#a")]
    public async Task UnusableDesk_ShouldReadUnavailable_WithNoTokenRequest(string? desk)
    {
        await using var rig = new DeskRig(desk);
        using var owner = Client(rig.Host.Factory, Owner);
        var sha = await UploadAsync(owner);

        Assert.False((await StatusAsync(owner)).GetProperty("available").GetBoolean());
        await AssertUnavailableAsync(await owner.PostAsJsonAsync($"/api/blog/media/{sha}/edits", Removal));

        Assert.Empty(rig.TokenRequests);
        Assert.Empty(rig.DeskCalls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task DeskRefusingTheToken_ShouldReadUnavailable_AndWarnWithoutTheToken(HttpStatusCode refusal)
    {
        await using var rig = new DeskRig { DeskAnswer = _ => new(refusal) };
        using var owner = Client(rig.Host.Factory, Owner);
        var sha = await UploadAsync(owner);

        Assert.False((await StatusAsync(owner)).GetProperty("available").GetBoolean());
        await AssertUnavailableAsync(await owner.PostAsJsonAsync($"/api/blog/media/{sha}/edits", Removal));

        Assert.Contains(rig.Logs.Warnings, warning => warning.Contains("Desk refused", StringComparison.Ordinal) && warning.Contains(((int)refusal).ToString(), StringComparison.Ordinal));
        foreach (var token in rig.DeskCalls.Select(call => call.Authorization!.Parameter!))
            Assert.DoesNotContain(rig.Logs.Warnings, warning => warning.Contains(token, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("refused")]
    [InlineData("gateway")]
    [InlineData("no-token")]
    [InlineData("not-json")]
    [InlineData("offline")]
    public async Task FailedTokenRequest_ShouldReadUnavailable_WithNoCallToDesk(string failure)
    {
        await using var rig = new DeskRig
        {
            TokenAnswer = () => failure switch
            {
                "refused" => new(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { error = "invalid_client" }) },
                "gateway" => new(HttpStatusCode.BadGateway) { Content = new StringContent("<html>bad gateway</html>") },
                "no-token" => new(HttpStatusCode.OK) { Content = JsonContent.Create(new { token_type = "Bearer" }) },
                "not-json" => new(HttpStatusCode.OK) { Content = new StringContent("<html>sign in</html>") },
                _ => throw new HttpRequestException("Connection refused"),
            },
        };
        using var owner = Client(rig.Host.Factory, Owner);
        var sha = await UploadAsync(owner);

        Assert.False((await StatusAsync(owner)).GetProperty("available").GetBoolean());
        await AssertUnavailableAsync(await owner.PostAsJsonAsync($"/api/blog/media/{sha}/edits", Removal));

        Assert.NotEmpty(rig.TokenRequests);
        Assert.Empty(rig.DeskCalls);
    }

    [Theory]
    [InlineData("edits")]
    [InlineData("edits/preview")]
    public async Task PersonWithoutEdit_ShouldBeForbidden_BeforeAnyCall(string route)
    {
        await using var rig = new DeskRig();
        using var owner = Client(rig.Host.Factory, Owner);
        using var viewer = Client(rig.Host.Factory, Viewer);
        var sha = await UploadAsync(owner);

        using var response = await viewer.PostAsJsonAsync($"/api/blog/media/{sha}/{route}", Removal);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("edit_permission_required", await CodeAsync(response));
        Assert.Empty(rig.TokenRequests);
        Assert.Empty(rig.DeskCalls);
    }

    [Fact]
    public async Task DeskBusy_ShouldKeepTheBusyMessage()
    {
        await using var rig = new DeskRig { DeskAnswer = _ => new(HttpStatusCode.TooManyRequests) };
        using var owner = Client(rig.Host.Factory, Owner);
        var sha = await UploadAsync(owner);

        using var response = await owner.PostAsJsonAsync($"/api/blog/media/{sha}/edits", Removal);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Contains("Image processing is busy. Try again shortly.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("text/plain", "desk-model", 16)]
    [InlineData("image/png", null, 16)]
    [InlineData("image/png", "bad model", 16)]
    [InlineData("image/png", "desk-model", MediaEditingService.MaximumBytes + 1)]
    public async Task DeskAnswerOutsideTheContract_ShouldReadUnavailable(string contentType, string? model, int length)
    {
        await using var rig = new DeskRig
        {
            DeskAnswer = _ =>
            {
                var answer = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[length]) };
                answer.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                if (model is not null) answer.Headers.TryAddWithoutValidation("X-Image-Model", model);
                return answer;
            },
        };
        using var owner = Client(rig.Host.Factory, Owner);

        await AssertUnavailableAsync(await owner.PostAsJsonAsync($"/api/blog/media/{await UploadAsync(owner)}/edits", Removal));
    }

    [Fact]
    public async Task DeskRedirecting_ShouldReadUnavailable()
    {
        await using var rig = new DeskRig
        {
            DeskAnswer = _ => new(HttpStatusCode.Found) { Headers = { Location = new Uri("https://elsewhere.test/") } },
        };
        using var owner = Client(rig.Host.Factory, Owner);

        await AssertUnavailableAsync(await owner.PostAsJsonAsync($"/api/blog/media/{await UploadAsync(owner)}/edits", Removal));

        Assert.Single(rig.DeskCalls);
    }

    [Fact]
    public async Task DeskClient_ShouldFollowNoRedirectAndKeepNoCookies()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());

        Assert.Equal(TimeSpan.FromSeconds(95), host.Factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient(EditorialImageWorker.ClientName).Timeout);
        HttpMessageHandler? handler = host.Factory.Services.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(EditorialImageWorker.ClientName);
        while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler;
        var primary = Assert.IsType<HttpClientHandler>(handler);
        Assert.False(primary.AllowAutoRedirect);
        Assert.False(primary.UseCookies);
    }

    [Theory]
    [InlineData("/api/blog/export/site.zip")]
    [InlineData("/api/blog/export/atlas.zip")]
    [InlineData("/api/blog/export/wear.zip")]
    [InlineData("/api/blog/export/catalogue.zip")]
    [InlineData("/API/Blog/Export/Site.zip")]
    public async Task ExportToken_ShouldReadTheZip(string path)
    {
        using var issuer = new FakeTokenIssuer();
        await using var harness = new BlogHarness(services: services => services.AddFakeTokenIssuer(issuer));
        using var client = Bearer(harness.Factory, issuer.IssueServiceToken("ocharo-platform", Resource, "other.scope", "editorial.export"));

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/blog/export/site.zip")]
    [InlineData("/api/blog/export/atlas.zip")]
    public async Task ExportToken_ShouldBeRefused_WhenItDoesNotHold(string path)
    {
        using var issuer = new FakeTokenIssuer();
        using var stranger = new FakeTokenIssuer();
        using var past = new FakeTokenIssuer(clock: new FakeTimeProvider(DateTimeOffset.UtcNow.AddHours(-1)));
        await using var harness = new BlogHarness(seed: false, services: services => services.AddFakeTokenIssuer(issuer));
        var person = new FakePerson { Id = Guid.NewGuid(), OrganizationId = Organization, Role = "owner", Permissions = ["editorial:edit"], SessionId = Guid.NewGuid() };

        foreach (var token in new[]
        {
            issuer.IssueServiceToken("ocharo-platform", "https://elsewhere.test/", "editorial.export"),
            stranger.IssueServiceToken("ocharo-platform", Resource, "editorial.export"),
            past.IssueServiceToken("ocharo-platform", Resource, "editorial.export"),
            "not-a-token",
        })
        {
            using var client = Bearer(harness.Factory, token);
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("sign_in_required", await CodeAsync(response));
        }

        foreach (var token in new[]
        {
            issuer.IssueServiceToken("ocharo-platform", Resource),
            issuer.IssueServiceToken("ocharo-platform", Resource, "editorial.read"),
            issuer.IssuePersonToken(person, Resource, scopes: "editorial.export"),
            issuer.IssueExchangedToken(person, "ocharo-platform", Resource, "editorial.export"),
        })
        {
            using var client = Bearer(harness.Factory, token);
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("scope_required", await CodeAsync(response));
        }
    }

    [Theory]
    [InlineData("/api/blog/export/site.zip")]
    [InlineData("/api/blog/export/atlas.zip")]
    public async Task Export_WithNoTokenAndNoSession_ShouldNeedSignIn(string path)
    {
        using var issuer = new FakeTokenIssuer();
        await using var harness = new BlogHarness(seed: false, services: services => services.AddFakeTokenIssuer(issuer));

        using var response = await harness.Anonymous().GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("sign_in_required", await CodeAsync(response));
    }

    [Theory]
    [InlineData("/api/blog/export/site.zip")]
    [InlineData("/api/blog/export/atlas.zip")]
    public async Task Export_ShouldStillAdmitTheSignedInEditor_UnlessABadTokenComesToo(string path)
    {
        using var issuer = new FakeTokenIssuer();
        await using var harness = new BlogHarness(seed: false, services: services => services.AddFakeTokenIssuer(issuer));
        using var editor = Client(harness.Factory, Editor);

        using var response = await editor.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        editor.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-token");
        using var refused = await editor.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/blog/articles")]
    [InlineData("GET", "/api/blog/media")]
    [InlineData("GET", "/api/blog/export/other.zip")]
    [InlineData("GET", "/api/blog/export/site.zip/more")]
    [InlineData("GET", "/preview/apps/atlas/")]
    [InlineData("HEAD", "/api/blog/export/site.zip")]
    [InlineData("POST", "/api/blog/export/site.zip")]
    [InlineData("POST", "/api/blog/articles")]
    public async Task ValidToken_ShouldGrantNothingElsewhere(string method, string path)
    {
        using var issuer = new FakeTokenIssuer();
        await using var harness = new BlogHarness(seed: false, services: services => services.AddFakeTokenIssuer(issuer));
        using var client = Bearer(harness.Factory, issuer.IssueServiceToken("ocharo-platform", Resource, "editorial.export"));

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        if (method != "HEAD") Assert.Equal("sign_in_required", await CodeAsync(response));
    }

    [Fact]
    public async Task SessionCookie_ShouldStayTheDefaultScheme()
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        var schemes = host.Factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

        Assert.Equal(BffAuthenticationSchemeConstants.Cookie, (await schemes.GetDefaultAuthenticateSchemeAsync())!.Name);
        Assert.Equal(BffAuthenticationSchemeConstants.Cookie, (await schemes.GetDefaultChallengeSchemeAsync())!.Name);
        Assert.Equal(BffAuthenticationSchemeConstants.Cookie, (await schemes.GetDefaultForbidSchemeAsync())!.Name);
    }

    [Fact]
    public async Task Audience_ShouldBeNormalizedToItsAbsoluteForm()
    {
        using var issuer = new FakeTokenIssuer();
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { ["Identity:Resource:Uri"] = "https://EDITORIAL.example.test" },
            services: services => services.AddFakeTokenIssuer(issuer));
        using var client = Bearer(host.Factory, issuer.IssueServiceToken("ocharo-platform", Resource, "editorial.export"));

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/blog/export/site.zip")).StatusCode);
    }

    [Fact]
    public async Task SignedSignal_ShouldBeAccepted_AndADeniedSessionsTokenRefused()
    {
        const string signalUrl = "https://editorial.example.test/signals/identity";
        using var issuer = new FakeTokenIssuer();
        using var sender = new IdentitySignalSender(issuer.Issuer);
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { [EditorialAccess.SignalUrlKey] = signalUrl },
            services: services =>
            {
                services.AddFakeTokenIssuer(issuer);
                services.PostConfigure<IdentitySignalReceiverOptions>(options => options.TrustedIssuers.Add(sender.TrustedIssuer));
            });
        var person = new FakePerson { Id = Guid.NewGuid(), OrganizationId = Organization, SessionId = Guid.NewGuid() };
        using var bearer = Bearer(host.Factory, issuer.IssuePersonToken(person, Resource, "fake-app", EditorialAccess.ExportScope));
        // The token passes, and only then does the export refuse a person: that 403 is the "still counts" answer.
        Assert.Equal(HttpStatusCode.Forbidden, (await bearer.GetAsync("/api/blog/export/site.zip")).StatusCode);

        using var anonymous = Anonymous(host.Factory);
        using var signal = await anonymous.SendAsync(sender.SessionEnded(signalUrl, person.SessionId!.Value.ToString("D"), person.Id.ToString("D")));
        Assert.Equal(HttpStatusCode.Accepted, signal.StatusCode);

        // This test host runs no hosted services, so the bus stays idle: the entry its handler would write goes in directly.
        await host.Factory.Services.GetRequiredService<ITokenDenyList>().AddSessionAsync(issuer.Issuer, person.SessionId!.Value.ToString("D"), null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await bearer.GetAsync("/api/blog/export/site.zip")).StatusCode);
    }

    [Theory]
    [InlineData("/signals/identity")]
    [InlineData("/api/identity/signal")]
    public async Task SignalRoutes_ShouldAnswer401_ForAnUnsignedPostWithoutASession(string path)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>
        {
            [EditorialAccess.SignalUrlKey] = "https://editorial.example.test/signals/identity",
            ["Identity:Client:SignalUrl"] = "https://editorial.example.test/api/identity/signal",
        });
        using var client = Anonymous(host.Factory);

        using var response = await client.PostAsync(path, new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RealIssuerCheck_ShouldRefuseAnUnknownIssuer_WithNoDiscovery()
    {
        using var stranger = new FakeTokenIssuer("https://stranger.test/");
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = Bearer(host.Factory, stranger.IssueServiceToken("ocharo-platform", Resource, "editorial.export"));

        using var response = await client.GetAsync("/api/blog/export/site.zip");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("sign_in_required", await CodeAsync(response));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-uri")]
    [InlineData("http://editorial.example.test/")]
    public async Task Host_ShouldRefuseToBoot_WithoutAResourceUri(string? uri)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { ["Identity:Resource:Uri"] = uri });

        var failure = Record.Exception(() => host.Factory.CreateClient());

        Assert.NotNull(failure);
        Assert.Contains("Identity:Resource:Uri", failure.ToString());
    }

    private static HttpClient Bearer(WebApplicationFactory<Program> factory, string token)
    {
        var client = Anonymous(factory);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> StatusAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/blog/media/editing-capabilities")).GetProperty("backgroundRemoval");

    private static async Task<string> UploadAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(BlogHarness.Png(80));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "original.png");
        using var response = await client.PostAsync("/api/blog/media", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sha256").GetString()!;
    }

    private static async Task AssertUnavailableAsync(HttpResponseMessage response)
    {
        using (response)
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains(UnavailableMessage, await response.Content.ReadAsStringAsync());
        }
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>A host whose Desk is a stub and whose token endpoint is a fake issuer's, both keeping what they were sent.</summary>
    private sealed class DeskRig : IAsyncDisposable
    {
        private readonly FakeTokenIssuer issuer = new("https://identity.example.test/");
        private readonly Lazy<EditorialHost> host;

        public DeskRig(string? desk = Desk)
        {
            issuer.ResourceScopes[Desk] = ["desk.media"];
            host = new(() => EditorialHost.Create(new Dictionary<string, string?>
            {
                ["Blog:DeskBaseUrl"] = desk,
                ["Identity:Client:ClientSecret"] = "test-only-secret",
            }, Logs, services =>
            {
                services.AddHttpClient(EditorialImageWorker.ClientName).ConfigurePrimaryHttpMessageHandler(() => new DeskStub(this));
                services.AddHttpClient(OAuth2HttpClientNames.TokenEndpoint).ConfigurePrimaryHttpMessageHandler(() => new TokenEndpoint(this, issuer.CreateTokenEndpointHandler()));
            }));
        }

        public EditorialHost Host => host.Value;
        public CapturedLogs Logs { get; } = new();
        public ConcurrentQueue<DeskCall> DeskCalls { get; } = new();
        public ConcurrentQueue<TokenRequest> TokenRequests { get; } = new();
        public Func<HttpRequestMessage, HttpResponseMessage> DeskAnswer { get; init; } = Accept;
        public Func<HttpResponseMessage>? TokenAnswer { get; init; }

        public async ValueTask DisposeAsync()
        {
            await Host.DisposeAsync();
            issuer.Dispose();
        }

        private static HttpResponseMessage Accept(HttpRequestMessage request)
        {
            if (request.Method == HttpMethod.Get)
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { available = true, model = "desk-model" }) };
            var answer = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(BlogHarness.Png(200)) };
            answer.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            answer.Headers.Add("X-Image-Model", "desk-model");
            return answer;
        }
    }

    private sealed record DeskCall(HttpMethod Method, Uri Uri, AuthenticationHeaderValue? Authorization, string[] Headers);

    private sealed record TokenRequest(Uri Uri, string Body);

    private sealed class DeskStub(DeskRig rig) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            rig.DeskCalls.Enqueue(new(request.Method, request.RequestUri!, request.Headers.Authorization, request.Headers.Select(header => header.Key).ToArray()));
            return Task.FromResult(rig.DeskAnswer(request));
        }
    }

    private sealed class TokenEndpoint(DeskRig rig, HttpMessageHandler fake) : DelegatingHandler(fake)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            rig.TokenRequests.Enqueue(new(request.RequestUri!, await request.Content!.ReadAsStringAsync(cancellationToken)));
            return rig.TokenAnswer is { } answer ? answer() : await base.SendAsync(request, cancellationToken);
        }
    }
}

/// <summary>Tests that take the media editor's process-wide capacity, which one test fills on purpose, so they don't run together.</summary>
internal static class MediaEditingCapacity
{
    public const string Name = "Media editing capacity";
}
