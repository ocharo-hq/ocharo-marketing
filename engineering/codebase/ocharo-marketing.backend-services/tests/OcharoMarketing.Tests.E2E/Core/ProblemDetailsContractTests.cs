using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using OcharoMarketing.Tests.Support;

namespace OcharoMarketing.Tests.E2E.Core;

/// <summary>Real HTTP error contracts over isolated temporary stores.</summary>
public sealed class ProblemDetailsContractTests
{
    [Theory]
    [InlineData("GET", "/api/missing-contract-route", null, null, 404)]
    [InlineData("POST", "/api/marketing/drafts", "application/json", "{", 400)]
    [InlineData("POST", "/api/marketing/drafts", "text/plain", "invalid", 415)]
    public async Task Errors_ShouldStayProblemJson_WhenHtmlIsRequested(
        string method,
        string path,
        string? contentType,
        string? body,
        int expected)
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>());
        using var client = await MarketingTestIdentity.SignedIn(host.Factory, MarketingTestIdentity.Owner);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Accept.ParseAdd("text/html");
        if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, contentType!);
        using var response = await client.SendAsync(request);
        ((int)response.StatusCode).Should().Be(expected);
        (response.Content.Headers.ContentType?.MediaType).Should().Be("application/problem+json");
        (response.Headers.CacheControl?.NoStore).Should().BeTrue();
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        (problem.GetProperty("status").GetInt32()).Should().Be(expected);
        (string.IsNullOrWhiteSpace(problem.GetProperty("type").GetString())).Should().BeFalse();
        (string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString())).Should().BeFalse();
        (problem.GetProperty("instance").GetString()).Should().Be(path);
        (string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString())).Should().BeFalse();
        (string.IsNullOrWhiteSpace(problem.GetProperty("requestId").GetString())).Should().BeFalse();
    }
}
