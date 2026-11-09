using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OcharoMarketing.Api.RuntimeConfig.Settings;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace OcharoMarketing.Tests;

/// <summary>Marketing serves the Ocharo app addresses as public runtime settings, anonymously and uncached.</summary>
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
        await using var host = await MarketingHost.CreateAsync(EveryApp);
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
    public async Task RuntimeConfig_ShouldAnswerWithoutASession_WhileSignInGuardsTheRestOfTheApi(string path)
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"{MarketingTestIdentity.CookieName}=forged");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Contains("OCHARO_DESK_URL", (await ReadAsync(response)).Keys);
        // Sign-in still guards the private API.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/marketing/workspace")).StatusCode);
    }

    [Theory]
    [InlineData("/health/")]
    [InlineData("/API/SYSTEM/STATUS/")]
    [InlineData("/api/runtime-config/")]
    public async Task TechnicalRoutes_ShouldAnswerEveryVariantWithoutASession(string path)
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RuntimeConfig_ShouldStayClosedToWrites()
    {
        await using var host = await MarketingHost.CreateAsync(EveryApp);
        using var client = host.Client();

        using var response = await client.PostAsync("/api/runtime-config", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RuntimeConfig_ShouldDropEveryAddressThatIsNotAPlainHttpsLink_AndWarnOnceAtStartup()
    {
        var logs = new CapturedLogs();
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>
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
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>(), logs: logs);
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

/// <summary>An isolated Marketing host with its own PostgreSQL database (<see cref="MarketingPostgres"/>) and data
/// folder, signed in through <see cref="MarketingTestIdentity"/>; <c>settings</c> apply last, so a test may change or
/// clear (null) any default.</summary>
internal sealed class MarketingHost : IAsyncDisposable
{
    private MarketingHost(string root, string connectionString, WebApplicationFactory<Program> factory)
    {
        Root = root;
        ConnectionString = connectionString;
        Factory = factory;
    }

    /// <summary>The host's data folder (<c>Marketing:DataDirectory</c>).</summary>
    public string Root { get; }

    /// <summary>The host's database.</summary>
    public string ConnectionString { get; }

    public WebApplicationFactory<Program> Factory { get; }

    public static async Task<MarketingHost> CreateAsync(
        IReadOnlyDictionary<string, string?> settings, CapturedLogs? logs = null, bool realCookieScheme = false,
        string? connectionString = null, string? root = null)
    {
        root ??= Path.Combine(Path.GetTempPath(), "ocharo-marketing-host", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        connectionString ??= await MarketingPostgres.CreateDatabaseAsync();
        var configuration = MarketingTestIdentity.Settings();
        foreach (var (key, value) in settings) configuration[key] = value;
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("DatabaseSettings:ConnectionString", connectionString);
            builder.UseSetting("Marketing:DataDirectory", root);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                if (!realCookieScheme) MarketingTestIdentity.Use(services);
                // The host logs through Serilog, which reads its sinks from the services.
                if (logs is not null) services.AddSingleton<ILogEventSink>(logs);
            });
        });
        return new MarketingHost(root, connectionString, factory);
    }

    /// <summary>A host on a database at the level of every set; the boot's own migration finds it current.</summary>
    public static Task<MarketingHost> CreateWithDatabaseAsync() => CreateAsync(new Dictionary<string, string?>());

    /// <summary>An anonymous client over HTTPS.</summary>
    public HttpClient Client() => MarketingTestIdentity.Anonymous(Factory);

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await MarketingPostgres.DropDatabaseAsync(ConnectionString);
        try { Directory.Delete(Root, true); } catch (IOException) { }
    }
}

/// <summary>Keeps every warning the host logs, so a test can count the startup warnings.</summary>
internal sealed class CapturedLogs : ILogEventSink
{
    private readonly ConcurrentQueue<string> warnings = new();

    public IReadOnlyCollection<string> Warnings => warnings.ToArray();

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level == LogEventLevel.Warning) warnings.Enqueue(logEvent.RenderMessage());
    }
}
