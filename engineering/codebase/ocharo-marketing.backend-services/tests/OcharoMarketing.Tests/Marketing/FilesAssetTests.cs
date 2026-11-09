using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OcharoMarketing.Api.Configurations;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Persistence;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using Xunit;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Marketing on WoW2 Files (<c>Files:BaseUrl</c> set): assets land in Files once per SHA-256 as app
/// <c>marketing</c>, the asset route redirects to a short-lived Files link, server-side reads come back from Files checked
/// against their hash, and each approved revision's export ZIP is a private Files file with the same bytes on every call.
/// An asset or export made before Files reads from the local blob folder until the backfill fills its id. Files itself is
/// <see cref="TestFiles"/>, an in-memory broker set.</summary>
public sealed class FilesAssetTests : IAsyncDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ocharo-marketing-files-tests", Guid.NewGuid().ToString("N"));
    private readonly List<IAsyncDisposable> hosts = [];
    private string? database;

    public async ValueTask DisposeAsync()
    {
        foreach (var host in Enumerable.Reverse(hosts))
            await host.DisposeAsync();
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    [Fact]
    public async Task An_asset_lands_in_files_once_and_reads_back_through_files()
    {
        var files = new TestFiles();
        var services = await ServicesAsync(files);
        var assets = Scope(services).GetRequiredService<IAssetService>();
        var bytes = Png(1);
        var sha = Sha(bytes);

        var front = await assets.SaveAsync(bytes, "front.png", "Test front", "front", default, "front");
        await assets.SaveAsync(bytes, "copy.png", "Test copy", "reference", default, "copy");
        var again = await assets.SaveAsync(bytes, "front.png", "Test front", "front", default, "front");
        var record = await Scope(services).GetRequiredService<IMarketingRepository>().FindAssetAsync("front", default);
        var file = files.Model(record!.FileId!);
        var read = await assets.ReadAsync("copy", default);
        var opened = await assets.OpenAsync("front", default);

        Assert.IsType<FilesAssetService>(assets);
        Assert.Equal(1, files.Uploads);
        Assert.Equal(front, again);
        Assert.Equal("marketing", file.AppId);
        Assert.Equal("", record.BlobPath);
        Assert.Contains(new FileReference("marketing.asset", sha), files.ReferencesOf(file.Id));
        Assert.Equal(bytes, read.Bytes);
        Assert.Equal(TestFiles.Link(file.Id, null), opened.Location);
        Assert.Null(opened.Bytes);
        Assert.False(Directory.Exists(Path.Combine(root, "blobs", "originals")));

        files.Tamper(file.Id);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => assets.ReadAsync("front", default))).StatusCode);
    }

    [Fact]
    public async Task An_asset_stored_before_files_reads_locally_until_the_backfill_fills_its_id()
    {
        var bytes = Png(2);
        var local = await ServicesAsync(null);
        await Scope(local).GetRequiredService<IAssetService>().SaveAsync(bytes, "front.png", "Test front", "front", default, "front");

        var unmigrated = await ServicesAsync(new TestFiles());
        var before = await Scope(unmigrated).GetRequiredService<IAssetService>().OpenAsync("front", default);

        var migrated = new TestFiles();
        var moved = migrated.Add(bytes, "marketing", new FileReference("marketing.asset", Sha(bytes)));
        var after = await ServicesAsync(migrated);
        await Scope(after).GetRequiredService<FilesAssetBackfill>().RunAsync(default);
        var opened = await Scope(after).GetRequiredService<IAssetService>().OpenAsync("front", default);
        var record = await Scope(after).GetRequiredService<IMarketingRepository>().FindAssetAsync("front", default);

        Assert.Null(before.Location);
        Assert.Equal(bytes, before.Bytes);
        Assert.Equal(moved.Id.ToString(), record?.FileId);
        Assert.Equal(TestFiles.Link(moved.Id, null), opened.Location);
        Assert.Equal(0, migrated.Uploads);
    }

    [Fact]
    public async Task An_export_lands_in_files_once_and_answers_the_same_bytes_each_time()
    {
        var files = new TestFiles();
        var services = await ServicesAsync(files);
        var assets = Scope(services).GetRequiredService<IAssetService>();
        await assets.SaveAsync(Png(3), "front.png", "Test front", "front", default, "front");
        await ApproveDraftAsync(services);

        var first = await assets.ExportDraftAsync("draft-1", default);
        var second = await Scope(services).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);
        var exported = Assert.Single(files.Of("marketing"), file => file.ContentType == "application/zip");

        Assert.Equal(first, second);
        Assert.Equal(2, files.Uploads);
        Assert.Equal(Sha(first), exported.Sha256);
        Assert.Contains(new FileReference("marketing.export", Sha(first)), files.ReferencesOf(exported.Id));
        Assert.False(Directory.Exists(Path.Combine(root, "blobs", "exports")));
    }

    [Fact]
    public async Task An_export_made_before_files_keeps_its_bytes_and_the_backfill_fills_its_id()
    {
        var local = await ServicesAsync(null);
        await Scope(local).GetRequiredService<IAssetService>().SaveAsync(Png(4), "front.png", "Test front", "front", default, "front");
        await ApproveDraftAsync(local);
        var before = await Scope(local).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);

        var unmigrated = new TestFiles();
        var still = await Scope(await ServicesAsync(unmigrated)).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);

        var migrated = new TestFiles();
        var moved = migrated.Add(before, "marketing", new FileReference("marketing.export", Sha(before)), "application/zip");
        var after = await ServicesAsync(migrated);
        await Scope(after).GetRequiredService<FilesAssetBackfill>().RunAsync(default);
        var record = await Scope(after).GetRequiredService<IMarketingRepository>().FindExportFileAsync("draft-1", 1, default);
        File.Delete(Path.Combine(root, "blobs", "exports", "draft-1", "1.zip"));
        var fromFiles = await Scope(after).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);

        Assert.Equal(before, still);
        Assert.Equal(0, unmigrated.Uploads);
        Assert.Equal(moved.Id.ToString(), record?.FileId);
        Assert.Equal(before, fromFiles);
    }

    [Fact]
    public async Task An_unreachable_files_refuses_an_upload_with_503_and_leaves_the_backfill_quiet()
    {
        var local = await ServicesAsync(null);
        await Scope(local).GetRequiredService<IAssetService>().SaveAsync(Png(5), "front.png", "Test front", "front", default, "front");
        var files = new TestFiles { Outage = AppErrorFactory.ExternalUnavailable("WoW2 Files is down.") };
        var services = await ServicesAsync(files);

        var refused = await Assert.ThrowsAsync<MarketingException>(() =>
            Scope(services).GetRequiredService<IAssetService>().SaveAsync(Png(6), "detail.png", "Test detail", "detail", default, "detail"));
        await Scope(services).GetRequiredService<FilesAssetBackfill>().RunAsync(default);
        var record = await Scope(services).GetRequiredService<IMarketingRepository>().FindAssetAsync("front", default);

        Assert.Equal(503, refused.StatusCode);
        Assert.Null(record?.FileId);
        Assert.Null(await Scope(services).GetRequiredService<IMarketingRepository>().FindAssetAsync("detail", default));
    }

    [Fact]
    public async Task The_asset_route_redirects_a_member_to_the_link_and_the_export_route_answers_bytes()
    {
        var files = new TestFiles();
        var settings = MarketingTestIdentity.Settings();
        settings["Files:BaseUrl"] = TestFiles.Url;
        var connection = await DatabaseAsync();
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("DatabaseSettings:ConnectionString", connection);
            builder.UseSetting("Marketing:DataDirectory", Path.Combine(root, "web"));
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHostedService>();
                MarketingTestIdentity.Use(services);
            });
            builder.ConfigureTestServices(files.Register);
        });
        hosts.Add(factory);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAssetService>().SaveAsync(Png(7), "front.png", "Test front", "front", default, "front");
        }
        await ApproveDraftAsync(factory.Services);
        var stored = Assert.Single(files.Of("marketing"));
        using var member = await MarketingTestIdentity.SignedIn(factory, MarketingTestIdentity.Viewer);
        using var outsider = await MarketingTestIdentity.SignedIn(factory, MarketingTestIdentity.Outsider);

        using var asset = await member.GetAsync("/api/marketing/assets/front");
        using var hidden = await outsider.GetAsync("/api/marketing/assets/front");
        using var export = await member.GetAsync("/api/marketing/drafts/draft-1/export");
        var zip = await export.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.Redirect, asset.StatusCode);
        Assert.Equal(TestFiles.Link(stored.Id, null), asset.Headers.Location);
        Assert.True(asset.Headers.CacheControl?.NoStore);
        Assert.Equal(HttpStatusCode.Forbidden, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("application/zip", export.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Sha(zip), Assert.Single(files.Of("marketing"), file => file.ContentType == "application/zip").Sha256);
    }

    private async Task<ServiceProvider> ServicesAsync(TestFiles? files)
    {
        Directory.CreateDirectory(root);
        var settings = MarketingTestIdentity.Settings();
        settings["DatabaseSettings:ConnectionString"] = await DatabaseAsync();
        settings["Marketing:DataDirectory"] = root;
        settings["Files:BaseUrl"] = files is null ? null : TestFiles.Url;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HostConfiguration).Assembly.GetName().Name,
            ContentRootPath = root,
            EnvironmentName = "Testing",
        });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Configure();
        files?.Register(builder.Services);
        var services = builder.Services.BuildServiceProvider();
        hosts.Add(services);
        return services;
    }

    /// <summary>The test's own database, shared by every host the test builds as they shared one store before, and
    /// dropped after them.</summary>
    private async Task<string> DatabaseAsync()
    {
        if (database is null)
        {
            database = await MarketingPostgres.CreateDatabaseAsync();
            hosts.Insert(0, new Dropped(database));
        }
        return database;
    }

    /// <summary>Drops a test database; listed first, it disposes after the hosts listed later.</summary>
    private sealed class Dropped(string connectionString) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await MarketingPostgres.DropDatabaseAsync(connectionString);
    }

    /// <summary>Saves draft <c>draft-1</c> with the asset <c>front</c> and approves its first revision.</summary>
    private static async Task ApproveDraftAsync(IServiceProvider services)
    {
        var repository = Scope(services).GetRequiredService<IMarketingRepository>();
        var now = DateTimeOffset.UtcNow;
        await repository.SaveDraftAsync(new DraftModel("draft-1", "Nimcha", "nimcha-v06", "t02-bone", "en", "Original caption",
            ["instagram"], ["front"], 1, null, "draft", now, now), null, default);
        await repository.ApproveDraftAsync("draft-1", 1, default);
    }

    private static IServiceProvider Scope(IServiceProvider services) => services.CreateScope().ServiceProvider;

    /// <summary>A 1×1 PNG with a trailing byte, so each test stores distinct bytes.</summary>
    private static byte[] Png(byte seed) =>
        [.. Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII="), seed];

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
