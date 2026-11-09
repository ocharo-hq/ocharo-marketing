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
using AwesomeAssertions;
using OcharoMarketing.Api.Configurations;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Assets.Services;
using OcharoMarketing.Persistence;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using Xunit;

namespace OcharoMarketing.Tests.E2E.Assets;

/// <summary>Marketing on WoW2 Files (<c>Files:BaseUrl</c> set): assets land in Files once per SHA-256 as app
/// <c>marketing</c>, the asset route redirects to a short-lived Files link, server-side reads come back from Files
/// checked
/// against their hash, and each approved revision's export ZIP is a private Files file with the same bytes on every
/// call.
/// An asset or export made before Files reads from the local blob folder until the backfill fills its id. Files itself
/// is
/// <see cref="TestFiles"/>, an in-memory broker set.</summary>
public sealed class FilesAssetTests : IAsyncDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "ocharo-marketing-files-tests",
        Guid.NewGuid().ToString("N"));
    private readonly List<IAsyncDisposable> hosts = [];
    private string? database;

    public async ValueTask DisposeAsync()
    {
        foreach (var host in Enumerable.Reverse(hosts))
            await host.DisposeAsync();
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    [Fact]
    public async Task Asset_ShouldLandInFilesOnceAndReadBackThroughFiles_WhenFilesIsSet()
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

        assets.Should().BeOfType<FilesAssetService>();
        files.Uploads.Should().Be(1);
        again.Should().Be(front);
        file.AppId.Should().Be("marketing");
        record.BlobPath.Should().Be("");
        files.ReferencesOf(file.Id).Should().Contain(new FileReference("marketing.asset", sha));
        read.Bytes.Should().Equal(bytes);
        opened.Location.Should().Be(TestFiles.Link(file.Id, null));
        opened.Bytes.Should().BeNull();
        (Directory.Exists(Path.Combine(root, "blobs", "originals"))).Should().BeFalse();

        files.Tamper(file.Id);
        (
            ErrorStatus
                .Of(
                    (
                        await FluentActions
                            .Awaiting(() => assets.ReadAsync("front", default))
                            .Should()
                            .ThrowExactlyAsync<AppException>()).Which))
                .Should()
                .Be(409);
    }

    [Fact]
    public async Task Asset_ShouldReadLocallyUntilTheBackfillFillsItsId_WhenStoredBeforeFiles()
    {
        var bytes = Png(2);
        var local = await ServicesAsync(null);
        await Scope(local)
            .GetRequiredService<IAssetService>()
            .SaveAsync(bytes, "front.png", "Test front", "front", default, "front");

        var unmigrated = await ServicesAsync(new TestFiles());
        var before = await Scope(unmigrated).GetRequiredService<IAssetService>().OpenAsync("front", default);

        var migrated = new TestFiles();
        var moved = migrated.Add(bytes, "marketing", new FileReference("marketing.asset", Sha(bytes)));
        var after = await ServicesAsync(migrated);
        await Scope(after).GetRequiredService<FilesAssetBackfill>().RunAsync(default);
        var opened = await Scope(after).GetRequiredService<IAssetService>().OpenAsync("front", default);
        var record = await Scope(after).GetRequiredService<IMarketingRepository>().FindAssetAsync("front", default);

        before.Location.Should().BeNull();
        before.Bytes.Should().Equal(bytes);
        (record?.FileId).Should().Be(moved.Id.ToString());
        opened.Location.Should().Be(TestFiles.Link(moved.Id, null));
        migrated.Uploads.Should().Be(0);
    }

    [Fact]
    public async Task Export_ShouldLandInFilesOnceAndAnswerTheSameBytes_WhenFilesIsSet()
    {
        var files = new TestFiles();
        var services = await ServicesAsync(files);
        var assets = Scope(services).GetRequiredService<IAssetService>();
        await assets.SaveAsync(Png(3), "front.png", "Test front", "front", default, "front");
        await ApproveDraftAsync(services);

        var first = await assets.ExportDraftAsync("draft-1", default);
        var second = await Scope(services).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);
        var exported = (files.Of("marketing"))
            .Should()
            .ContainSingle(file => file.ContentType == "application/zip").Which;

        second.Should().Equal(first);
        files.Uploads.Should().Be(2);
        exported.Sha256.Should().Be(Sha(first));
        files.ReferencesOf(exported.Id).Should().Contain(new FileReference("marketing.export", Sha(first)));
        (Directory.Exists(Path.Combine(root, "blobs", "exports"))).Should().BeFalse();
    }

    [Fact]
    public async Task Export_ShouldKeepItsBytesWhileTheBackfillFillsItsId_WhenMadeBeforeFiles()
    {
        var local = await ServicesAsync(null);
        await Scope(local)
            .GetRequiredService<IAssetService>()
            .SaveAsync(Png(4), "front.png", "Test front", "front", default, "front");
        await ApproveDraftAsync(local);
        var before = await Scope(local).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);

        var unmigrated = new TestFiles();
        var still = await Scope(await ServicesAsync(unmigrated))
            .GetRequiredService<IAssetService>()
            .ExportDraftAsync("draft-1", default);

        var migrated = new TestFiles();
        var moved = migrated.Add(
            before,
            "marketing",
            new FileReference("marketing.export", Sha(before)),
            "application/zip");
        var after = await ServicesAsync(migrated);
        await Scope(after).GetRequiredService<FilesAssetBackfill>().RunAsync(default);
        var record = await Scope(after)
            .GetRequiredService<IMarketingRepository>()
            .FindExportFileAsync("draft-1", 1, default);
        File.Delete(Path.Combine(root, "blobs", "exports", "draft-1", "1.zip"));
        var fromFiles = await Scope(after).GetRequiredService<IAssetService>().ExportDraftAsync("draft-1", default);

        still.Should().Equal(before);
        unmigrated.Uploads.Should().Be(0);
        (record?.FileId).Should().Be(moved.Id.ToString());
        fromFiles.Should().Equal(before);
    }

    [Fact]
    public async Task Upload_ShouldReturn503AndLeaveTheBackfillQuiet_WhenFilesIsUnreachable()
    {
        var local = await ServicesAsync(null);
        await Scope(local)
            .GetRequiredService<IAssetService>()
            .SaveAsync(Png(5), "front.png", "Test front", "front", default, "front");
        var files = new TestFiles { Outage = AppErrorFactory.ExternalUnavailable("WoW2 Files is down.") };
        var services = await ServicesAsync(files);

        var refused = (await FluentActions.Awaiting(() =>
            Scope(services)
                .GetRequiredService<IAssetService>()
                .SaveAsync(Png(6), "detail.png", "Test detail", "detail", default, "detail"))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which;
        await Scope(services).GetRequiredService<FilesAssetBackfill>().RunAsync(default);
        var record = await Scope(services).GetRequiredService<IMarketingRepository>().FindAssetAsync("front", default);

        ErrorStatus.Of(refused).Should().Be(503);
        (record?.FileId).Should().BeNull();
        (await Scope(services).GetRequiredService<IMarketingRepository>().FindAssetAsync("detail", default))
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task AssetAndExportRoutes_ShouldRedirectToTheLinkAndAnswerBytes_WhenAMemberAsks()
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
            await scope.ServiceProvider
                .GetRequiredService<IAssetService>()
                .SaveAsync(Png(7), "front.png", "Test front", "front", default, "front");
        }
        await ApproveDraftAsync(factory.Services);
        var stored = (files.Of("marketing")).Should().ContainSingle().Which;
        using var member = await MarketingTestIdentity.SignedIn(factory, MarketingTestIdentity.Viewer);
        using var outsider = await MarketingTestIdentity.SignedIn(factory, MarketingTestIdentity.Outsider);

        using var asset = await member.GetAsync("/api/marketing/assets/front");
        using var hidden = await outsider.GetAsync("/api/marketing/assets/front");
        using var export = await member.GetAsync("/api/marketing/drafts/draft-1/export");
        var zip = await export.Content.ReadAsByteArrayAsync();

        asset.StatusCode.Should().Be(HttpStatusCode.Redirect);
        asset.Headers.Location.Should().Be(TestFiles.Link(stored.Id, null));
        (asset.Headers.CacheControl?.NoStore).Should().BeTrue();
        hidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        export.StatusCode.Should().Be(HttpStatusCode.OK);
        (export.Content.Headers.ContentType?.MediaType).Should().Be("application/zip");
        ((files.Of("marketing")).Should().ContainSingle(file => file.ContentType == "application/zip").Which.Sha256)
            .Should()
            .Be(Sha(zip));
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
        await repository.SaveDraftAsync(
            new DraftModel("draft-1", "Nimcha", "nimcha-v06", "t02-bone", "en", "Original caption",
            ["instagram"], ["front"], 1, null, "draft", now, now), null, default);
        await repository.ApproveDraftAsync("draft-1", 1, default);
    }

    private static IServiceProvider Scope(IServiceProvider services) => services.CreateScope().ServiceProvider;

    /// <summary>A 1×1 PNG with a trailing byte, so each test stores distinct bytes.</summary>
    private static byte[] Png(byte seed) =>
        [
            .. Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII="),
            seed];

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
