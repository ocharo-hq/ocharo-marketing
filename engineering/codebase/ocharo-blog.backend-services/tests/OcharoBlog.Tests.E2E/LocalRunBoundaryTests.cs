using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace OcharoBlog.Tests.E2E;

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
        var database = await BlogDatabase.CreateAsync();
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                if (deployEnvironment is not null) builder.UseSetting("DEPLOY_ENVIRONMENT", deployEnvironment);
                EditorialSignIn.UseBootSettings(builder);
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>(EditorialSignIn.Settings)
                {
                    ["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Blog:DataRoot"] = Path.Combine(root, "data"),
                    ["Blog:SeedFile"] = Path.Combine(root, "no-seed.json"), ["Blog:SchedulerSeconds"] = "0",
                }).AddInMemoryCollection(BlogDatabase.Settings(database)));
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    EditorialSignIn.Use(services);
                });
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false,
            });

            using var response = await client.GetAsync("/openapi/v1.json");

            response.StatusCode.Should().Be(expected);
        }
        finally
        {
            await BlogDatabase.DropAsync(database);
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
