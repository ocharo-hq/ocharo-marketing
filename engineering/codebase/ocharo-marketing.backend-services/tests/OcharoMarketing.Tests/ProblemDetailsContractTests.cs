using System.Net;
using System.Text.Json;

namespace OcharoMarketing.Tests;

/// <summary>Real HTTP error contracts over isolated temporary stores.</summary>
public sealed class ProblemDetailsContractTests
{
    [Theory]
    [InlineData("GET", "/api/missing-contract-route", null, null, 404)]
    [InlineData("POST", "/api/marketing/drafts", "application/json", "{", 400)]
    [InlineData("POST", "/api/marketing/drafts", "text/plain", "invalid", 415)]
    public async Task ErrorsRemainProblemJsonWhenHtmlIsRequested(string method, string path, string? contentType, string? body, int expected)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await MarketingTestIdentity.SignedIn(host.Factory, MarketingTestIdentity.Owner);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Accept.ParseAdd("text/html");
        if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, contentType!);
        using var response = await client.SendAsync(request);
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(expected, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.Equal(path, problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("requestId").GetString()));
    }
}
