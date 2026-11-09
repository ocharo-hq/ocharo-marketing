using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using OcharoMarketing.Api.Configurations;
using OcharoMarketing.Infrastructure.Automation;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Persistence.Import;
using Xunit;
using static OcharoMarketing.Tests.MarketingTestIdentity;

namespace OcharoMarketing.Tests.Import;

/// <summary>
/// Until <c>import-sqlite</c> retires the store an earlier release kept, nothing may write into the schema it must find
/// empty: data routes answer 503 <c>import_pending</c> while health, status, runtime config and the session routes stay
/// open, and the seed, the Files backfill and the automation pass skip their work. With no store, the seed runs as
/// before.
/// </summary>
public sealed class ImportGateTests : IAsyncLifetime
{
    private readonly string root =
        Path.Combine(Path.GetTempPath(), "ocharo-marketing-gate", Guid.NewGuid().ToString("N"));

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(root);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DataRoutes_ShouldAnswer503ImportPending_WhileTheStoreWaits()
    {
        await File.WriteAllBytesAsync(Path.Combine(root, "marketing.db"), []);
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>(), root: root);
        using var anonymous = host.Client();
        using var member = await SignedIn(host.Factory, Owner);

        using var workspace = await member.GetAsync("/api/marketing/workspace");
        using var inquiry = await anonymous.PostAsJsonAsync("/api/public/inquiries", new { name = "Reader" });

        foreach (var response in new[] { workspace, inquiry })
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("import_pending", await CodeAsync(response));
        }
        foreach (var path in new[] { "/health", "/api/system/status", "/api/runtime-config" })
            Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync("/api/identity/session")).StatusCode);
    }

    [Fact]
    public async Task Writers_ShouldLeaveTheSchemaEmpty_WhileTheStoreWaits()
    {
        await File.WriteAllBytesAsync(Path.Combine(root, "marketing.db"), []);
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>(), root: root);
        var services = host.Factory.Services;

        await ActivatorUtilities.CreateInstance<SeedImportHostedService>(services).StartAsync(default);
        await ActivatorUtilities.CreateInstance<FilesBackfillHostedService>(services).StartAsync(default);
        using (var scope = services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AutomationRunner>().RunScheduledAsync(default);

        Assert.Equal(0L, await RowsAsync(host.ConnectionString));
    }

    [Fact]
    public async Task Seed_ShouldRunAsBefore_WithoutAStore()
    {
        await using var host = await MarketingHost.CreateAsync(new Dictionary<string, string?>(), root: root);

        await ActivatorUtilities.CreateInstance<SeedImportHostedService>(host.Factory.Services).StartAsync(default);

        Assert.True(await RowsAsync(host.ConnectionString) > 0);
    }

    [Fact]
    public void Gate_ShouldOpenOnceTheStoreIsRetired_AfterItsCheckInterval()
    {
        var store = Path.Combine(root, "marketing.db");
        File.WriteAllBytes(store, []);
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var gate = new SqliteImportGate(new SqliteImportSource(store), time);

        Assert.True(gate.IsPending);
        File.Move(store, Path.Combine(root, "marketing.imported-20261109120000.db"));
        Assert.True(gate.IsPending);
        time.Advance(SqliteImportGate.CheckEvery);
        Assert.False(gate.IsPending);
    }

    /// <summary>Counts the rows of every table the import copies into.</summary>
    private static async Task<long> RowsAsync(string connectionString)
    {
        var counts = SqliteImportTables.All.Select(table => $"(SELECT COUNT(*) FROM marketing.{table.Target})");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT {string.Join(" + ", counts)}", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
