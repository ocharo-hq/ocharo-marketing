using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using AwesomeAssertions;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Assets.Services;
using OcharoMarketing.Infrastructure.Core;
using OcharoMarketing.Persistence.Core.Repositories;
using OcharoMarketing.Tests.Support;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Core;

/// <summary>Verifies revision, export and queue behavior across real PostgreSQL contexts.</summary>
public sealed class MarketingPersistenceTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly string blobRoot = Path.Combine(
        Path.GetTempPath(),
        "ocharo-marketing-tests",
        Guid.NewGuid().ToString("N"));

    public async Task InitializeAsync() => await database.InitializeAsync();
    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        if (Directory.Exists(blobRoot)) Directory.Delete(blobRoot, true);
    }

    [Fact]
    public async Task SaveAndApprove_ShouldFailWithoutChangingTheRevision_WhenItIsStale()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var initial = Draft();
        await repository.SaveDraftAsync(initial, null, default);
        await repository.SaveDraftAsync(initial with { Revision = 2, Caption = "Updated caption" }, 1, default);
        var stale = (
            await FluentActions
                .Awaiting(() => repository.SaveDraftAsync(initial with { Revision = 2 }, 1, default))
                .Should()
                .ThrowExactlyAsync<AppException>()).Which;
        ErrorStatus.Of(stale).Should().Be(409);
        ErrorStatus
            .Of(
                (
                    await FluentActions
                        .Awaiting(() => repository.ApproveDraftAsync(initial.Id, 1, default))
                        .Should()
                        .ThrowExactlyAsync<AppException>()).Which)
            .Should()
            .Be(409);
        ((await repository.FindDraftAsync(initial.Id, default))!.Caption).Should().Be("Updated caption");
    }

    [Fact]
    public async Task Schedule_ShouldKeepTheOriginalCopy_WhenTheDraftIsEditedLater()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var initial = Draft();
        await repository.SaveDraftAsync(initial, null, default);
        await repository.ApproveDraftAsync(initial.Id, 1, default);
        await repository.ScheduleAsync(
            new(
                "schedule-1",
                initial.Id,
                1,
                "instagram",
                DateTimeOffset.UtcNow.AddDays(1),
                "planned",
                "Manual plan",
                null),
            default);
        await repository.SaveDraftAsync(
            initial with { Revision = 2, Title = "New draft title", Caption = "Changed", ApprovedRevision = null },
            1,
            default);
        var schedule = (await repository.GetSchedulesAsync(default)).Should().ContainSingle().Which;
        schedule.DraftRevision.Should().Be(1);
        schedule.DraftTitle.Should().Be("Nimcha");
        ((await repository.FindRevisionAsync(initial.Id, 1, default))!.Caption).Should().Be("Original caption");
        ((await repository.FindDraftAsync(initial.Id, default))!.ApprovedRevision).Should().BeNull();
        await FluentActions
            .Awaiting(() => repository.ScheduleAsync(schedule with { Id = "schedule-2" }, default))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }

    [Fact]
    public async Task Claim_ShouldSurviveContextsAndClaimOnlyOnce()
    {
        var now = DateTimeOffset.UtcNow;
        await using (var context = database.NewContext())
            await new MarketingRepository(context).EnqueueAsync(Job("card-job", "card", now), "{}", null, default);
        await using var first = database.NewContext();
        var one = await new MarketingRepository(first).ClaimAsync(now, TimeSpan.FromMinutes(1), default);
        one.Should().NotBeNull();
        await using var second = database.NewContext();
        (await new MarketingRepository(second).ClaimAsync(now, TimeSpan.FromMinutes(1), default)).Should().BeNull();
        await new MarketingRepository(first).CompleteAsync(one, ["output"], default);
        ((await new MarketingRepository(second).GetJobsAsync(default)).Should().ContainSingle().Which.Status)
            .Should()
            .Be("completed");
    }

    [Fact]
    public async Task Claim_ShouldMarkExternalUnknownAndRequeueLocal_WhenTheLeaseExpires()
    {
        var now = DateTimeOffset.UtcNow;
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        await repository.EnqueueAsync(Job("external", "image", now), "{}", null, default);
        (await repository.ClaimAsync(now, TimeSpan.FromSeconds(1), default)).Should().NotBeNull();
        (await repository.ClaimAsync(now.AddSeconds(2), TimeSpan.FromSeconds(1), default)).Should().BeNull();
        ((await repository.GetJobsAsync(default)).Should().ContainSingle().Which.Status).Should().Be("unknown");
        await repository.EnqueueAsync(Job("local", "card", now), "{}", null, default);
        var local = await repository.ClaimAsync(now, TimeSpan.FromSeconds(1), default);
        local.Should().NotBeNull();
        var recovered = await repository.ClaimAsync(now.AddSeconds(2), TimeSpan.FromSeconds(1), default);
        recovered!.Id.Should().Be(local.Id);
        recovered.ClaimToken.Should().NotBe(local.ClaimToken);
        await FluentActions
            .Awaiting(() => repository.CompleteAsync(local, ["stale-output"], default))
            .Should()
            .ThrowExactlyAsync<AppException>();
    }

    [Fact]
    public async Task ExportDraft_ShouldAnswerStableBytesAndNeedNewApproval_WhenTheDraftIsEdited()
    {
        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        var assets = new LocalAssetService(
            repository,
            new LocalFileBlobRepository(blobRoot),
            Options.Create(new MarketingOptions()));
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aCfoAAAAASUVORK5CYII=");
        var image = await assets.SaveAsync(png, "front.png", "Test original", "front", default);
        var draft = Draft() with { MediaIds = [image.Id] };
        await repository.SaveDraftAsync(draft, null, default);
        await repository.ApproveDraftAsync(draft.Id, 1, default);
        (await assets.ExportDraftAsync(draft.Id, default))
            .Should()
            .Equal(await assets.ExportDraftAsync(draft.Id, default));
        await repository.SaveDraftAsync(draft with { Revision = 2, Caption = "New" }, 1, default);
        ErrorStatus
            .Of(
                (
                    await FluentActions
                        .Awaiting(() => assets.ExportDraftAsync(draft.Id, default))
                        .Should()
                        .ThrowExactlyAsync<AppException>()).Which)
            .Should()
            .Be(409);
    }

    [Fact]
    public async Task MarketingSet_ShouldHoldEveryColumnTheModelMapsWithItsType()
    {
        await using var db = database.NewContext();
        var schema = new Dictionary<(string Table, string Column), (string Type, bool IsNullable)>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT table_name, column_name, data_type, is_nullable = 'YES' "
                + "FROM information_schema.columns WHERE table_schema = 'marketing'";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                schema[(reader.GetString(0), reader.GetString(1))] = (reader.GetString(2), reader.GetBoolean(3));
        }

        foreach (var entity in db.Model.GetEntityTypes())
        {
            entity.GetSchema().Should().Be("marketing");
            var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                var column = schema.Should().ContainKey((table.Name, property.GetColumnName(table)!)).WhoseValue;
                column.Should().Be((property.GetColumnType(), property.IsNullable));
            }
        }
        var repository = new MarketingRepository(db);
        await repository.SaveDraftAsync(Draft(), null, default);
        (await repository.GetDraftsAsync(default)).Should().ContainSingle();
    }

    private static DraftModel Draft() =>
        new(
            "draft-1",
            "Nimcha",
            "nimcha-v06",
            "t02-bone",
            "en",
            "Original caption",
            ["instagram"],
            ["front"],
            1,
            null,
            "draft",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    private static GenerationJobModel Job(string id, string kind, DateTimeOffset now) =>
        new(id, kind, kind == "card" ? "local" : "openai", "Test", null, "queued", now, [], null);
}
