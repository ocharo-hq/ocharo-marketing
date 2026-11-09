using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Npgsql;
using NpgsqlTypes;

namespace OcharoMarketing.Persistence.Import;

/// <summary>Copies the SQLite store an earlier release kept into the <c>marketing</c> schema once, proves the copy,
/// then retires the store's files.</summary>
/// <remarks>
///   - opens the store read-only; reads the tables <see cref="SqliteImportTables"/> lists, and refuses one it does not
///     know that holds rows, or a known table's unknown column that holds a value
///   - copies every table in one PostgreSQL transaction, ids kept, under an advisory lock, then resets identity
///     sequences
///   - before copying, fails with exit <c>2</c>, rolled back, when a column keyed into Identity holds ids Identity does
///     not hold: it names the table, the column and how many ids, never the ids
///   - before committing, hashes each table from SQLite and from PostgreSQL read back in that transaction
///     (<see cref="ImportDigest"/>); commits only when every table matches, else rolls back with exit <c>2</c>
///   - refuses with exit <c>1</c>, before any write, a target that already holds rows unless they are exactly the
///     store's (an import that committed but never retired the store), which it retires
///   - after the commit, renames <c>marketing.db</c> and its <c>-wal</c>, <c>-shm</c> and <c>-journal</c> to
///     <c>marketing.imported-{UTC yyyyMMddHHmmss}.db</c> and so on, the <c>.db</c> last; a name taken already
///     gains <c>-2</c>, <c>-3</c> and so on after the stamp
///   - a dry run does all of it, rolls back and renames nothing
/// </remarks>
public sealed class SqliteImporter(NpgsqlDataSource target, TimeProvider time)
{
    private const string LockName = "ocharo-marketing:import-sqlite";
    private const string NothingToImport = "nothing-to-import";
    private static readonly string[] Companions = ["-wal", "-shm", "-journal"];

    /// <summary>Runs the import of the store at <paramref name="sourcePath"/>.</summary>
    /// <param name="sourcePath">The SQLite file the earlier release used.</param>
    /// <param name="dryRun">Whether to roll back after verifying and rename nothing.</param>
    /// <param name="ct">Token to cancel the run.</param>
    public async Task<SqliteImportResult> RunAsync(string sourcePath, bool dryRun, CancellationToken ct)
    {
        var name = Path.GetFileName(sourcePath);
        if (!File.Exists(sourcePath))
            return new(0, NothingToImport, dryRun, name, "No SQLite store at the configured path.", [], []);

        IReadOnlyList<SourceTable> source;
        try
        {
            source = ReadSource(sourcePath);
        }
        catch (SourceRefusedException refused)
        {
            return new(1, "refused", dryRun, name, refused.Message, [], []);
        }
        catch (SqliteException exception)
        {
            var reason = $"SQLite error {exception.SqliteErrorCode} while reading the store.";
            return new(2, "failed", dryRun, name, reason, [], []);
        }

        var stage = "connecting";
        try
        {
            await using var connection = await target.OpenConnectionAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);
            stage = "locking";
            await ExecuteAsync(connection, $"SELECT pg_advisory_xact_lock(hashtextextended('{LockName}', 0))", ct);

            stage = "checking the schema";
            if (await MissingTableAsync(connection, ct) is { } missing)
                return new(1, "refused", dryRun, name,
                    $"Table {MarketingSchema.Name}.{missing} is missing; run migrate first.", [], []);

            stage = "reading the target";
            var existing = await DigestTargetAsync(connection, ct);
            if (existing.Any(table => table.Digest.Rows > 0))
            {
                var current = Compare(source, existing);
                await transaction.RollbackAsync(ct);
                if (!current.All(table => table.Matches))
                    return new(1, "refused", dryRun, name,
                        "The marketing schema already holds rows the store does not; nothing was copied.", current, []);
                return dryRun
                    ? new(0, "already-imported", dryRun, name, null, current, [])
                    : Retire(sourcePath, "already-imported", current);
            }

            stage = "resolving Identity ids";
            if (await UnresolvedAsync(connection, source, ct) is { } unresolved)
            {
                await transaction.RollbackAsync(ct);
                return new(2, "unresolved", dryRun, name, unresolved, [], []);
            }

            foreach (var table in source)
            {
                stage = $"copying {table.Table.Target}";
                await CopyAsync(connection, table, ct);
            }
            stage = "resetting identity sequences";
            await ResetSequencesAsync(connection, ct);

            stage = "reading the copy back";
            var tables = Compare(source, await DigestTargetAsync(connection, ct));
            if (!tables.All(table => table.Matches))
            {
                await transaction.RollbackAsync(ct);
                return new(2, "mismatch", dryRun, name,
                    "PostgreSQL read back differs from SQLite; rolled back.", tables, []);
            }
            if (dryRun)
            {
                await transaction.RollbackAsync(ct);
                return new(0, "verified", dryRun, name, null, tables, []);
            }

            stage = "committing";
            await transaction.CommitAsync(ct);
            return Retire(sourcePath, "imported", tables);
        }
        catch (PostgresException exception)
        {
            var reason = $"PostgreSQL {exception.SqlState} while {stage}; rolled back.";
            return new(2, "failed", dryRun, name, reason, [], []);
        }
        catch (NpgsqlException)
        {
            return new(2, "failed", dryRun, name, $"PostgreSQL could not be reached while {stage}.", [], []);
        }
        catch (JsonException)
        {
            return new(2, "failed", dryRun, name, $"A document is not JSON while {stage}; rolled back.", [], []);
        }
    }

    /// <summary>Renames the store and its companions to their imported names, the store itself last.</summary>
    private SqliteImportResult Retire(string sourcePath, string outcome, IReadOnlyList<SqliteImportTableResult> tables)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
        var stamp = time.GetUtcNow().UtcDateTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var stem = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        var retired = Path.Combine(folder, $"{stem}.imported-{stamp}{extension}");
        for (var copy = 2; Companions.Prepend(string.Empty).Any(suffix => File.Exists(retired + suffix)); copy++)
            retired = Path.Combine(folder, $"{stem}.imported-{stamp}-{copy}{extension}");
        var names = new List<string>();
        try
        {
            foreach (var suffix in Companions.Where(suffix => File.Exists(sourcePath + suffix)))
            {
                File.Move(sourcePath + suffix, retired + suffix);
                names.Add(Path.GetFileName(retired + suffix));
            }
            File.Move(sourcePath, retired);
            names.Insert(0, Path.GetFileName(retired));
        }
        catch (IOException)
        {
            return new(2, outcome, false, Path.GetFileName(sourcePath),
                "The copy committed, but the store could not be renamed; rerun to retire it.", tables, names);
        }
        catch (UnauthorizedAccessException)
        {
            return new(2, outcome, false, Path.GetFileName(sourcePath),
                "The copy committed, but the store may not be renamed here; rerun to retire it.", tables, names);
        }
        return new(0, outcome, false, Path.GetFileName(sourcePath), null, tables, names);
    }

    /// <summary>Reads every listed table of the store, read-only, in one read transaction.</summary>
    private static IReadOnlyList<SourceTable> ReadSource(string path)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: true);

        var present = Strings(connection, "SELECT name FROM sqlite_master WHERE type = 'table'")
            .Where(table => !table.StartsWith("sqlite_", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        foreach (var unknown in present.Where(table => !SqliteImportTables.Ignored.Contains(table)
                     && SqliteImportTables.All.All(known => known.Source != table)))
        {
            if (Count(connection, $"SELECT COUNT(*) FROM {Quote(unknown)}") > 0)
                throw new SourceRefusedException($"The store's table {unknown} holds rows the import does not know.");
        }

        var tables = new List<SourceTable>();
        foreach (var table in SqliteImportTables.All)
        {
            if (!present.Contains(table.Source))
            {
                tables.Add(new SourceTable(table, []));
                continue;
            }
            var columns = Strings(connection, $"SELECT name FROM pragma_table_info('{table.Source}')")
                .ToHashSet(StringComparer.Ordinal);
            foreach (var extra in columns.Where(column => table.Columns.All(known => known.Source != column)))
            {
                var holding = $"SELECT COUNT(*) FROM {Quote(table.Source)} WHERE {Quote(extra)} IS NOT NULL";
                if (Count(connection, holding) > 0)
                    throw new SourceRefusedException(
                        $"The store's column {table.Source}.{extra} holds values the import does not know.");
            }
            var lacking = table.Columns.FirstOrDefault(
                column => !columns.Contains(column.Source) && !column.IsNullable);
            if (lacking is not null)
                throw new SourceRefusedException(
                    $"The store's table {table.Source} lacks its column {lacking.Source}.");

            var read = table.Columns.Select(column => columns.Contains(column.Source) ? Quote(column.Source) : "NULL");
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {string.Join(", ", read)} FROM {Quote(table.Source)}";
            using var reader = command.ExecuteReader();
            var rows = new List<object?[]>();
            while (reader.Read())
            {
                var row = new object?[table.Columns.Count];
                for (var index = 0; index < row.Length; index++)
                    row[index] = FromSqlite(reader, index, table, table.Columns[index]);
                rows.Add(row);
            }
            tables.Add(new SourceTable(table, rows));
        }
        return tables;
    }

    /// <summary>Reads one SQLite value into its canonical form, refusing a value its column cannot hold.</summary>
    private static object? FromSqlite(SqliteDataReader reader, int index, ImportTable table, ImportColumn column)
    {
        if (reader.IsDBNull(index))
            return column.IsNullable ? null : throw Refused(table, column, "is null");
        var value = reader.GetValue(index);
        switch (column.Kind)
        {
            case ImportColumnKind.Text when value is string text:
                return text;
            case ImportColumnKind.Document when value is string document:
                try
                {
                    using var _ = JsonDocument.Parse(document);
                }
                catch (JsonException)
                {
                    throw Refused(table, column, "holds a value that is not JSON");
                }
                return document;
            case ImportColumnKind.Integer when value is long number:
                return number is >= int.MinValue and <= int.MaxValue
                    ? number
                    : throw Refused(table, column, "holds a number beyond a 32-bit integer");
            case ImportColumnKind.UnixMilliseconds when value is long milliseconds:
                return milliseconds is >= -62_135_596_800_000 and <= 253_402_300_799_999
                    ? milliseconds * 1000
                    : throw Refused(table, column, "holds an instant out of range");
            case ImportColumnKind.IdentityId when value is string text:
                return Guid.TryParse(text, out var id) ? id.ToString("D") : text;
            case ImportColumnKind.OffsetText when value is string text && DateTimeOffset.TryParse(
                text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at):
                return Microseconds(at.UtcDateTime);
            default:
                throw Refused(table, column, $"holds a {value.GetType().Name} where {column.Kind} belongs");
        }
    }

    /// <summary>Copies one table's rows by binary COPY, keeping every id.</summary>
    private static async Task CopyAsync(NpgsqlConnection connection, SourceTable table, CancellationToken ct)
    {
        if (table.Rows.Count == 0)
            return;
        var columns = string.Join(", ", table.Table.Columns.Select(column => column.Target));
        await using var copy = await connection.BeginBinaryImportAsync(
            $"COPY {MarketingSchema.Name}.{table.Table.Target} ({columns}) FROM STDIN (FORMAT BINARY)", ct);
        foreach (var row in table.Rows)
        {
            await copy.StartRowAsync(ct);
            for (var index = 0; index < row.Length; index++)
            {
                var value = row[index];
                var task = (value, table.Table.Columns[index].Kind) switch
                {
                    (null, _) => copy.WriteNullAsync(ct),
                    (string text, ImportColumnKind.Document) => copy.WriteAsync(text, NpgsqlDbType.Jsonb, ct),
                    (string id, ImportColumnKind.IdentityId) => copy.WriteAsync(Guid.Parse(id), NpgsqlDbType.Uuid, ct),
                    (string text, _) => copy.WriteAsync(text, NpgsqlDbType.Text, ct),
                    (long number, ImportColumnKind.Integer) => copy.WriteAsync((int)number, NpgsqlDbType.Integer, ct),
                    (long micros, _) => copy.WriteAsync(Instant(micros), NpgsqlDbType.TimestampTz, ct),
                    _ => throw new InvalidOperationException("A value holds no copyable form."),
                };
                await task;
            }
        }
        await copy.CompleteAsync(ct);
    }

    /// <summary>Moves each identity column's sequence past the rows the copy kept, so the next insert never
    /// collides.</summary>
    private static async Task ResetSequencesAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var identities = new List<(string Table, string Column)>();
        await using (var command = new NpgsqlCommand(
            "SELECT table_name, column_name FROM information_schema.columns "
            + "WHERE table_schema = $1 AND is_identity = 'YES' AND table_name = ANY($2)", connection))
        {
            command.Parameters.Add(new NpgsqlParameter { Value = MarketingSchema.Name });
            var tables = SqliteImportTables.All.Select(table => table.Target).ToArray();
            command.Parameters.Add(new NpgsqlParameter { Value = tables });
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                identities.Add((reader.GetString(0), reader.GetString(1)));
        }
        foreach (var (table, column) in identities)
        {
            var qualified = $"{MarketingSchema.Name}.{table}";
            await ExecuteAsync(connection, $"SELECT setval(pg_get_serial_sequence('{qualified}', '{column}'), "
                + $"COALESCE(MAX({column}), 0) + 1, false) FROM {qualified}", ct);
        }
    }

    /// <summary>Counts, per column keyed into Identity, the distinct ids the referenced Identity table does not hold, a
    /// text that is no uuid included; null when every id resolves.</summary>
    /// <remarks>The foreign keys name the referenced tables, so the schema Identity's set claimed decides, never a
    /// guess. The reason names the table, the column and the count, never an id.</remarks>
    private static async Task<string?> UnresolvedAsync(
        NpgsqlConnection connection, IReadOnlyList<SourceTable> source, CancellationToken ct)
    {
        var problems = new List<string>();
        foreach (var table in source.Where(table => table.Rows.Count > 0))
        {
            var references = await ReferencesAsync(connection, table.Table.Target, ct);
            for (var index = 0; index < table.Table.Columns.Count; index++)
            {
                var column = table.Table.Columns[index];
                if (column.Kind != ImportColumnKind.IdentityId
                    || !references.TryGetValue(column.Target, out var target))
                    continue;
                var ids = table.Rows.Select(row => row[index]).OfType<string>()
                    .Distinct(StringComparer.Ordinal).ToList();
                var uuids = ids.Select(id => Guid.TryParse(id, out var uuid) ? uuid : (Guid?)null).ToList();
                await using var command = new NpgsqlCommand(
                    $"SELECT count(*) FROM unnest($1::uuid[]) AS ids(id) WHERE NOT EXISTS "
                    + $"(SELECT 1 FROM {target} AS referenced WHERE referenced.id = ids.id)", connection);
                command.Parameters.Add(new NpgsqlParameter
                {
                    Value = uuids.OfType<Guid>().ToArray(),
                    NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Uuid,
                });
                var missing = (long)(await command.ExecuteScalarAsync(ct))! + uuids.Count(uuid => uuid is null);
                if (missing > 0)
                    problems.Add($"{table.Table.Target}.{column.Target} holds {missing} id(s) Identity does not hold");
            }
        }
        return problems.Count == 0 ? null : string.Join("; ", problems) + "; rolled back.";
    }

    /// <summary>Maps each column of a table that a foreign key keys into another table to that table's qualified
    /// name.</summary>
    private static async Task<Dictionary<string, string>> ReferencesAsync(
        NpgsqlConnection connection, string table, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT a.attname::text, quote_ident(n.nspname) || '.' || quote_ident(r.relname)
            FROM pg_constraint c
            JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = c.conkey[1]
            JOIN pg_class r ON r.oid = c.confrelid
            JOIN pg_namespace n ON n.oid = r.relnamespace
            WHERE c.contype = 'f' AND c.conrelid = to_regclass($1)
            """, connection);
        command.Parameters.Add(new NpgsqlParameter { Value = $"{MarketingSchema.Name}.{table}" });
        var references = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            references[reader.GetString(0)] = reader.GetString(1);
        return references;
    }

    /// <summary>Names the first listed table the schema lacks, or null.</summary>
    private static async Task<string?> MissingTableAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        foreach (var table in SqliteImportTables.All)
        {
            await using var command = new NpgsqlCommand("SELECT to_regclass($1) IS NULL", connection);
            command.Parameters.Add(new NpgsqlParameter { Value = $"{MarketingSchema.Name}.{table.Target}" });
            if (await command.ExecuteScalarAsync(ct) is true)
                return table.Target;
        }
        return null;
    }

    /// <summary>Reads every listed table from PostgreSQL in canonical form and hashes it.</summary>
    private static async Task<IReadOnlyList<(ImportTable Table, ImportDigest Digest)>> DigestTargetAsync(
        NpgsqlConnection connection, CancellationToken ct)
    {
        var digests = new List<(ImportTable, ImportDigest)>();
        foreach (var table in SqliteImportTables.All)
        {
            var columns = string.Join(", ", table.Columns.Select(column => column.Target));
            await using var command = new NpgsqlCommand(
                $"SELECT {columns} FROM {MarketingSchema.Name}.{table.Target}", connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var rows = new List<object?[]>();
            while (await reader.ReadAsync(ct))
            {
                var row = new object?[table.Columns.Count];
                for (var index = 0; index < row.Length; index++)
                    row[index] = await reader.IsDBNullAsync(index, ct) ? null : table.Columns[index].Kind switch
                    {
                        ImportColumnKind.Integer => (long)reader.GetInt32(index),
                        ImportColumnKind.IdentityId => reader.GetGuid(index).ToString("D"),
                        ImportColumnKind.UnixMilliseconds or ImportColumnKind.OffsetText =>
                            Microseconds(reader.GetFieldValue<DateTime>(index)),
                        _ => reader.GetString(index),
                    };
                rows.Add(row);
            }
            digests.Add((table, ImportDigest.Of(table, rows)));
        }
        return digests;
    }

    /// <summary>Pairs the store's digests with the target's, table by table.</summary>
    private static IReadOnlyList<SqliteImportTableResult> Compare(
        IReadOnlyList<SourceTable> source, IReadOnlyList<(ImportTable Table, ImportDigest Digest)> target) =>
        source.Zip(target, (from, to) =>
        {
            var digest = ImportDigest.Of(from.Table, from.Rows);
            return new SqliteImportTableResult(
                from.Table.Target, digest.Rows, digest.Sha256, to.Digest.Rows, to.Digest.Sha256);
        }).ToArray();

    private static long Microseconds(DateTime utc) => (long)Math.Floor(
        (utc.ToUniversalTime().Ticks - DateTime.UnixEpoch.Ticks) / (decimal)TimeSpan.TicksPerMicrosecond);

    private static DateTime Instant(long microseconds) =>
        DateTime.UnixEpoch.AddTicks(microseconds * TimeSpan.TicksPerMicrosecond);

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static List<string> Strings(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read())
            values.Add(reader.GetString(0));
        return values;
    }

    private static long Count(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    private static string Quote(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static SourceRefusedException Refused(ImportTable table, ImportColumn column, string problem) =>
        new($"The store's column {table.Source}.{column.Source} {problem}.");

    /// <summary>One table as the store holds it, each value in canonical form.</summary>
    private sealed record SourceTable(ImportTable Table, IReadOnlyList<object?[]> Rows);

    /// <summary>Stops the read before any write: the store holds what the import cannot copy faithfully.</summary>
    private sealed class SourceRefusedException(string message) : Exception(message);
}
