using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OcharoBlog.Tests.E2E;

public sealed class StatusBoundaryTests
{
    [Theory]
    [InlineData("/api/system/status")]
    [InlineData("/API/SYSTEM/STATUS/")]
    public async Task Anonymous_ShouldSeeOnlyTechnicalStatus(string statusPath)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>());
        using var client = EditorialSignIn.Anonymous(host.Factory);

        using var response = await client.GetAsync(statusPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>();
        status.EnumerateObject()
            .Select(item => item.Name)
            .Order()
            .ToArray()
            .Should()
            .Equal(new[] { "service", "status", "version" });
        status.GetProperty("service").GetString().Should().Be("OcharoBlog");
        status.GetProperty("status").GetString().Should().Be("healthy");
        string.IsNullOrWhiteSpace(status.GetProperty("version").GetString()).Should().BeFalse();
        ((await client.GetAsync("/health")).StatusCode).Should().Be(HttpStatusCode.OK);

        foreach (var path in new[] { "/api/blog/articles", "/api/system/status/details" })
        {
            using var businessResponse = await client.GetAsync(path);
            businessResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            businessResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }
        using var write = await client.PostAsync("/api/system/status", null);
        write.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
