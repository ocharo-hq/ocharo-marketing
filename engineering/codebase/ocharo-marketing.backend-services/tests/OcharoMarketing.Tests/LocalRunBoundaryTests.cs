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

/// <summary>
/// The pilot starts on a local run, or on a public host: the owner opened deployed hosts to the public on 2026-10-24,
/// until the Ocharo identity server lands. Deployed dev also runs as Development, so only a local run counts as local.
/// </summary>
public sealed class LocalRunBoundaryTests
{
    [Theory]
    [InlineData("Development", "dev", "false")]
    [InlineData("Production", null, null)]
    [InlineData("Production", "prod", "false")]
    public void HostThatIsNeitherLocalNorPublic_ShouldRefusePilotStartup(string environment, string? deployEnvironment, string? publicAccess)
    {
        using var host = new MarketingHost(environment, deployEnvironment, publicAccess);

        var error = Assert.Throws<InvalidOperationException>(() => host.Client());

        Assert.Contains("local Development run", error.Message);
    }

    /// <remarks>Production's default integration mode is <c>local</c>: private routes answer without sign-in. A Development
    /// host defaults to <c>connected</c>, so its private routes still ask for a Platform ticket past the open boundary.</remarks>
    [Theory]
    [InlineData("Production", "prod", null, "local")]
    [InlineData("Production", null, "true", "local")]
    [InlineData("Development", "dev", null, "connected")]
    public async Task PublicHost_ShouldServeRemoteCallers(string environment, string? deployEnvironment, string? publicAccess, string mode)
    {
        using var host = new MarketingHost(environment, deployEnvironment, publicAccess);
        using var client = host.Client();

        using var health = await client.GetAsync("/health");
        using var session = await client.GetAsync("/api/integrations/session");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        var body = await session.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(mode, body.GetProperty("mode").GetString());
        Assert.Equal(mode == "local", body.GetProperty("active").GetBoolean());
    }

    [Theory]
    [InlineData("https://evil.example", true)]
    [InlineData("http://marketing.example", true)]
    [InlineData(MarketingHost.Origin, false)]
    public async Task PublicHost_ShouldStillRefuseCrossOriginWrites(string origin, bool refused)
    {
        using var host = new MarketingHost("Production", "prod", null);
        using var client = host.Client();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/integrations/connect") { Content = JsonContent.Create(new { ticket = "x" }) };
        request.Headers.Add("Origin", origin);

        using var response = await client.SendAsync(request);

        Assert.Equal(refused, response.StatusCode == HttpStatusCode.Forbidden);
    }

    /// <summary>A Marketing host on a remote address, in one environment, with an isolated store.</summary>
    private sealed class MarketingHost : IDisposable
    {
        public const string Origin = "https://marketing.example";

        private readonly string root = Path.Combine(Path.GetTempPath(), "ocharo-local-run-host", Guid.NewGuid().ToString("N"));
        private readonly WebApplicationFactory<Program> factory;

        public MarketingHost(string environment, string? deployEnvironment, string? publicAccess)
        {
            Directory.CreateDirectory(root);
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                if (deployEnvironment is not null) builder.UseSetting("DEPLOY_ENVIRONMENT", deployEnvironment);
                if (publicAccess is not null) builder.UseSetting("Marketing:PublicAccess", publicAccess);
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AllowedHosts"] = "marketing.example;localhost",
                    ["Identity:Sso:Enabled"] = "false",
                    ["ConnectionStrings:OcharoMarketing"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Marketing:DataDirectory"] = root,
                }));
                builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
            });
        }

        public HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(Origin), AllowAutoRedirect = false,
        });

        public void Dispose()
        {
            factory.Dispose();
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
