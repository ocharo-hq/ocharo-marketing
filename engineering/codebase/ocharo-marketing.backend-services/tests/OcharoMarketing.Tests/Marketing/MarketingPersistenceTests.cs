using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Persistence.Marketing.Repositories;
using WoW.Two.Sdk.Backend.Beta.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Verifies revision, export and queue behavior across real SQLite contexts.</summary>
public sealed class MarketingPersistenceTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(Path.GetTempPath(), "ocharo-marketing-tests", Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync() => await database.InitializeAsync();
    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public async Task StaleEditsAndApprovalFailWithoutChangingCurrentRevision()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var initial = Draft();
        await repository.SaveDraftAsync(initial, null, default);
        await repository.SaveDraftAsync(initial with { Revision = 2, Caption = "Updated caption" }, 1, default);
        var stale = await Assert.ThrowsAsync<MarketingException>(() => repository.SaveDraftAsync(initial with { Revision = 2 }, 1, default));
        Assert.Equal(409, stale.StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => repository.ApproveDraftAsync(initial.Id, 1, default))).StatusCode);
        Assert.Equal("Updated caption", (await repository.FindDraftAsync(initial.Id, default))!.Caption);
    }

    [Fact]
    public async Task ScheduledRevisionKeepsOriginalCopyAfterDraftEdit()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var initial = Draft();
        await repository.SaveDraftAsync(initial, null, default);
        await repository.ApproveDraftAsync(initial.Id, 1, default);
        await repository.ScheduleAsync(new("schedule-1", initial.Id, 1, "instagram", DateTimeOffset.UtcNow.AddDays(1), "planned", "Manual plan", null), default);
        await repository.SaveDraftAsync(initial with { Revision = 2, Title = "New draft title", Caption = "Changed", ApprovedRevision = null }, 1, default);
        var schedule = Assert.Single(await repository.GetSchedulesAsync(default));
        Assert.Equal(1, schedule.DraftRevision);
        Assert.Equal("Nimcha", schedule.DraftTitle);
        Assert.Equal("Original caption", (await repository.FindRevisionAsync(initial.Id, 1, default))!.Caption);
        Assert.Null((await repository.FindDraftAsync(initial.Id, default))!.ApprovedRevision);
        await Assert.ThrowsAsync<MarketingException>(() => repository.ScheduleAsync(schedule with { Id = "schedule-2" }, default));
    }

    [Fact]
    public async Task QueueSurvivesContextsAndClaimsOnlyOnce()
    {
        var now = DateTimeOffset.UtcNow;
        await using (var context = database.NewContext())
            await new MarketingRepository(context).EnqueueAsync(Job("card-job", "card", now), "{}", default);
        await using var first = database.NewContext();
        var one = await new MarketingRepository(first).ClaimAsync(now, TimeSpan.FromMinutes(1), default);
        Assert.NotNull(one);
        await using var second = database.NewContext();
        Assert.Null(await new MarketingRepository(second).ClaimAsync(now, TimeSpan.FromMinutes(1), default));
        await new MarketingRepository(first).CompleteAsync(one, ["output"], default);
        Assert.Equal("completed", Assert.Single(await new MarketingRepository(second).GetJobsAsync(default)).Status);
    }

    [Fact]
    public async Task ExpiredExternalLeaseBecomesUnknownWhileLocalRenderingRequeues()
    {
        var now = DateTimeOffset.UtcNow;
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        await repository.EnqueueAsync(Job("external", "image", now), "{}", default);
        Assert.NotNull(await repository.ClaimAsync(now, TimeSpan.FromSeconds(1), default));
        Assert.Null(await repository.ClaimAsync(now.AddSeconds(2), TimeSpan.FromSeconds(1), default));
        Assert.Equal("unknown", Assert.Single(await repository.GetJobsAsync(default)).Status);
        await repository.EnqueueAsync(Job("local", "card", now), "{}", default);
        var local = await repository.ClaimAsync(now, TimeSpan.FromSeconds(1), default);
        Assert.NotNull(local);
        var recovered = await repository.ClaimAsync(now.AddSeconds(2), TimeSpan.FromSeconds(1), default);
        Assert.Equal(local.Id, recovered!.Id);
        Assert.NotEqual(local.ClaimToken, recovered.ClaimToken);
        await Assert.ThrowsAsync<MarketingException>(() => repository.CompleteAsync(local, ["stale-output"], default));
    }

    [Fact]
    public async Task ApprovedExportBytesAreStableAndEditsRequireNewApproval()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(repository, new LocalFileBlobRepository(blobRoot), Options.Create(new MarketingOptions()));
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII=");
        var image = await assets.SaveAsync(png, "front.png", "Test original", "front", default);
        var draft = Draft() with { MediaIds = [image.Id] };
        await repository.SaveDraftAsync(draft, null, default);
        await repository.ApproveDraftAsync(draft.Id, 1, default);
        Assert.Equal(await assets.ExportDraftAsync(draft.Id, default), await assets.ExportDraftAsync(draft.Id, default));
        await repository.SaveDraftAsync(draft with { Revision = 2, Caption = "New" }, 1, default);
        Assert.Equal(409, (await Assert.ThrowsAsync<MarketingException>(() => assets.ExportDraftAsync(draft.Id, default))).StatusCode);
    }

    [Fact]
    public async Task InitialMigrationCreatesDurableSchema()
    {
        await using var db = database.NewContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        var repository = new MarketingRepository(db);
        await repository.SaveDraftAsync(Draft(), null, default);
        Assert.Single(await repository.GetDraftsAsync(default));
        Assert.Contains("202609270001_InitialMarketing", await db.Database.GetAppliedMigrationsAsync());
    }

    private static DraftModel Draft() => new("draft-1", "Nimcha", "nimcha-v06", "t02-bone", "en", "Original caption", ["instagram"], ["front"], 1, null, "draft", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    private static GenerationJobModel Job(string id, string kind, DateTimeOffset now) => new(id, kind, kind == "card" ? "local" : "openai", "Test", null, "queued", now, [], null);
}
