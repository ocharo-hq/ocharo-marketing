using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OcharoMarketing.Tests.Support;
using Serilog.Core;

namespace OcharoMarketing.Tests.E2E;

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
