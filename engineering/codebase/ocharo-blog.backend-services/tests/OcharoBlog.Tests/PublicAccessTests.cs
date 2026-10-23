using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OcharoBlog.Tests;

/// <summary>
/// The owner opened the deployed Editorial host to the public on 2026-10-24, until the Ocharo identity server lands: a
/// deployed host accepts remote callers without sign-in, a local run keeps the loopback boundary, and
/// <c>Blog:PublicAccess</c> overrides either way. Cross-origin writes stay refused.
/// </summary>
public sealed class PublicAccessTests
{
    private static readonly IPAddress Remote = IPAddress.Parse("192.0.2.1");

    [Theory]
    [InlineData(null, null, HttpStatusCode.Forbidden)]
    [InlineData("dev", null, HttpStatusCode.OK)]
    [InlineData("prod", null, HttpStatusCode.OK)]
    [InlineData("prod", "false", HttpStatusCode.Forbidden)]
    [InlineData(null, "true", HttpStatusCode.OK)]
    public async Task RemoteCaller_ShouldReachTheEditorialApi_OnlyOnAPublicHost(string? deployEnvironment, string? publicAccess, HttpStatusCode expected)
    {
        var settings = new Dictionary<string, string?>();
        if (deployEnvironment is not null) settings["DEPLOY_ENVIRONMENT"] = deployEnvironment;
        if (publicAccess is not null) settings["Blog:PublicAccess"] = publicAccess;
        await using var host = EditorialHost.Create(settings, remoteAddress: Remote);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/blog/articles");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("https://evil.example", true)]
    [InlineData("http://localhost", true)]
    [InlineData("https://localhost", false)]
    public async Task PublicHost_ShouldStillRefuseCrossOriginWrites(string origin, bool refused)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?> { ["DEPLOY_ENVIRONMENT"] = "prod" }, remoteAddress: Remote);
        using var client = host.Client();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/blog/articles") { Content = JsonContent.Create(new { }) };
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);

        Assert.Equal(refused, response.StatusCode == HttpStatusCode.Forbidden);
    }
}
