using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoBlog.Api.RuntimeConfig.Settings;

namespace OcharoBlog.Tests;

/// <summary>Editorial’s host serves the Ocharo app addresses as public runtime settings, anonymously and uncached.</summary>
public sealed class RuntimeConfigTests
{
    private static readonly Dictionary<string, string?> EveryApp = new()
    {
        ["OcharoApps:Desk"] = "https://ocharo-desk-dev.example.ts.net/",
        ["OcharoApps:Catalogue"] = "https://ocharo-catalogue-dev.example.ts.net/catalogues",
        ["OcharoApps:Studio"] = "https://ocharo-studio-dev.example.ts.net/studio",
        ["OcharoApps:Marketing"] = "https://ocharo-marketing-dev.example.ts.net/",
        ["OcharoApps:Editorial"] = "https://ocharo-blog-dev.example.ts.net/studio/",
        ["OcharoApps:Brand"] = "https://ocharo-brand-dev.example.ts.net/",
        ["OcharoApps:Atlas"] = "https://ocharo-atlas-dev.example.ts.net/",
        ["OcharoApps:Wear"] = "https://ocharo-wear-dev.example.ts.net/",
        ["OcharoApps:Blog"] = "https://ocharo-blog-dev.example.ts.net/",
    };

    [Fact]
    public async Task RuntimeConfig_ShouldServeEveryConfiguredAddressAnonymouslyAndUncached()
    {
        await using var host = EditorialHost.Create(EveryApp);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal(new Dictionary<string, string>
        {
            ["OCHARO_DESK_URL"] = "https://ocharo-desk-dev.example.ts.net/",
            ["OCHARO_CATALOGUE_URL"] = "https://ocharo-catalogue-dev.example.ts.net/catalogues",
            ["OCHARO_STUDIO_URL"] = "https://ocharo-studio-dev.example.ts.net/studio",
            ["OCHARO_MARKETING_URL"] = "https://ocharo-marketing-dev.example.ts.net/",
            ["OCHARO_EDITORIAL_URL"] = "https://ocharo-blog-dev.example.ts.net/studio/",
            ["OCHARO_BRAND_URL"] = "https://ocharo-brand-dev.example.ts.net/",
            ["OCHARO_ATLAS_URL"] = "https://ocharo-atlas-dev.example.ts.net/",
            ["OCHARO_WEAR_URL"] = "https://ocharo-wear-dev.example.ts.net/",
            ["OCHARO_BLOG_URL"] = "https://ocharo-blog-dev.example.ts.net/",
        }, await ReadAsync(response));
    }

    [Theory]
    [InlineData("/api/runtime-config")]
    [InlineData("/API/RUNTIME-CONFIG/")]
    [InlineData("/Api/Runtime-Config")]
    public async Task RuntimeConfig_ShouldAnswerWithoutASession_WhenSsoGuardsTheRestOfTheApi(string path)
    {
        await using var host = EditorialHost.Create(EveryApp, sso: true);
        using var client = host.Client(handleCookies: false);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", "__Host-ocharo-editorial=forged");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Contains("OCHARO_DESK_URL", (await ReadAsync(response)).Keys);
        // The SSO boundary still holds for the private API.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/blog/articles")).StatusCode);
    }

    [Theory]
    [InlineData("/health/")]
    [InlineData("/API/SYSTEM/STATUS/")]
    [InlineData("/api/runtime-config/")]
    public async Task TechnicalRoutes_ShouldAnswerTheSameVariantsThroughBothGates_WhenSsoIsOn(string path)
    {
        await using var host = EditorialHost.Create(EveryApp, sso: true);
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/runtime-config")]
    [InlineData("/API/RUNTIME-CONFIG/")]
    public async Task RuntimeConfig_ShouldAnswerInUnconfiguredConnectedMode_WhileBusinessApisFailClosed(string path)
    {
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(EveryApp)
        {
            ["PlatformIntegration:Mode"] = "connected",
            ["PlatformIntegration:BaseUrl"] = "https://desk.test",
            ["PlatformIntegration:OrganizationId"] = "",
        });
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("https://ocharo-desk-dev.example.ts.net/", (await ReadAsync(response))["OCHARO_DESK_URL"]);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/blog/articles")).StatusCode);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.Unauthorized)]
    [InlineData(false, HttpStatusCode.ServiceUnavailable)]
    public async Task RuntimeConfig_ShouldStayClosedToWrites(bool sso, HttpStatusCode expected)
    {
        var settings = new Dictionary<string, string?>(EveryApp);
        if (!sso)
        {
            settings["PlatformIntegration:Mode"] = "connected";
            settings["PlatformIntegration:BaseUrl"] = "https://desk.test";
            settings["PlatformIntegration:OrganizationId"] = "";
        }
        await using var host = EditorialHost.Create(settings, sso);
        using var client = host.Client();

        using var response = await client.PostAsync("/api/runtime-config", null);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RuntimeConfig_ShouldKeepTheLoopbackPilotBoundary(bool sso)
    {
        await using var host = EditorialHost.Create(EveryApp, sso, remoteAddress: IPAddress.Parse("192.0.2.1"));
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("ocharo-desk-dev", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RuntimeConfig_ShouldDropEveryAddressThatIsNotAPlainHttpsLink_AndWarnOnceAtStartup()
    {
        var logs = new CapturedLogs();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>
        {
            ["OcharoApps:Desk"] = "http://ocharo-desk-dev.example.ts.net/",
            ["OcharoApps:Catalogue"] = "https://operator:secret@ocharo-catalogue-dev.example.ts.net/",
            ["OcharoApps:Studio"] = "https://ocharo-studio-dev.example.ts.net/studio?token=abc",
            ["OcharoApps:Marketing"] = "https://ocharo-marketing-dev.example.ts.net/#access_token=abc",
            ["OcharoApps:Editorial"] = "/studio/",
            ["OcharoApps:Brand"] = "not a link",
            ["OcharoApps:Atlas"] = "https://ocharo-atlas-dev.example.ts.net/",
            ["OcharoApps:Wear"] = "",
            ["OcharoApps:Blog"] = "javascript:alert(1)",
        }, logs: logs);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");
        await client.GetAsync("/api/runtime-config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Dictionary<string, string> { ["OCHARO_ATLAS_URL"] = "https://ocharo-atlas-dev.example.ts.net/" }, await ReadAsync(response));
        var warning = Assert.Single(logs.Warnings, message => message.StartsWith("Runtime config leaves out", StringComparison.Ordinal));
        Assert.Contains("OcharoApps:Desk, OcharoApps:Catalogue, OcharoApps:Studio, OcharoApps:Marketing, OcharoApps:Editorial, OcharoApps:Brand, OcharoApps:Blog", warning);
        Assert.DoesNotContain("secret", warning);
    }

    [Fact]
    public async Task RuntimeConfig_ShouldServeAnEmptyObjectWithoutAWarning_WhenNoAddressIsConfigured()
    {
        var logs = new CapturedLogs();
        await using var host = EditorialHost.Create(new Dictionary<string, string?>(), logs: logs);
        using var client = host.Client();

        using var response = await client.GetAsync("/api/runtime-config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal("{}", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(logs.Warnings, message => message.StartsWith("Runtime config", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://ocharo-desk-dev.example.ts.net/", true)]
    [InlineData("https://desk.example.com:8443/organizations", true)]
    [InlineData("https://example.com/@handle", true)]
    [InlineData("http://desk.example.com/", false)]
    [InlineData("https://user@desk.example.com/", false)]
    [InlineData("https://desk.example.com/?", false)]
    [InlineData("https://desk.example.com/#", false)]
    [InlineData("https://desk.example.com/a b", false)]
    [InlineData(" https://desk.example.com/", false)]
    [InlineData("//desk.example.com/", false)]
    [InlineData("/organizations", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPublicAddress_ShouldAcceptOnlyPlainAbsoluteHttpsLinks(string? value, bool accepted)
    {
        Assert.Equal(accepted, OcharoAppsSettingsExtensions.IsPublicAddress(value));
    }

    [Fact]
    public void RejectedSettings_ShouldNameConfiguredAddressesItLeavesOut()
    {
        var settings = new OcharoAppsSettings { Desk = "http://desk.example.com/", Atlas = "https://atlas.example.com/", Wear = " " };

        Assert.Equal(["OcharoApps:Desk"], settings.RejectedSettings());
    }

    private static async Task<Dictionary<string, string>> ReadAsync(HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(await response.Content.ReadAsStringAsync())!;
    }
}

/// <summary>An isolated Blog host with its own SQLite file and data folder, optionally behind enabled SSO.</summary>
internal sealed class EditorialHost : IAsyncDisposable
{
    private readonly string root;

    private EditorialHost(string root, WebApplicationFactory<Program> factory)
    {
        this.root = root;
        Factory = factory;
    }

    public WebApplicationFactory<Program> Factory { get; }

    public static EditorialHost Create(
        IReadOnlyDictionary<string, string?> settings, bool sso = false, CapturedLogs? logs = null, IPAddress? remoteAddress = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-blog-runtime-config", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var configuration = new Dictionary<string, string?>
        {
            ["PlatformIntegration:Mode"] = "local",
            ["Identity:Sso:Enabled"] = "false",
        };
        foreach (var (key, value) in settings) configuration[key] = value;
        configuration["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False";
        configuration["Blog:DataRoot"] = Path.Combine(root, "data");
        configuration["Blog:SeedFile"] = Path.Combine(root, "no-seed.json");
        configuration["Blog:RunScheduler"] = "false";
        if (sso)
        {
            configuration["Identity:Sso:Enabled"] = "true";
            configuration["Identity:Sso:DeskOrigin"] = "https://desk.test";
            configuration["Identity:Sso:ClientOrigin"] = "https://localhost";
            configuration["Identity:Sso:ClientId"] = "fixture";
            configuration["Identity:Sso:ClientSecret"] = new string('s', 32);
            configuration["Identity:Sso:OrganizationId"] = Guid.NewGuid().ToString();
        }
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                if (remoteAddress is not null) services.AddSingleton<IStartupFilter>(new RemoteAddressFilter(remoteAddress));
            });
            if (logs is not null) builder.ConfigureLogging(logging => logging.AddProvider(logs));
        });
        return new EditorialHost(root, factory);
    }

    public HttpClient Client(bool handleCookies = true)
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = handleCookies,
        });
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        try { Directory.Delete(root, true); } catch (IOException) { }
    }
}

/// <summary>Makes every request arrive from one remote address, ahead of the host's own middleware.</summary>
internal sealed class RemoteAddressFilter(IPAddress address) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((context, following) =>
            {
                context.Connection.RemoteIpAddress = address;
                return following(context);
            });
            next(app);
        };
    }
}

/// <summary>Keeps every warning the host logs, so a test can count the startup warnings.</summary>
internal sealed class CapturedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> warnings = new();

    public IReadOnlyCollection<string> Warnings => warnings.ToArray();

    public ILogger CreateLogger(string categoryName) => new Logger(warnings);

    public void Dispose()
    {
    }

    private sealed class Logger(ConcurrentQueue<string> warnings) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) warnings.Enqueue(formatter(state, exception));
        }
    }
}
