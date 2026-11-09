using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OcharoBlog.Api.Blog;
using OcharoBlog.Api.Configurations;
using OcharoBlog.Application.Blog;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog;
using OcharoBlog.Persistence;
using WoW.Two.Sdk.Beta.Be.Meta;

namespace OcharoBlog.Tests.E2E;

/// <summary>
/// The one-time move of an earlier release's SQLite store (<see cref="LegacyStore"/>) into PostgreSQL: the
/// <c>import-sqlite</c> verb, run as a deploy's operator job would run it, beside a serving host that holds every write
/// back until it ran.
/// </summary>
public sealed class SqliteImportTests
{
    private static readonly string[] Tables =
        ["topics", "site_settings", "articles", "article_revisions", "media_blobs", "media_edits"];

    [Fact]
    public async Task Import_ShouldCopyEveryRowAndRetireTheStore_WhenTheTargetIsEmpty()
    {
        await using var harness = new BlogHarness();
        var rows = await LegacyStore.WriteAsync(harness.StorePath);
        using var anonymous = harness.Anonymous();
        ((await anonymous.GetAsync("/")).StatusCode).Should().Be(HttpStatusCode.ServiceUnavailable);

        var run = await RunAsync(harness, at: new DateTimeOffset(2026, 11, 9, 17, 4, 5, TimeSpan.FromHours(5)));

        run.Exit.Should().Be(0);
        run.Result.GetProperty("outcome").GetString().Should().Be("imported");
        run.Result.GetProperty("dryRun").GetBoolean().Should().BeFalse();
        AssertMatches(run.Result, rows);
        File.Exists(harness.StorePath).Should().BeFalse();
        var retired = run.Result.GetProperty("retired").EnumerateArray().Select(name => name.GetString()!).ToList();
        retired[0].Should().Be("blog.imported-20261109120405.db");
        File.Exists(Path.Combine(harness.Root, retired[0])).Should().BeTrue();

        var db = harness.Get<OcharoBlogDbContext>();
        var articles = await db.Articles.AsNoTracking().ToListAsync();
        articles.Select(row => row.Id).Order().Should().Equal(rows.Articles.Select(row => row.Id).Order());
        articles.Count(row => row.Status == ArticleStatus.Published).Should().Be(30);
        foreach (var source in rows.Articles)
        {
            var copy = articles.Single(row => row.Id == source.Id);
            ((copy.Slug, copy.Status, copy.Revision)).Should().Be((source.Slug, source.Status, source.Revision));
            ((copy.PublishAt, copy.PublishedAt)).Should().Be((source.PublishAt, source.PublishedAt));
            ((copy.CreatedAt, copy.UpdatedAt)).Should().Be((source.CreatedAt, source.UpdatedAt));
            Document(copy.Json).Should().Be(Document(source.Json));
        }
        var scheduled = articles.Single(row => row.Status == ArticleStatus.Scheduled);
        scheduled.PublishAt.Should().Be(LegacyStore.ScheduledAt);
        scheduled.PublishAt!.Value.Offset.Should().Be(TimeSpan.Zero);
        var revisions = await db.ArticleRevisions.AsNoTracking().ToListAsync();
        revisions.Select(row => (row.Id, row.ArticleId, row.Revision))
            .Order()
            .Should()
            .Equal(rows.Revisions.Select(row => (row.Id, row.ArticleId, row.Revision)).Order());
        var media = await db.MediaBlobs.AsNoTracking().ToListAsync();
        media.Select(row => (row.Sha256, row.FileId))
            .Order()
            .Should()
            .Equal(rows.Media.Select(row => (row.Sha256, row.FileId)).Order());
        media.Should().Contain(row => row.FileId == LegacyStore.FileId);
        var edit = (await db.MediaEdits.AsNoTracking().ToListAsync()).Should().ContainSingle().Subject;
        JsonDocument.Parse(edit.RecipeJson).RootElement.GetProperty("rotate").GetInt32().Should().Be(90);

        // The serving host saw the store retired without a restart.
        (await anonymous.GetStringAsync("/")).Should().Contain("Browse all 30 stories");
        foreach (var story in articles.Where(row => row.Status == ArticleStatus.Published))
        {
            var title = BlogJson.Read<ArticleDocument>(story.Json).Title;
            var page = await anonymous.GetAsync($"/journal/{story.Slug}/");
            page.StatusCode.Should().Be(HttpStatusCode.OK);
            (await page.Content.ReadAsStringAsync()).Should().Contain(WebUtility.HtmlEncode(title));
        }
    }

    [Fact]
    public async Task Import_ShouldVerifyThenRollBack_WhenRunDry()
    {
        await using var harness = new BlogHarness(seed: false);
        var rows = await LegacyStore.WriteAsync(harness.StorePath);

        var run = await RunAsync(harness, ["--dry-run"]);

        run.Exit.Should().Be(0);
        run.Result.GetProperty("outcome").GetString().Should().Be("verified");
        run.Result.GetProperty("dryRun").GetBoolean().Should().BeTrue();
        AssertMatches(run.Result, rows);
        run.Result.GetProperty("retired").EnumerateArray().Should().BeEmpty();
        File.Exists(harness.StorePath).Should().BeTrue();
        Directory.GetFiles(harness.Root, "blog.imported-*").Should().BeEmpty();
        Tables.Should().AllSatisfy(table => Count(harness, table).Should().Be(0));
        harness.Get<SqliteImportGate>().IsPending.Should().BeTrue();
    }

    [Fact]
    public async Task Import_ShouldWriteNothing_WhenTheTargetHoldsOtherRows()
    {
        // The seed ran at boot, so the schema holds the journal under ids of its own.
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var seeded = Count(harness, "articles");
        await LegacyStore.WriteAsync(harness.StorePath);

        var run = await RunAsync(harness);

        run.Exit.Should().Be(1);
        run.Result.GetProperty("outcome").GetString().Should().Be("refused");
        File.Exists(harness.StorePath).Should().BeTrue();
        Count(harness, "articles").Should().Be(seeded);
        Count(harness, "media_edits").Should().Be(0);
    }

    [Fact]
    public async Task Import_ShouldExitWith2NamingEachColumn_WhenIdentityLacksAnId()
    {
        await using var harness = new BlogHarness(seed: false);
        var journal = await LegacyStore.JournalAsync();
        var receipt = journal.Edits[0];
        const string stranger = "0193a1b0-0000-7000-8000-0000000000ff";
        const string notAnId = "subject-from-a-test-sign-in";
        MediaEditRecord Receipt(char id, string organization, string actor) => new()
        {
            Id = new string(id, 64), SourceSha256 = receipt.SourceSha256, Sha256 = receipt.Sha256,
            RecipeJson = receipt.RecipeJson, OrganizationId = organization, Actor = actor,
            CreatedAt = receipt.CreatedAt,
        };
        LegacyStore.Write(harness.StorePath, journal with
        {
            Edits = [receipt, Receipt('a', receipt.OrganizationId, stranger), Receipt('b', stranger, notAnId)],
        });

        foreach (var arguments in new[] { ["--dry-run"], Array.Empty<string>() })
        {
            var run = await RunAsync(harness, arguments);

            run.Exit.Should().Be(2);
            run.Result.GetProperty("outcome").GetString().Should().Be("failed");
            var reason = run.Result.GetProperty("reason").GetString()!;
            reason.Should().Contain("media_edits.organization_id: 1 row");
            reason.Should().Contain("media_edits.actor: 2 rows");
            (run.Result.GetRawText() + run.Error).Should().NotContain(stranger);
            (run.Result.GetRawText() + run.Error).Should().NotContain(notAnId);
        }
        File.Exists(harness.StorePath).Should().BeTrue();
        Tables.Should().AllSatisfy(table => Count(harness, table).Should().Be(0));
    }

    [Fact]
    public async Task Import_ShouldDoNothing_WhenThereIsNoStore()
    {
        await using var harness = new BlogHarness(seed: false);

        var run = await RunAsync(harness);

        run.Exit.Should().Be(0);
        run.Result.GetProperty("outcome").GetString().Should().Be("nothing-to-import");
        run.Result.GetProperty("tables").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task Import_ShouldRetireCompanionFiles_WhenARerunFollowsALostRename()
    {
        await using var harness = new BlogHarness(seed: false);
        await LegacyStore.WriteAsync(harness.StorePath);
        // An empty write-ahead log and rollback journal hold nothing, as a store left by a clean shutdown.
        File.WriteAllBytes(harness.StorePath + "-wal", []);
        File.WriteAllBytes(harness.StorePath + "-journal", []);
        var kept = Path.Combine(harness.Root, "kept.db");
        File.Copy(harness.StorePath, kept);
        var at = new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

        var run = await RunAsync(harness, at: at);

        run.Exit.Should().Be(0);
        var retired = run.Result.GetProperty("retired").EnumerateArray().Select(name => name.GetString()!).ToList();
        retired.Should().Contain(retired[0] + "-wal");
        retired.Should().Contain(retired[0] + "-journal");
        retired.Should().AllSatisfy(name => File.Exists(Path.Combine(harness.Root, name)).Should().BeTrue());
        Directory.GetFiles(harness.Root, "blog.db*").Should().BeEmpty();

        // The store comes back, as after a commit whose rename failed: the rows are already the store's.
        File.Copy(kept, harness.StorePath);
        var rerun = await RunAsync(harness, at: at.AddMinutes(1));

        rerun.Exit.Should().Be(0);
        rerun.Result.GetProperty("outcome").GetString().Should().Be("already-imported");
        File.Exists(harness.StorePath).Should().BeFalse();
    }

    [Fact]
    public async Task PendingImport_ShouldReturn503AndHoldSeeding_WhenTheStoreWaits()
    {
        await using var harness = new BlogHarness();
        await LegacyStore.WriteAsync(harness.StorePath);
        using var anonymous = harness.Anonymous();
        using var owner = harness.Client();

        // Boot ran the seed, and the scheduler's pass runs now: both skip while the store waits.
        await harness.Get<ScheduledPublisher>().PublishDueAsync(CancellationToken.None);
        Tables.Should().AllSatisfy(table => Count(harness, table).Should().Be(0));

        foreach (var path in new[] { "/", "/journal/", "/journal/let-one-piece-lead/", "/media/abc" })
            await AssertPendingAsync(await anonymous.GetAsync(path));
        await AssertPendingAsync(await owner.GetAsync("/api/blog/articles"));
        await AssertPendingAsync(await owner.PostAsync("/api/blog/topics", JsonContent("""{"key":"x","name":"X"}""")));
        ((await anonymous.GetAsync("/health")).StatusCode).Should().Be(HttpStatusCode.OK);
        ((await anonymous.GetAsync("/api/system/status")).StatusCode).Should().Be(HttpStatusCode.OK);
        ((await anonymous.GetAsync("/api/runtime-config")).StatusCode).Should().Be(HttpStatusCode.OK);
        ((await owner.GetAsync("/api/identity/session")).StatusCode).Should().Be(HttpStatusCode.OK);
        Tables.Should().AllSatisfy(table => Count(harness, table).Should().Be(0));

        ((await RunAsync(harness)).Exit).Should().Be(0);
        ((await anonymous.GetAsync("/")).StatusCode).Should().Be(HttpStatusCode.OK);
        ((await owner.GetAsync("/api/blog/articles")).StatusCode).Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seeding_ShouldFillAnEmptyBlog_WhenThereIsNoStore()
    {
        await using var harness = new BlogHarness();
        using var anonymous = harness.Anonymous();

        (await anonymous.GetStringAsync("/")).Should().Contain("Browse all 30 stories");
        harness.Get<SqliteImportGate>().IsPending.Should().BeFalse();
    }

    /// <summary>Runs <c>import-sqlite</c> through the service host's verb dispatch, over the harness's database and
    /// store, as the operator job runs it in the release image.</summary>
    /// <param name="harness">The serving host whose database and store the run shares.</param>
    /// <param name="arguments">The verb's arguments; none by default.</param>
    /// <param name="at">The run's clock, which names the retired files; the system clock by default.</param>
    private static async Task<(int Exit, JsonElement Result, string Error)> RunAsync(BlogHarness harness,
        string[]? arguments = null, DateTimeOffset? at = null)
    {
        var settings = new Dictionary<string, string?>(EditorialSignIn.Settings)
        {
            ["ConnectionStrings:OcharoBlog"] = $"Data Source={harness.StorePath}",
        };
        foreach (var (key, value) in BlogDatabase.Settings(harness.Database)) settings[key] = value;
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await ServiceHost.RunAsync([SqliteImportCommand.Name, .. arguments ?? []], builder =>
        {
            builder.Configuration.AddInMemoryCollection(settings);
            builder.Configure();
            if (at is { } now)
                builder.Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FakeTimeProvider(now)));
        }, _ => Task.CompletedTask, verbs => verbs.AddBlogVerbs(), output, error, CancellationToken.None);

        var last = output.ToString().TrimEnd().Split('\n')[^1];
        last.Should().StartWith("import-result ");
        var result = JsonDocument.Parse(last["import-result ".Length..]).RootElement;
        result.GetProperty("exit").GetInt32().Should().Be(exit);
        return (exit, result, error.ToString());
    }

    /// <summary>Asserts every table matched, with the store's row counts on both sides.</summary>
    private static void AssertMatches(JsonElement result, LegacyStore.Rows rows)
    {
        var expected = new Dictionary<string, int>
        {
            ["topics"] = rows.Topics.Count, ["site_settings"] = rows.Settings.Count,
            ["articles"] = rows.Articles.Count, ["article_revisions"] = rows.Revisions.Count,
            ["media_blobs"] = rows.Media.Count, ["media_edits"] = rows.Edits.Count,
        };
        var tables = result.GetProperty("tables").EnumerateArray().ToList();
        tables.Select(table => table.GetProperty("table").GetString()).Should().Equal(Tables);
        foreach (var table in tables)
        {
            var count = expected[table.GetProperty("table").GetString()!];
            table.GetProperty("sourceRows").GetInt32().Should().Be(count);
            table.GetProperty("targetRows").GetInt32().Should().Be(count);
            table.GetProperty("targetSha256").GetString().Should().Be(table.GetProperty("sourceSha256").GetString());
            table.GetProperty("matches").GetBoolean().Should().BeTrue();
        }
        rows.Articles.Count(row => row.Status == ArticleStatus.Published).Should().Be(30);
    }

    private static async Task AssertPendingAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        problem.GetProperty("code").GetString().Should().Be("import_pending");
    }

    private static string Document(string json) => BlogJson.Write(BlogJson.Read<ArticleDocument>(json));

    private static long Count(BlogHarness harness, string table)
    {
        var sql = "SELECT COUNT(*) AS \"Value\" FROM blog." + table;
        return harness.Get<OcharoBlogDbContext>().Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();
    }

    private static StringContent JsonContent(string json) => new(json, System.Text.Encoding.UTF8, "application/json");
}
