using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OcharoMarketing.Tests;

public sealed class StatusBoundaryTests
{
    [Theory]
    [InlineData("/api/system/status")]
    [InlineData("/API/SYSTEM/STATUS/")]
    public async Task Anonymous_ShouldSeeOnlyTechnicalStatus(string statusPath)
    {
        await using var host = MarketingHost.Create(new Dictionary<string, string?>());
        using var client = host.Client();

        using var response = await client.GetAsync(statusPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "service", "status", "version" }, status.EnumerateObject().Select(item => item.Name).Order().ToArray());
        Assert.Equal("OcharoMarketing", status.GetProperty("service").GetString());
        Assert.Equal("healthy", status.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(status.GetProperty("version").GetString()));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

        foreach (var path in new[] { "/api/blog/articles", "/api/system/status/details" })
        {
            using var businessResponse = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, businessResponse.StatusCode);
            Assert.Equal("application/problem+json", businessResponse.Content.Headers.ContentType!.MediaType);
        }
        using var write = await client.PostAsync("/api/system/status", null);
        Assert.Equal(HttpStatusCode.Unauthorized, write.StatusCode);
    }
}
