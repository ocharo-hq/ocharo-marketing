using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoBlog.Api.RuntimeConfig.Settings;

namespace OcharoBlog.Tests.E2E;

/// <summary>An isolated Blog host with its own database and data folder, behind the test sign-in.</summary>
internal sealed class EditorialHost : IAsyncDisposable
{
    private readonly string root;
    private readonly string? ownDatabase;

    private EditorialHost(string root, string? ownDatabase, WebApplicationFactory<Program> factory)
    {
        this.root = root;
        this.ownDatabase = ownDatabase;
        Factory = factory;
    }

    public WebApplicationFactory<Program> Factory { get; }

    /// <summary>The folder the host's <c>Blog:DataRoot</c> resolves to.</summary>
    public string DataRoot => Path.Combine(root, "data");

    /// <param name="settings">Settings over the test defaults.</param>
    /// <param name="logs">Keeps the host's warnings.</param>
    /// <param name="services">Service changes after the host's own.</param>
    /// <param name="realCookie">Keeps the BFF's own cookie scheme instead of the test sign-in, so every request is
    /// anonymous.</param>
    /// <param name="webRoot">The folder the host serves files from, such as a Studio bundle; null keeps the project's.
    /// </param>
    /// <param name="database">A database another host shares, which this one leaves in place; null makes its
    /// own.</param>
    public static EditorialHost Create(IReadOnlyDictionary<string, string?> settings, CapturedLogs? logs = null,
        Action<IServiceCollection>? services = null, bool realCookie = false, string? webRoot = null,
        string? database = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "ocharo-blog-runtime-config", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var configuration = new Dictionary<string, string?>(EditorialSignIn.Settings);
        foreach (var (key, value) in settings) configuration[key] = value;
        configuration["ConnectionStrings:OcharoBlog"] = $"Data Source={Path.Combine(root, "api.db")};Pooling=False";
        var ownDatabase = database is null ? BlogDatabase.Create() : null;
        foreach (var (key, value) in BlogDatabase.Settings(database ?? ownDatabase!)) configuration[key] = value;
        configuration["Blog:DataRoot"] = Path.Combine(root, "data");
        configuration["Blog:SeedFile"] = Path.Combine(root, "no-seed.json");
        configuration["Blog:SchedulerSeconds"] = "0";
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            if (webRoot is not null) builder.UseWebRoot(webRoot);
            EditorialSignIn.UseBootSettings(builder, configuration);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));
            builder.ConfigureServices(collection =>
            {
                collection.RemoveAll<IHostedService>();
                if (!realCookie) EditorialSignIn.Use(collection);
                services?.Invoke(collection);
                // The host logs through Serilog, which reads no logging providers: the captured logs replace its
                // logger factory.
                if (logs is not null)
                    collection.AddSingleton<ILoggerFactory>(_ => LoggerFactory.Create(l => l.AddProvider(logs)));
            });
        });
        return new EditorialHost(root, ownDatabase, factory);
    }

    /// <summary>An anonymous client over HTTPS.</summary>
    public HttpClient Client() => EditorialSignIn.Anonymous(Factory);

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        if (ownDatabase is not null) await BlogDatabase.DropAsync(ownDatabase);
        try { Directory.Delete(root, true); } catch (IOException) { }
    }
}
