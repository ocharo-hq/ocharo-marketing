using System.Text.Json;
using OcharoMarketing.Infrastructure.Import;
using OcharoMarketing.Persistence.Import;

namespace OcharoMarketing.Api.Import;

/// <summary>The <c>import-sqlite [--dry-run]</c> verb: copies the SQLite store an earlier release kept into PostgreSQL
/// once (<see cref="SqliteImporter"/>), outside the <c>migrate</c> step.</summary>
/// <remarks>
///   - the store is <c>ConnectionStrings:OcharoMarketing</c>'s file, else <c>{Marketing:DataDirectory}/marketing.db</c>
///   - exits <c>0</c> imported, verified by a dry run, already imported or nothing to import; <c>1</c> refused before
///     any write; <c>2</c> failed and rolled back
///   - prints a summary, then one last line <c>import-result {json}</c>: tables, counts, hashes, <c>dryRun</c> and
///     outcome; never row data or a secret
/// </remarks>
public static class ImportSqliteCommand
{
    /// <summary>The verb's name, its first argument.</summary>
    public const string Name = "import-sqlite";

    private const string DryRunFlag = "--dry-run";

    private static readonly JsonSerializerOptions Line = new(JsonSerializerDefaults.Web);

    /// <summary>Runs the verb on the built host's services.</summary>
    /// <param name="services">The host's root provider.</param>
    /// <param name="arguments">The arguments after the verb: nothing, or <c>--dry-run</c>.</param>
    /// <param name="output">Standard output: the summary and the <c>import-result</c> line.</param>
    /// <param name="error">Standard error: the usage on a malformed call.</param>
    /// <param name="ct">Token to cancel the run.</param>
    public static async Task<int> RunAsync(
        IServiceProvider services,
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error,
        CancellationToken ct)
    {
        var dryRun = arguments is [DryRunFlag];
        var source = services.GetRequiredService<SqliteImportGate>().SourcePath;
        SqliteImportResult result;
        if (arguments.Count > 0 && !dryRun)
        {
            await error.WriteLineAsync($"Use {Name} [{DryRunFlag}].");
            result = new(1, "refused", false, Path.GetFileName(source), "Unknown arguments.", [], []);
        }
        else
        {
            result = await services.GetRequiredService<SqliteImporter>().RunAsync(source, dryRun, ct);
        }

        await output.WriteLineAsync(Summary(result));
        await output.WriteLineAsync("import-result " + JsonSerializer.Serialize(new
        {
            verb = Name,
            exit = result.Exit,
            dryRun = result.DryRun,
            outcome = result.Outcome,
            source = result.Source,
            reason = result.Reason,
            tables = result.Tables,
            retired = result.Retired,
        }, Line));
        return result.Exit;
    }

    /// <summary>Says in one line what the run did.</summary>
    private static string Summary(SqliteImportResult result)
    {
        var rows = result.Tables.Sum(table => table.SourceRows);
        var tables = result.Tables.Count;
        return result.Outcome switch
        {
            "imported" when result.Exit == 0 =>
                $"Imported {rows} rows in {tables} tables from {result.Source}; retired it as {result.Retired[0]}.",
            "verified" => $"Dry run: {rows} rows in {tables} tables copied and matched, then rolled back.",
            "already-imported" when result.DryRun =>
                $"{result.Source} is already imported; a run without {DryRunFlag} retires it.",
            "already-imported" when result.Exit == 0 =>
                $"{result.Source} was already imported; retired it as {result.Retired[0]}.",
            "nothing-to-import" => $"Nothing to import: no {result.Source} at the configured path.",
            _ => $"Import {result.Outcome}: {result.Reason}",
        };
    }
}
