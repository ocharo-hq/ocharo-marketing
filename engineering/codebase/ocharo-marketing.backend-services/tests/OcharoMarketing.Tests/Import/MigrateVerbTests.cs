using Npgsql;
using Xunit;

namespace OcharoMarketing.Tests.Import;

/// <summary>The image's <c>migrate</c> verb, as the deploy step runs it through <c>Migrations:ConnectionString</c>: it
/// creates the database when missing, refuses until Identity's set is there, applies the <c>marketing</c> set and the
/// sessions' <c>distributed_cache</c> set,
/// each in its own schema of the shared product database, leaves <c>public</c> to its owner, and reruns as a
/// no-op.</summary>
public sealed class MigrateVerbTests
{
    [Fact]
    public async Task Migrate_ShouldApplyBothSetsInTheirOwnSchemas_AndRerunAsANoOp()
    {
        var database = await MarketingPostgres.EmptyDatabaseAsync();
        try
        {
            var settings = MarketingTestIdentity.Settings();
            settings["Migrations:ConnectionString"] = database;

            var withoutIdentity = await MarketingPostgres.RunVerbAsync(settings, "migrate");
            await MarketingPostgres.ApplyIdentityStubAsync(database);
            var first = await MarketingPostgres.RunVerbAsync(settings, "migrate");
            var again = await MarketingPostgres.RunVerbAsync(settings, "migrate");

            // A job's requester keys into Identity, so the step refuses until Identity's set publishes its tables.
            Assert.Equal(1, withoutIdentity.Exit);
            Assert.Equal(0, first.Exit);
            Assert.StartsWith("migrate-result ", first.Output.TrimEnd().Split('\n')[^1]);
            Assert.Equal(0, again.Exit);
            Assert.Equal(new[] { "data_protection_keys", "products", "source_analyses" },
                await TablesAsync(database, "marketing", "data_protection_keys", "products", "source_analyses"));
            Assert.Equal(new[] { "distributed_cache" },
                await TablesAsync(database, "marketing_cache", "distributed_cache"));
            Assert.Empty(await TablesAsync(database, "public"));
        }
        finally
        {
            await MarketingPostgres.DropDatabaseAsync(database);
        }
    }

    /// <summary>Lists a schema's tables, only the named ones when names are given.</summary>
    private static async Task<string[]> TablesAsync(string connectionString, string schema, params string[] names)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT table_name::text FROM information_schema.tables WHERE table_schema = $1 ORDER BY 1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = schema });
        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add(reader.GetString(0));
        return names.Length == 0 ? tables.ToArray() : tables.Where(names.Contains).ToArray();
    }
}
