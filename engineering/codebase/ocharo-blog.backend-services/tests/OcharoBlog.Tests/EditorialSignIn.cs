using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;

namespace OcharoBlog.Tests;

/// <summary>
/// What the Editorial test hosts replace for sign-in: a stand-in for the Identity BFF cookie scheme. A request that names one
/// of the <see cref="People"/> in <see cref="PersonHeader"/> is signed in with the claims Identity's token carries and a
/// session reference, so the BFF's <c>X-Csrf-Token</c> check holds as for a real session; any other request is anonymous. No
/// call reaches the Identity instance.
/// </summary>
internal static class EditorialSignIn
{
    /// <summary>Names the person a request signs in as.</summary>
    public const string PersonHeader = "X-Test-Person";

    /// <summary>The organization the test hosts bind (<c>Blog:OrganizationId</c>).</summary>
    public static readonly Guid Organization = Guid.Parse("5f0c9b1e-4a7d-4c2e-9b8a-1d2e3f405162");

    /// <summary>The audience the test hosts serve (<c>Identity:Resource:Uri</c>).</summary>
    public const string Resource = "https://editorial.example.test/";

    /// <summary>A signed-in person; a null <paramref name="OrganizationId"/> carries no organization claims.</summary>
    public sealed record Person(string Subject, Guid? OrganizationId, string? Role);

    public static readonly Person Owner = new("owner-sub", Organization, "owner");
    public static readonly Person Editor = new("editor-sub", Organization, "editor");
    public static readonly Person Viewer = new("viewer-sub", Organization, "viewer");
    public static readonly Person Outsider = new("outsider-sub", Guid.Parse("0b6f1c2d-3e4f-4a5b-8c6d-7e8f90a1b2c3"), "owner");
    public static readonly Person Unaffiliated = new("unaffiliated-sub", null, null);

    private static readonly Dictionary<string, Person> People = new[] { Owner, Editor, Viewer, Outsider, Unaffiliated }
        .ToDictionary(person => person.Subject, StringComparer.Ordinal);

    /// <summary>Settings that satisfy <c>Identity:Client</c> validation without any network call to the authority and bind the
    /// test organization.</summary>
    public static IReadOnlyDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>
    {
        ["Identity:Client:Authority"] = "https://identity.example.test/",
        ["Identity:Client:ClientId"] = "editorial",
        ["Identity:Client:CookieName"] = "__Host-ocharo-editorial",
        ["Identity:Resource:Uri"] = Resource,
        ["Blog:OrganizationId"] = Organization.ToString("D"),
    };

    /// <summary>Hands over the settings the host reads while it is being built, which <c>ConfigureAppConfiguration</c> reaches
    /// too late for.</summary>
    public static void UseBootSettings(IWebHostBuilder builder, IReadOnlyDictionary<string, string?>? settings = null)
    {
        settings ??= Settings;
        foreach (var key in new[] { "Identity:Client:Authority", "Identity:Resource:Uri" })
        {
            if (settings[key] is { } value) builder.UseSetting(key, value);
        }
    }

    /// <summary>Replaces the BFF cookie scheme's handler with the header-driven test handler.</summary>
    public static void Use(IServiceCollection services)
    {
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            foreach (var scheme in options.Schemes.Where(scheme => scheme.Name == BffAuthenticationSchemeConstants.Cookie))
                scheme.HandlerType = typeof(Handler);
        });
    }

    /// <summary>A client over HTTPS that stays anonymous.</summary>
    public static HttpClient Anonymous(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });

    /// <summary>A client over HTTPS signed in as <paramref name="person"/>; with <paramref name="csrf"/> its unsafe requests carry
    /// the session's <c>X-Csrf-Token</c>, read from the session route as the SPA does.</summary>
    public static HttpClient Client(WebApplicationFactory<Program> factory, Person person, bool csrf = true) =>
        factory.CreateDefaultClient(new Uri("https://localhost"), new SessionHandler(person, csrf));

    /// <summary>The session route's <c>data</c>.</summary>
    public static async Task<JsonElement> SessionAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/identity/session")).GetProperty("data");

    /// <summary>The permissions the deployed Identity grants a role: owner and admin edit and approve, editor edits, viewer reads.</summary>
    private static string[] Permissions(string role) => role switch
    {
        "owner" or "admin" => ["editorial:edit", "editorial:approve"],
        "editor" => ["editorial:edit"],
        _ => [],
    };

    /// <summary>Names the person on every request and adds the session's token to unsafe ones.</summary>
    private sealed class SessionHandler(Person person, bool csrf) : DelegatingHandler
    {
        private string? token;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.TryAddWithoutValidation(PersonHeader, person.Subject);
            var safe = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head || request.Method == HttpMethod.Options;
            if (csrf && !safe && !request.Headers.Contains("X-Csrf-Token"))
                request.Headers.TryAddWithoutValidation("X-Csrf-Token", token ??= await ReadTokenAsync(new Uri(request.RequestUri!, "/api/identity/session"), cancellationToken));
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> ReadTokenAsync(Uri session, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, session);
            request.Headers.TryAddWithoutValidation(PersonHeader, person.Subject);
            using var response = await base.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return body.GetProperty("data").GetProperty("csrfToken").GetString()!;
        }
    }

    private sealed class Handler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(PersonHeader, out var subject) || !People.TryGetValue(subject.ToString(), out var person))
                return Task.FromResult(AuthenticateResult.NoResult());
            var session = "sid-" + person.Subject;
            var claims = new List<Claim> { new("sub", person.Subject), new("sid", session), new("name", person.Subject) };
            if (person.OrganizationId is { } organization && person.Role is { } role)
            {
                var id = organization.ToString("D", CultureInfo.InvariantCulture);
                claims.Add(new Claim(OrganizationClaimTypeConstants.Id, id));
                claims.Add(new Claim(OrganizationClaimTypeConstants.Role, $"{id}:{role}"));
                claims.AddRange(Permissions(role).Select(permission => new Claim(OrganizationClaimTypeConstants.Permission, $"{id}:{permission}")));
            }
            // The BFF's session reference (`.bff.sid`, `.bff.ref`): the session route then issues a CSRF token bound to it,
            // and the antiforgery check demands it on unsafe API requests.
            var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
            properties.Items[".bff.sid"] = session;
            properties.Items[".bff.ref"] = "test-reference";
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name, "name", "role"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name)));
        }
    }
}
