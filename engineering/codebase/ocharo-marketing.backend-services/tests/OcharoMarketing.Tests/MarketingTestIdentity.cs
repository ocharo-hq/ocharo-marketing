using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;

namespace OcharoMarketing.Tests;

/// <summary>
/// What the Marketing test hosts replace for sign-in: a stand-in for the Identity BFF cookie scheme. A request whose session
/// cookie (<see cref="CookieName"/>) names one of the <see cref="People"/> is signed in with the claims Identity's token
/// carries and a session reference, so the BFF's <c>X-Csrf-Token</c> check holds as for a real session; any other request is
/// anonymous. No call reaches the Identity instance. The session travels in the cookie, as in production, so a test sees
/// what the host does to that cookie.
/// </summary>
internal static class MarketingTestIdentity
{
    /// <summary>The session cookie's name in the test hosts.</summary>
    public const string CookieName = "__Host-ocharo-marketing";

    /// <summary>The organization the test hosts bind (<c>Marketing:OrganizationId</c>).</summary>
    public static readonly Guid Organization = Guid.Parse("7c1e4b2a-9d3f-4e6a-8b5c-2f1a0d9e8c7b");

    /// <summary>A signed-in person; a null <paramref name="OrganizationId"/> carries no organization claims.</summary>
    public sealed record Person(string Subject, Guid? OrganizationId, string? Role, string Name, string Email);

    public static readonly Person Owner = new("owner-sub", Organization, "owner", "Malika Owner", "owner@example.test");
    public static readonly Person Editor = new("editor-sub", Organization, "editor", "Aziz Editor", "editor@example.test");
    public static readonly Person Viewer = new("viewer-sub", Organization, "viewer", "Nodira Viewer", "viewer@example.test");
    public static readonly Person Outsider = new("outsider-sub", Guid.Parse("1d2e3f40-5a6b-4c7d-8e9f-0a1b2c3d4e5f"), "owner", "Other Owner", "other@example.test");
    public static readonly Person Unaffiliated = new("unaffiliated-sub", null, null, "No Organization", "none@example.test");

    private static readonly Dictionary<string, Person> People = new[] { Owner, Editor, Viewer, Outsider, Unaffiliated }
        .ToDictionary(person => person.Subject, StringComparer.Ordinal);

    /// <summary>Settings that satisfy <c>Identity:Client</c> validation without any network call to the authority, and bind
    /// the test organization.</summary>
    public static Dictionary<string, string?> Settings() => new()
    {
        ["Identity:Client:Authority"] = "https://identity.example.test/",
        ["Identity:Client:ClientId"] = "marketing",
        ["Identity:Client:CookieName"] = CookieName,
        ["Marketing:OrganizationId"] = Organization.ToString(),
    };

    /// <summary>Replaces the BFF cookie scheme's handler with the cookie-driven test handler.</summary>
    public static void Use(IServiceCollection services)
    {
        services.PostConfigure<AuthenticationOptions>(options =>
        {
            foreach (var scheme in options.Schemes.Where(scheme => scheme.Name == BffAuthenticationSchemeConstants.Cookie))
                scheme.HandlerType = typeof(Handler);
        });
    }

    /// <summary>A client over HTTPS that stays anonymous.</summary>
    public static HttpClient Anonymous(WebApplicationFactory<Program> factory, string baseAddress = "https://localhost") =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri(baseAddress), HandleCookies = false, AllowAutoRedirect = false });

    /// <summary>A client over HTTPS carrying <paramref name="person"/>'s session cookie; with <paramref name="csrf"/> it sends
    /// the session's <c>X-Csrf-Token</c>, read from the session route as the SPA does, on every request.</summary>
    public static async Task<HttpClient> SignedIn(WebApplicationFactory<Program> factory, Person person, bool csrf = true)
    {
        var client = Anonymous(factory);
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieName}={person.Subject}");
        if (csrf)
            client.DefaultRequestHeaders.Add("X-Csrf-Token", (await SessionAsync(client)).GetProperty("csrfToken").GetString());
        return client;
    }

    /// <summary>The session route's <c>data</c>.</summary>
    public static async Task<JsonElement> SessionAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/identity/session")).GetProperty("data");

    /// <summary>The principal Identity's token gives <paramref name="person"/>.</summary>
    public static ClaimsPrincipal Principal(Person person, string authenticationType = "test")
    {
        var claims = new List<Claim>
        {
            new("sub", person.Subject), new("sid", "sid-" + person.Subject), new("name", person.Name), new("email", person.Email),
        };
        if (person.OrganizationId is { } organization && person.Role is { } role)
        {
            var id = organization.ToString("D", CultureInfo.InvariantCulture);
            claims.Add(new Claim(OrganizationClaimTypeConstants.Id, id));
            claims.Add(new Claim(OrganizationClaimTypeConstants.Role, $"{id}:{role}"));
            claims.Add(new Claim(OrganizationClaimTypeConstants.Name, "Ocharo"));
            claims.Add(new Claim(OrganizationClaimTypeConstants.Slug, "ocharo"));
            claims.AddRange(Permissions(role).Select(permission => new Claim(OrganizationClaimTypeConstants.Permission, $"{id}:{permission}")));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType, "name", "role"));
    }

    /// <summary>The permissions the deployed Identity grants a role: owner and admin edit and approve, editor edits, viewer reads.</summary>
    private static string[] Permissions(string role) => role switch
    {
        "owner" or "admin" => ["marketing:edit", "marketing:approve"],
        "editor" => ["marketing:edit"],
        _ => [],
    };

    private sealed class Handler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Cookies.TryGetValue(CookieName, out var subject) || !People.TryGetValue(subject, out var person))
                return Task.FromResult(AuthenticateResult.NoResult());
            // The BFF's session reference (`.bff.sid`, `.bff.ref`): the session route then issues a CSRF token bound to it,
            // and the antiforgery check demands it on unsafe API requests.
            var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) };
            properties.Items[".bff.sid"] = "sid-" + person.Subject;
            properties.Items[".bff.ref"] = "test-reference";
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Principal(person, Scheme.Name), properties, Scheme.Name)));
        }
    }
}
