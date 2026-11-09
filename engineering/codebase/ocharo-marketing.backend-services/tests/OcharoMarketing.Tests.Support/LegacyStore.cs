using Microsoft.Data.Sqlite;

namespace OcharoMarketing.Tests.Support;

/// <summary>Builds the SQLite store an earlier release kept, from the retired EF migrations' DDL kept as fixtures, with
/// a row in every table, values that tell the copy apart included.</summary>
internal static class LegacyStore
{
    /// <summary>A value only the rows carry, so a test can prove the verb never prints row data.</summary>
    public const string RowMarker = "row-marker-do-not-print-7f3a";

    /// <summary>The account job-1's requester names, spelled in capitals as a token might carry it; the copy keeps it
    /// as the lowercase uuid Identity holds.</summary>
    public static readonly string Member = MarketingPostgres.Member.ToString("D").ToUpperInvariant();

    /// <summary>The rows <see cref="Fill"/> writes, by PostgreSQL table.</summary>
    public static IReadOnlyDictionary<string, int> Rows { get; } = new Dictionary<string, int>
    {
        ["products"] = 2, ["metadata"] = 2, ["assets"] = 2, ["drafts"] = 2, ["draft_revisions"] = 2,
        ["export_files"] = 1, ["schedules"] = 1, ["generation_jobs"] = 2, ["delivery_connections"] = 1,
        ["publication_operations"] = 1, ["metric_observations"] = 1, ["metric_read_attempts"] = 1,
        ["source_analyses"] = 1,
    };

    /// <summary>Creates the store at <paramref name="path"/> at the last EF level (or the third, without Files
    /// ids).</summary>
    public static void Create(string path, bool lastLevel = true, bool fill = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var connection = Open(path);
        Execute(connection, Fixture("marketing-sqlite-v3.sql"));
        if (lastLevel)
            Execute(connection, Fixture("marketing-sqlite-v4.sql"));
        if (fill)
            Fill(connection, lastLevel);
    }

    /// <summary>Runs SQL against an existing store.</summary>
    public static void Execute(string path, string sql)
    {
        using var connection = Open(path);
        Execute(connection, sql);
    }

    private static void Fill(SqliteConnection connection, bool lastLevel)
    {
        Execute(connection, $$"""
            INSERT INTO "Products" VALUES ('nimcha',
                '{"id":"nimcha","name":"Nimcha","price":130000.50,"tags":["a","b"]}');
            INSERT INTO "Products" VALUES ('abaya',
                '{ "name" : "Abaya",  "id":"abaya", "price": 1.5E+2, "size": null }');
            INSERT INTO "Metadata" VALUES ('brand', '{"name":"OCHARO","timezone":"Asia/Tashkent","handle":null}');
            INSERT INTO "Metadata" VALUES ('inquiry:a1',
                '{"name":"Жилет \"quoted\"","note":"{{RowMarker}}","z":-0.0,"n":1e-3,"b":true}');
            INSERT INTO "Assets" ("Id", "Json", "BlobPath")
                VALUES ('front', '{"id":"front","sha256":"aa"}', 'originals/front/aa');
            INSERT INTO "Assets" ("Id", "Json", "BlobPath")
                VALUES ('legacy', '{"id":"legacy","sha256":"bb"}', 'originals/legacy/bb');
            INSERT INTO "Drafts" VALUES ('draft-1', 2, 2, '{"id":"draft-1","revision":2,"caption":"Yangi"}');
            INSERT INTO "Drafts" VALUES ('draft-2', 1, NULL, '{"id":"draft-2","revision":1}');
            INSERT INTO "DraftRevisions" VALUES ('draft-1:1', 'draft-1', 1, '{"id":"draft-1","revision":1}');
            INSERT INTO "DraftRevisions" VALUES ('draft-1:2', 'draft-1', 2, '{"id":"draft-1","revision":2}');
            INSERT INTO "Schedules" VALUES ('plan-1', 'draft-1', 2, 1790624208276, 'planned', '{"id":"plan-1"}');
            INSERT INTO "GenerationJobs" VALUES ('job-1', 'card', 'completed', '{"html":"<p>a</p>"}', '{"id":"job-1"}',
                1790451402907, NULL, NULL, 1, NULL, '{{Member}}', '7c1e4b2a-9d3f-4e6a-8b5c-2f1a0d9e8c7b');
            INSERT INTO "GenerationJobs" VALUES ('job-2', 'image', 'running', '{"prompt":"x"}', '{"id":"job-2"}',
                1790451406144, 1790452306144, 'token-1', 2, '{"stage":"submitted"}', NULL, NULL);
            INSERT INTO "DeliveryConnections"
                VALUES ('x', 3, 'verified', 'Verified.', 'stamp-1', '{"connectionId":"x"}');
            INSERT INTO "PublicationOperations" VALUES ('op-1', 'plan-1', 'x', 'x', 3, 'stamp-1', 'queued',
                1790924400000, NULL, NULL, NULL, 0, '{"id":"op-1"}');
            INSERT INTO "MetricObservations" VALUES ('obs-1', 'plan-1', 'likes', 1790924500000, '{"value":12}');
            INSERT INTO "MetricReadAttempts" VALUES ('att-1', 'metrics', 'plan-1', 1790924500000, '{"status":"ok"}');
            INSERT INTO "SourceAnalyses" VALUES ('sa-1', 1, 'reviewed', 'openai', NULL, 'uz', 'Prompt', '[]', 'fp-1',
                'Suggested', 'Reviewed', 'Done', '2026-10-05 12:34:56.1234567+05:00', '2026-10-05 07:40:00+00:00',
                NULL);
            """);
        if (lastLevel)
        {
            Execute(connection, """
                UPDATE "Assets" SET "FileId" = 'file-0f2c' WHERE "Id" = 'front';
                INSERT INTO "ExportFiles" VALUES ('draft-1', 2, 'cc', 'file-export-1');
                """);
        }
    }

    private static SqliteConnection Open(string path)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Import", "Fixtures", name));
}
