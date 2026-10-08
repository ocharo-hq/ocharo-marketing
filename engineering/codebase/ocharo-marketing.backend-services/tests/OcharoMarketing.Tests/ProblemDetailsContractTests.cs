using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace OcharoMarketing.Tests;

/// <summary>Real HTTP error contracts over isolated temporary stores.</summary>
public sealed class ProblemDetailsContractTests
{
    [Theory]
    [InlineData("GET", "/api/missing-contract-route", null, null, 404)]
    [InlineData("PATCH", "/health", null, null, 405)]
    [InlineData("POST", "/api/marketing/drafts", "application/json", "{", 400)]
    [InlineData("POST", "/api/marketing/drafts", "text/plain", "invalid", 415)]
    public async Task ErrorsRemainProblemJsonWhenHtmlIsRequested(string method, string path, string? contentType, string? body, int expected)
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-problem-contract", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PlatformIntegration:Mode"] = "local",
                    ["ConnectionStrings:OcharoMarketing"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Marketing:DataDirectory"] = root,
                    ["Marketing:LocalPilot"] = "true",
                }));
                builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
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
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }
}
