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

namespace OcharoBlog.Tests;

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
    public async Task Import_copies_every_row_exactly_retires_the_store_and_opens_the_public_site()
    {
        await using var harness = new BlogHarness();
        var rows = await LegacyStore.WriteAsync(harness.StorePath);
        using var anonymous = harness.Anonymous();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await anonymous.GetAsync("/")).StatusCode);

        var run = await RunAsync(harness, at: new DateTimeOffset(2026, 11, 9, 17, 4, 5, TimeSpan.FromHours(5)));

        Assert.Equal(0, run.Exit);
        Assert.Equal("imported", run.Result.GetProperty("outcome").GetString());
        Assert.False(run.Result.GetProperty("dryRun").GetBoolean());
        AssertMatches(run.Result, rows);
        Assert.False(File.Exists(harness.StorePath));
        var retired = run.Result.GetProperty("retired").EnumerateArray().Select(name => name.GetString()!).ToList();
        Assert.Equal("blog.imported-20261109120405.db", retired[0]);
        Assert.True(File.Exists(Path.Combine(harness.Root, retired[0])));

        var db = harness.Get<OcharoBlogDbContext>();
        var articles = await db.Articles.AsNoTracking().ToListAsync();
        Assert.Equal(rows.Articles.Select(row => row.Id).Order(), articles.Select(row => row.Id).Order());
        Assert.Equal(30, articles.Count(row => row.Status == ArticleStatus.Published));
        foreach (var source in rows.Articles)
        {
            var copy = articles.Single(row => row.Id == source.Id);
            Assert.Equal((source.Slug, source.Status, source.Revision), (copy.Slug, copy.Status, copy.Revision));
            Assert.Equal((source.PublishAt, source.PublishedAt), (copy.PublishAt, copy.PublishedAt));
            Assert.Equal((source.CreatedAt, source.UpdatedAt), (copy.CreatedAt, copy.UpdatedAt));
            Assert.Equal(Document(source.Json), Document(copy.Json));
        }
        var scheduled = articles.Single(row => row.Status == ArticleStatus.Scheduled);
        Assert.Equal(LegacyStore.ScheduledAt, scheduled.PublishAt);
        Assert.Equal(TimeSpan.Zero, scheduled.PublishAt!.Value.Offset);
        var revisions = await db.ArticleRevisions.AsNoTracking().ToListAsync();
        Assert.Equal(rows.Revisions.Select(row => (row.Id, row.ArticleId, row.Revision)).Order(),
            revisions.Select(row => (row.Id, row.ArticleId, row.Revision)).Order());
        var media = await db.MediaBlobs.AsNoTracking().ToListAsync();
        Assert.Equal(rows.Media.Select(row => (row.Sha256, row.FileId)).Order(),
            media.Select(row => (row.Sha256, row.FileId)).Order());
        Assert.Contains(media, row => row.FileId == LegacyStore.FileId);
        var edit = Assert.Single(await db.MediaEdits.AsNoTracking().ToListAsync());
        Assert.Equal(90, JsonDocument.Parse(edit.RecipeJson).RootElement.GetProperty("rotate").GetInt32());

        // The serving host saw the store retired without a restart.
        Assert.Contains("Browse all 30 stories", await anonymous.GetStringAsync("/"));
        foreach (var story in articles.Where(row => row.Status == ArticleStatus.Published))
        {
            var title = BlogJson.Read<ArticleDocument>(story.Json).Title;
            var page = await anonymous.GetAsync($"/journal/{story.Slug}/");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Contains(WebUtility.HtmlEncode(title), await page.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task Dry_run_verifies_the_copy_then_rolls_it_back_and_renames_nothing()
    {
        await using var harness = new BlogHarness(seed: false);
        var rows = await LegacyStore.WriteAsync(harness.StorePath);

        var run = await RunAsync(harness, ["--dry-run"]);

        Assert.Equal(0, run.Exit);
        Assert.Equal("verified", run.Result.GetProperty("outcome").GetString());
        Assert.True(run.Result.GetProperty("dryRun").GetBoolean());
        AssertMatches(run.Result, rows);
        Assert.Empty(run.Result.GetProperty("retired").EnumerateArray());
        Assert.True(File.Exists(harness.StorePath));
        Assert.Empty(Directory.GetFiles(harness.Root, "blog.imported-*"));
        Assert.All(Tables, table => Assert.Equal(0, Count(harness, table)));
        Assert.True(harness.Get<SqliteImportGate>().IsPending);
    }

    [Fact]
    public async Task Import_refuses_a_target_that_holds_other_rows_and_writes_nothing()
    {
        // The seed ran at boot, so the schema holds the journal under ids of its own.
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var seeded = Count(harness, "articles");
        await LegacyStore.WriteAsync(harness.StorePath);

        var run = await RunAsync(harness);

        Assert.Equal(1, run.Exit);
        Assert.Equal("refused", run.Result.GetProperty("outcome").GetString());
        Assert.True(File.Exists(harness.StorePath));
        Assert.Equal(seeded, Count(harness, "articles"));
        Assert.Equal(0, Count(harness, "media_edits"));
    }

    [Fact]
    public async Task Import_fails_with_exit_2_naming_each_column_whose_id_Identity_does_not_hold()
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

            Assert.Equal(2, run.Exit);
            Assert.Equal("failed", run.Result.GetProperty("outcome").GetString());
            var reason = run.Result.GetProperty("reason").GetString()!;
            Assert.Contains("media_edits.organization_id: 1 row", reason);
            Assert.Contains("media_edits.actor: 2 rows", reason);
            Assert.DoesNotContain(stranger, run.Result.GetRawText() + run.Error);
            Assert.DoesNotContain(notAnId, run.Result.GetRawText() + run.Error);
        }
        Assert.True(File.Exists(harness.StorePath));
        Assert.All(Tables, table => Assert.Equal(0, Count(harness, table)));
    }

    [Fact]
    public async Task Import_has_nothing_to_do_without_a_store()
    {
        await using var harness = new BlogHarness(seed: false);

        var run = await RunAsync(harness);

        Assert.Equal(0, run.Exit);
        Assert.Equal("nothing-to-import", run.Result.GetProperty("outcome").GetString());
        Assert.Empty(run.Result.GetProperty("tables").EnumerateArray());
    }

    [Fact]
    public async Task Import_retires_the_stores_companion_files_and_a_rerun_after_a_lost_rename_retires_it_again()
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

        Assert.Equal(0, run.Exit);
        var retired = run.Result.GetProperty("retired").EnumerateArray().Select(name => name.GetString()!).ToList();
        Assert.Contains(retired[0] + "-wal", retired);
        Assert.Contains(retired[0] + "-journal", retired);
        Assert.All(retired, name => Assert.True(File.Exists(Path.Combine(harness.Root, name))));
        Assert.Empty(Directory.GetFiles(harness.Root, "blog.db*"));

        // The store comes back, as after a commit whose rename failed: the rows are already the store's.
        File.Copy(kept, harness.StorePath);
        var rerun = await RunAsync(harness, at: at.AddMinutes(1));

        Assert.Equal(0, rerun.Exit);
        Assert.Equal("already-imported", rerun.Result.GetProperty("outcome").GetString());
        Assert.False(File.Exists(harness.StorePath));
    }

    [Fact]
    public async Task Pending_import_answers_503_and_seeding_and_scheduling_leave_the_schema_empty()
    {
        await using var harness = new BlogHarness();
        await LegacyStore.WriteAsync(harness.StorePath);
        using var anonymous = harness.Anonymous();
        using var owner = harness.Client();

        // Boot ran the seed, and the scheduler's pass runs now: both skip while the store waits.
        await harness.Get<ScheduledPublisher>().PublishDueAsync(CancellationToken.None);
        Assert.All(Tables, table => Assert.Equal(0, Count(harness, table)));

        foreach (var path in new[] { "/", "/journal/", "/journal/let-one-piece-lead/", "/media/abc" })
            await AssertPendingAsync(await anonymous.GetAsync(path));
        await AssertPendingAsync(await owner.GetAsync("/api/blog/articles"));
        await AssertPendingAsync(await owner.PostAsync("/api/blog/topics", JsonContent("""{"key":"x","name":"X"}""")));
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/system/status")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/runtime-config")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/identity/session")).StatusCode);
        Assert.All(Tables, table => Assert.Equal(0, Count(harness, table)));

        Assert.Equal(0, (await RunAsync(harness)).Exit);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/blog/articles")).StatusCode);
    }

    [Fact]
    public async Task Seeding_without_a_store_fills_an_empty_blog_as_before()
    {
        await using var harness = new BlogHarness();
        using var anonymous = harness.Anonymous();

        Assert.Contains("Browse all 30 stories", await anonymous.GetStringAsync("/"));
        Assert.False(harness.Get<SqliteImportGate>().IsPending);
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
        Assert.StartsWith("import-result ", last);
        var result = JsonDocument.Parse(last["import-result ".Length..]).RootElement;
        Assert.Equal(exit, result.GetProperty("exit").GetInt32());
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
        Assert.Equal(Tables, tables.Select(table => table.GetProperty("table").GetString()));
        foreach (var table in tables)
        {
            var count = expected[table.GetProperty("table").GetString()!];
            Assert.Equal(count, table.GetProperty("sourceRows").GetInt32());
            Assert.Equal(count, table.GetProperty("targetRows").GetInt32());
            Assert.Equal(table.GetProperty("sourceSha256").GetString(), table.GetProperty("targetSha256").GetString());
            Assert.True(table.GetProperty("matches").GetBoolean());
        }
        Assert.Equal(30, rows.Articles.Count(row => row.Status == ArticleStatus.Published));
    }

    private static async Task AssertPendingAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("import_pending", problem.GetProperty("code").GetString());
    }

    private static string Document(string json) => BlogJson.Write(BlogJson.Read<ArticleDocument>(json));

    private static long Count(BlogHarness harness, string table)
    {
        var sql = "SELECT COUNT(*) AS \"Value\" FROM blog." + table;
        return harness.Get<OcharoBlogDbContext>().Database.SqlQueryRaw<long>(sql).AsEnumerable().Single();
    }

    private static StringContent JsonContent(string json) => new(json, System.Text.Encoding.UTF8, "application/json");
}
