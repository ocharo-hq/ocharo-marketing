using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace OcharoMarketing.Tests;

/// <summary>The loopback pilot starts only on a local run; deployed dev also runs as Development.</summary>
public sealed class LocalRunBoundaryTests
{
    [Fact]
    public void DeployedDevelopmentHost_ShouldRefusePilotStartup()
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-local-run-host", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("DEPLOY_ENVIRONMENT", "dev");
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:OcharoMarketing"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Marketing:DataDirectory"] = root,
                }));
                builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
            });

            var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

            Assert.Contains("local Development run", error.Message);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
