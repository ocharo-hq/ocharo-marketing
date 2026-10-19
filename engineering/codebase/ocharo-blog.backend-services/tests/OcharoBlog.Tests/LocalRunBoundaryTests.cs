using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace OcharoBlog.Tests;

/// <summary>Local-only conveniences key on a local run; deployed dev also runs as Development.</summary>
public sealed class LocalRunBoundaryTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.OK)]
    [InlineData("dev", HttpStatusCode.NotFound)]
    public async Task OpenApiDocument_ShouldBeMappedOnlyOnALocalRun(string? deployEnvironment, HttpStatusCode expected)
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-local-run-host", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                if (deployEnvironment is not null) builder.UseSetting("DEPLOY_ENVIRONMENT", deployEnvironment);
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PlatformIntegration:Mode"] = "local",
                    ["Identity:Sso:Enabled"] = "false",
                    ["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Blog:DataRoot"] = Path.Combine(root, "data"),
                    ["Blog:SeedFile"] = Path.Combine(root, "no-seed.json"), ["Blog:RunScheduler"] = "false",
                }));
                builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false,
            });

            using var response = await client.GetAsync("/openapi/v1.json");

            Assert.Equal(expected, response.StatusCode);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
