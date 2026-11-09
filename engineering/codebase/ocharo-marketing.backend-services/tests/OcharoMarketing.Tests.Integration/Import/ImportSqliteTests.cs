using System.Text.Json;
using AwesomeAssertions;
using Npgsql;
using OcharoMarketing.Persistence.Core.Repositories;
using OcharoMarketing.Persistence.Import;
using OcharoMarketing.Tests.Support;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Import;

/// <summary>
/// <c>import-sqlite</c>, run as the image runs it (<see cref="MarketingPostgres.RunVerbAsync"/>), against a store built
/// from the retired EF migrations: one transaction copies every table with its ids, the per-table counts and SHA-256s
/// from SQLite and from PostgreSQL read back must match before it commits, and the committed store is renamed aside.
/// </summary>
public sealed class ImportSqliteTests : IAsyncLifetime
{
    private readonly MarketingDatabase database = new();
    private readonly string root =
        Path.Combine(Path.GetTempPath(), "ocharo-marketing-import", Guid.NewGuid().ToString("N"));

    private string Store => Path.Combine(root, "marketing.db");

    public Task InitializeAsync() => database.InitializeAsync();

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    [Fact]
    public async Task Import_ShouldCopyEveryTableWithItsIdsAndRetireTheStore()
    {
        LegacyStore.Create(Store);

        var (exit, output, result) = await ImportAsync();

        exit.Should().Be(0);
        (result.GetProperty("outcome").GetString()).Should().Be("imported");
        (result.GetProperty("dryRun").GetBoolean()).Should().BeFalse();
        var tables = result.GetProperty("tables").EnumerateArray().ToArray();
        (tables.Select(table => table.GetProperty("table").GetString()))
            .Should()
            .Equal(SqliteImportTables.All.Select(table => table.Target));
        tables.Should().AllSatisfy(table =>
        {
            (table.GetProperty("matches").GetBoolean()).Should().BeTrue();
            var name = table.GetProperty("table").GetString()!;
            (table.GetProperty("targetRows").GetInt32()).Should().Be(LegacyStore.Rows[name]);
            (table.GetProperty("targetSha256").GetString()).Should().Be(table.GetProperty("sourceSha256").GetString());
        });
        output.Should().NotContain(LegacyStore.RowMarker);
        File.Exists(Store).Should().BeFalse();
        var retired = (result.GetProperty("retired").EnumerateArray()).Should().ContainSingle().Which.GetString()!;
        retired.Should().MatchRegex(@"^marketing\.imported-\d{14}\.db$");
        File.Exists(Path.Combine(root, retired)).Should().BeTrue();

        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        ((await repository.FindAssetAsync("front", default))!.FileId).Should().Be("file-0f2c");
        ((await repository.FindAssetAsync("legacy", default))!.FileId).Should().BeNull();
        ((await repository.FindExportFileAsync("draft-1", 2, default))!.FileId).Should().Be("file-export-1");
        var products = await repository.GetProductsAsync(default);
        products.Select(product => product.Id).Should().Equal(new[] { "abaya", "nimcha" });
        var requested = await db.GenerationJobs.FindAsync("job-1");
        requested!.RequestedBySubject.Should().Be(MarketingPostgres.Member.ToString("D"));
        var job = await db.GenerationJobs.FindAsync("job-2");
        (job!.CreatedAt, job.LeaseExpiresAt!.Value).Should().Be((1790451406144L, 1790452306144L));
        var analysis = await db.SourceAnalyses.FindAsync("sa-1");
        // SQLite kept 100 ns ticks at +05:00; PostgreSQL keeps the UTC instant to the microsecond.
        var created = new DateTimeOffset(2026, 10, 5, 7, 34, 56, TimeSpan.Zero).AddTicks(1_234_560);
        analysis!.CreatedAt.Should().Be(created);
        var name = await ScalarAsync("SELECT json->>'name' FROM marketing.metadata WHERE id = 'inquiry:a1'");
        name.Should().Be("Жилет \"quoted\"");
    }

    [Fact]
    public async Task DryRun_ShouldVerifyTheCopyThenRollBackAndRenameNothing()
    {
        LegacyStore.Create(Store);

        var (exit, _, result) = await ImportAsync("--dry-run");

        exit.Should().Be(0);
        (result.GetProperty("outcome").GetString()).Should().Be("verified");
        (result.GetProperty("dryRun").GetBoolean()).Should().BeTrue();
        (result.GetProperty("tables").EnumerateArray())
            .Should()
            .AllSatisfy(table => (table.GetProperty("matches").GetBoolean()).Should().BeTrue());
        (result.GetProperty("retired").EnumerateArray()).Should().BeEmpty();
        File.Exists(Store).Should().BeTrue();
        (await ScalarAsync(
            "SELECT (SELECT COUNT(*) FROM marketing.products) + (SELECT COUNT(*) FROM marketing.metadata)"))
                .Should()
                .Be(0L);
    }

    [Fact]
    public async Task Import_ShouldRefuseAndChangeNothing_WhenTheTargetHoldsOtherRows()
    {
        LegacyStore.Create(Store);
        await ScalarAsync("""
            INSERT INTO marketing.products (id, json) VALUES ('fresh', '{"id":"fresh"}') RETURNING 1
            """);

        var (exit, _, result) = await ImportAsync();

        exit.Should().Be(1);
        (result.GetProperty("outcome").GetString()).Should().Be("refused");
        File.Exists(Store).Should().BeTrue();
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.products")).Should().Be(1L);
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.metadata")).Should().Be(0L);
    }

    [Fact]
    public async Task Import_ShouldFindNothingToImport_WhenThereIsNoStore()
    {
        var (exit, _, result) = await ImportAsync();

        exit.Should().Be(0);
        (result.GetProperty("outcome").GetString()).Should().Be("nothing-to-import");
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.products")).Should().Be(0L);
    }

    [Fact]
    public async Task Import_ShouldRetireWithoutCopyingAgain_WhenTheStoreWasAlreadyCopied()
    {
        LegacyStore.Create(Store);
        var (_, _, first) = await ImportAsync();
        // A run that committed but stopped before renaming leaves the store beside the copy.
        File.Copy(Path.Combine(root, first.GetProperty("retired")[0].GetString()!), Store);

        var (exit, _, result) = await ImportAsync();

        exit.Should().Be(0);
        (result.GetProperty("outcome").GetString()).Should().Be("already-imported");
        File.Exists(Store).Should().BeFalse();
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.products")).Should().Be(2L);
    }

    [Fact]
    public async Task Import_ShouldReadMissingFilesIdsAsNull_WhenTheStoreIsOlder()
    {
        LegacyStore.Create(Store, lastLevel: false);

        var (exit, _, result) = await ImportAsync();

        exit.Should().Be(0);
        (result.GetProperty("outcome").GetString()).Should().Be("imported");
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.assets WHERE file_id IS NOT NULL")).Should().Be(0L);
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.export_files")).Should().Be(0L);
    }

    [Fact]
    public async Task Import_ShouldRefuseAStoreTableItDoesNotKnow_WhenItHoldsRows()
    {
        LegacyStore.Create(Store);
        LegacyStore.Execute(Store, """CREATE TABLE "Campaigns" ("Id" TEXT); INSERT INTO "Campaigns" VALUES ('c1');""");

        var (exit, _, result) = await ImportAsync();

        exit.Should().Be(1);
        (result.GetProperty("reason").GetString()).Should().Contain("Campaigns");
        File.Exists(Store).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_ShouldFailCountingButNeverPrintingThem_WhenIdentityLacksTheRequesters(bool dryRun)
    {
        const string Unknown = "0f9e8d7c-6b5a-4f3e-8d2c-1b0a9f8e7d6c", Legacy = "old-local-sub";
        LegacyStore.Create(Store);
        LegacyStore.Execute(Store, $$"""
            UPDATE "GenerationJobs" SET "RequestedBySubject" = '{{Unknown}}',
                "RequestedInOrganization" = '7c1e4b2a-9d3f-4e6a-8b5c-2f1a0d9e8c7b' WHERE "Id" = 'job-2';
            INSERT INTO "GenerationJobs" VALUES ('job-3', 'card', 'completed', '{}', '{"id":"job-3"}',
                1790451402907, NULL, NULL, 1, NULL, '{{Legacy}}', '7c1e4b2a-9d3f-4e6a-8b5c-2f1a0d9e8c7b');
            """);
        string[] arguments = dryRun ? ["--dry-run"] : [];

        var (exit, output, result) = await ImportAsync(arguments);

        exit.Should().Be(2);
        (result.GetProperty("outcome").GetString()).Should().Be("unresolved");
        (result.GetProperty("reason").GetString())
            .Should()
            .Be("generation_jobs.requested_by_subject holds 2 id(s) Identity does not hold; rolled back.");
        output.Should().NotContainEquivalentOf(Unknown);
        output.Should().NotContain(Legacy);
        File.Exists(Store).Should().BeTrue();
        (await ScalarAsync("SELECT COUNT(*) FROM marketing.generation_jobs")).Should().Be(0L);
    }

    [Theory]
    [InlineData("1.50", "1.50")]
    [InlineData("1.50E+1", "15.0")]
    [InlineData("1e2", "100")]
    [InlineData("1.5e-3", "0.0015")]
    [InlineData("-0.0", "0.0")]
    [InlineData("-12", "-12")]
    [InlineData("123456789012345678901234567890", "123456789012345678901234567890")]
    public async Task Numbers_ShouldReadAsJsonbPrintsThem(string raw, string expected)
    {
        ImportDigest.Number(raw).Should().Be(expected);
        (await ScalarAsync($"SELECT ('{raw}'::jsonb)::text")).Should().Be(expected);
    }

    /// <summary>Runs the verb with the store in the data folder and returns its exit, output and parsed result
    /// line.</summary>
    private async Task<(int Exit, string Output, JsonElement Result)> ImportAsync(params string[] arguments)
    {
        var settings = MarketingTestIdentity.Settings();
        settings["DatabaseSettings:ConnectionString"] = database.ConnectionString;
        settings["Marketing:DataDirectory"] = root;
        var (exit, output, _) = await MarketingPostgres.RunVerbAsync(settings, ["import-sqlite", .. arguments]);
        var line = output.TrimEnd().Split('\n')[^1];
        line.Should().StartWith("import-result ");
        return (exit, output, JsonDocument.Parse(line["import-result ".Length..]).RootElement.Clone());
    }

    private async Task<object?> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
