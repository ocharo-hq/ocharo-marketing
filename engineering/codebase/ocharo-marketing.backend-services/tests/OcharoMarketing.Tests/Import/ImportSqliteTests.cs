using System.Text.Json;
using Npgsql;
using OcharoMarketing.Persistence.Import;
using OcharoMarketing.Persistence.Marketing.Repositories;
using OcharoMarketing.Tests.Marketing;
using Xunit;

namespace OcharoMarketing.Tests.Import;

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
    public async Task Import_ShouldCopyEveryTableWithItsIds_ThenRetireTheStore()
    {
        LegacyStore.Create(Store);

        var (exit, output, result) = await ImportAsync();

        Assert.Equal(0, exit);
        Assert.Equal("imported", result.GetProperty("outcome").GetString());
        Assert.False(result.GetProperty("dryRun").GetBoolean());
        var tables = result.GetProperty("tables").EnumerateArray().ToArray();
        Assert.Equal(SqliteImportTables.All.Select(table => table.Target),
            tables.Select(table => table.GetProperty("table").GetString()));
        Assert.All(tables, table =>
        {
            Assert.True(table.GetProperty("matches").GetBoolean());
            var name = table.GetProperty("table").GetString()!;
            Assert.Equal(LegacyStore.Rows[name], table.GetProperty("targetRows").GetInt32());
            Assert.Equal(table.GetProperty("sourceSha256").GetString(), table.GetProperty("targetSha256").GetString());
        });
        Assert.DoesNotContain(LegacyStore.RowMarker, output);
        Assert.False(File.Exists(Store));
        var retired = Assert.Single(result.GetProperty("retired").EnumerateArray()).GetString()!;
        Assert.Matches(@"^marketing\.imported-\d{14}\.db$", retired);
        Assert.True(File.Exists(Path.Combine(root, retired)));

        await using var db = database.NewContext();
        var repository = new MarketingRepository(db);
        Assert.Equal("file-0f2c", (await repository.FindAssetAsync("front", default))!.FileId);
        Assert.Null((await repository.FindAssetAsync("legacy", default))!.FileId);
        Assert.Equal("file-export-1", (await repository.FindExportFileAsync("draft-1", 2, default))!.FileId);
        var products = await repository.GetProductsAsync(default);
        Assert.Equal(new[] { "abaya", "nimcha" }, products.Select(product => product.Id));
        var requested = await db.GenerationJobs.FindAsync("job-1");
        Assert.Equal(MarketingPostgres.Member.ToString("D"), requested!.RequestedBySubject);
        var job = await db.GenerationJobs.FindAsync("job-2");
        Assert.Equal((1790451406144L, 1790452306144L), (job!.CreatedAt, job.LeaseExpiresAt!.Value));
        var analysis = await db.SourceAnalyses.FindAsync("sa-1");
        // SQLite kept 100 ns ticks at +05:00; PostgreSQL keeps the UTC instant to the microsecond.
        var created = new DateTimeOffset(2026, 10, 5, 7, 34, 56, TimeSpan.Zero).AddTicks(1_234_560);
        Assert.Equal(created, analysis!.CreatedAt);
        var name = await ScalarAsync("SELECT json->>'name' FROM marketing.metadata WHERE id = 'inquiry:a1'");
        Assert.Equal("Жилет \"quoted\"", name);
    }

    [Fact]
    public async Task DryRun_ShouldVerifyTheCopy_ThenRollBackAndRenameNothing()
    {
        LegacyStore.Create(Store);

        var (exit, _, result) = await ImportAsync("--dry-run");

        Assert.Equal(0, exit);
        Assert.Equal("verified", result.GetProperty("outcome").GetString());
        Assert.True(result.GetProperty("dryRun").GetBoolean());
        Assert.All(result.GetProperty("tables").EnumerateArray(),
            table => Assert.True(table.GetProperty("matches").GetBoolean()));
        Assert.Empty(result.GetProperty("retired").EnumerateArray());
        Assert.True(File.Exists(Store));
        Assert.Equal(0L, await ScalarAsync(
            "SELECT (SELECT COUNT(*) FROM marketing.products) + (SELECT COUNT(*) FROM marketing.metadata)"));
    }

    [Fact]
    public async Task Import_ShouldRefuseATargetThatAlreadyHoldsOtherRows_AndChangeNothing()
    {
        LegacyStore.Create(Store);
        await ScalarAsync("""
            INSERT INTO marketing.products (id, json) VALUES ('fresh', '{"id":"fresh"}') RETURNING 1
            """);

        var (exit, _, result) = await ImportAsync();

        Assert.Equal(1, exit);
        Assert.Equal("refused", result.GetProperty("outcome").GetString());
        Assert.True(File.Exists(Store));
        Assert.Equal(1L, await ScalarAsync("SELECT COUNT(*) FROM marketing.products"));
        Assert.Equal(0L, await ScalarAsync("SELECT COUNT(*) FROM marketing.metadata"));
    }

    [Fact]
    public async Task Import_ShouldFindNothingToImport_WithoutAStore()
    {
        var (exit, _, result) = await ImportAsync();

        Assert.Equal(0, exit);
        Assert.Equal("nothing-to-import", result.GetProperty("outcome").GetString());
        Assert.Equal(0L, await ScalarAsync("SELECT COUNT(*) FROM marketing.products"));
    }

    [Fact]
    public async Task Rerun_ShouldRetireAStoreItAlreadyCopied_WithoutCopyingAgain()
    {
        LegacyStore.Create(Store);
        var (_, _, first) = await ImportAsync();
        // A run that committed but stopped before renaming leaves the store beside the copy.
        File.Copy(Path.Combine(root, first.GetProperty("retired")[0].GetString()!), Store);

        var (exit, _, result) = await ImportAsync();

        Assert.Equal(0, exit);
        Assert.Equal("already-imported", result.GetProperty("outcome").GetString());
        Assert.False(File.Exists(Store));
        Assert.Equal(2L, await ScalarAsync("SELECT COUNT(*) FROM marketing.products"));
    }

    [Fact]
    public async Task Import_ShouldCopyAnOlderStore_ReadingItsMissingFilesIdsAsNull()
    {
        LegacyStore.Create(Store, lastLevel: false);

        var (exit, _, result) = await ImportAsync();

        Assert.Equal(0, exit);
        Assert.Equal("imported", result.GetProperty("outcome").GetString());
        Assert.Equal(0L, await ScalarAsync("SELECT COUNT(*) FROM marketing.assets WHERE file_id IS NOT NULL"));
        Assert.Equal(0L, await ScalarAsync("SELECT COUNT(*) FROM marketing.export_files"));
    }

    [Fact]
    public async Task Import_ShouldRefuseAStoreTableItDoesNotKnow_WhenItHoldsRows()
    {
        LegacyStore.Create(Store);
        LegacyStore.Execute(Store, """CREATE TABLE "Campaigns" ("Id" TEXT); INSERT INTO "Campaigns" VALUES ('c1');""");

        var (exit, _, result) = await ImportAsync();

        Assert.Equal(1, exit);
        Assert.Contains("Campaigns", result.GetProperty("reason").GetString());
        Assert.True(File.Exists(Store));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_ShouldFailOnRequestersIdentityDoesNotHold_CountingThemButNeverPrintingThem(bool dryRun)
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

        Assert.Equal(2, exit);
        Assert.Equal("unresolved", result.GetProperty("outcome").GetString());
        Assert.Equal("generation_jobs.requested_by_subject holds 2 id(s) Identity does not hold; rolled back.",
            result.GetProperty("reason").GetString());
        Assert.DoesNotContain(Unknown, output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Legacy, output, StringComparison.Ordinal);
        Assert.True(File.Exists(Store));
        Assert.Equal(0L, await ScalarAsync("SELECT COUNT(*) FROM marketing.generation_jobs"));
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
        Assert.Equal(expected, ImportDigest.Number(raw));
        Assert.Equal(expected, await ScalarAsync($"SELECT ('{raw}'::jsonb)::text"));
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
        Assert.StartsWith("import-result ", line);
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
