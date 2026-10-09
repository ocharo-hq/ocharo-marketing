using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace OcharoMarketing.Tests;

public sealed class StatusBoundaryTests
{
    [Theory]
    [InlineData("/api/system/status")]
    [InlineData("/API/SYSTEM/STATUS/")]
    public async Task UnconfiguredConnectedMode_ShouldExposeOnlyTechnicalStatus(string statusPath)
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-status-host", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PlatformIntegration:Mode"] = "connected",
                    ["PlatformIntegration:BaseUrl"] = "https://desk.test",
                    ["PlatformIntegration:OrganizationId"] = "",
                    ["Identity:Sso:Enabled"] = "false",
                    ["ConnectionStrings:OcharoMarketing"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Marketing:DataDirectory"] = root, ["Marketing:LocalPilot"] = "true",
                }));
                builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false,
            });
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
                Assert.Equal(HttpStatusCode.ServiceUnavailable, businessResponse.StatusCode);
                Assert.Equal("application/problem+json", businessResponse.Content.Headers.ContentType!.MediaType);
            }
            using var write = await client.PostAsync("/api/system/status", null);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, write.StatusCode);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
