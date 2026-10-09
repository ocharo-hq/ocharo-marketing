using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace OcharoBlog.Tests;

public sealed class SsoHostTests
{
    [Fact]
    public async Task EnabledSso_ShouldProtectOperatorApiAndKeepHealthAvailable()
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-sso-host", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?>
                {
                    ["PlatformIntegration:Mode"] = "local",
                    ["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False",
                    ["Blog:DataRoot"] = Path.Combine(root, "data"), ["Blog:SeedFile"] = Path.Combine(root, "no-seed.json"), ["Blog:RunScheduler"] = "false",
                    ["Identity:Sso:Enabled"] = "true", ["Identity:Sso:DeskOrigin"] = "https://desk.test",
                    ["Identity:Sso:ClientOrigin"] = "https://localhost", ["Identity:Sso:ClientId"] = "fixture",
                    ["Identity:Sso:ClientSecret"] = new string('s', 32), ["Identity:Sso:OrganizationId"] = Guid.NewGuid().ToString()
                }));
                builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            using var privateResponse = await client.GetAsync("/api/integrations/session");
            Assert.Equal(HttpStatusCode.Unauthorized, privateResponse.StatusCode);
            Assert.Equal("application/problem+json", privateResponse.Content.Headers.ContentType!.MediaType);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
            using var signIn = await client.GetAsync("/api/sso/login?returnUrl=/");
            Assert.Equal(HttpStatusCode.Redirect, signIn.StatusCode);
            Assert.StartsWith("https://desk.test/api/sso/authorize?", signIn.Headers.Location!.OriginalString);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/media/" + new string('a', 64))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/preview/" + Guid.NewGuid())).StatusCode);
        }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }
}
