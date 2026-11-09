using System.Text.Json;
using OcharoBlog.Infrastructure.Blog;
using OcharoBlog.Persistence.Import;

namespace OcharoBlog.Api.Blog;

/// <summary>
/// The <c>import-sqlite [--dry-run]</c> verb: copies the SQLite store an earlier release kept
/// (<c>ConnectionStrings:OcharoBlog</c>) into the <c>blog</c> schema once, through <see cref="SqliteImporter"/>.
/// </summary>
/// <remarks>
///   - exits <c>0</c> once imported, verified by a dry run, already imported, or with no store to import
///   - exits <c>1</c> when it refuses before any write: a target that holds other rows, a schema <c>migrate</c> has not
///     made, a store it cannot copy faithfully, or an unknown argument
///   - exits <c>2</c> when the copy failed or read back differently, after rolling it back
///   - the human report goes to standard error; standard output carries only the last line,
///     <c>import-result {json}</c>: counts, hashes, <c>dryRun</c> and the outcome, never row data or a secret
/// </remarks>
public static class SqliteImportCommand
{
    /// <summary>The verb's name, the first process argument.</summary>
    public const string Name = "import-sqlite";

    private const string DryRunFlag = "--dry-run";

    private static readonly JsonSerializerOptions ResultJson = new(JsonSerializerDefaults.Web);

    /// <summary>Runs the verb on the built host's services.</summary>
    /// <param name="services">The host's root provider.</param>
    /// <param name="arguments">The arguments after the verb's name.</param>
    /// <param name="output">Standard output: the result line only.</param>
    /// <param name="error">Standard error: the human report.</param>
    /// <param name="ct">Token to cancel the run.</param>
    public static async Task<int> RunAsync(IServiceProvider services, IReadOnlyList<string> arguments,
        TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (arguments is not ([] or [DryRunFlag]))
        {
            await error.WriteLineAsync($"Usage: {Name} [{DryRunFlag}]");
            return await WriteAsync(output, error, new SqliteImportResult(1, "refused", false, null,
                $"Unknown arguments; usage: {Name} [{DryRunFlag}].", [], []));
        }

        var dryRun = arguments is [DryRunFlag];
        var store = services.GetRequiredService<SqliteImportGate>().StorePath;
        if (store is null)
            return await WriteAsync(output, error, new SqliteImportResult(0, "nothing-to-import", dryRun, null,
                "No SQLite store is configured.", [], []));

        SqliteImportResult result;
        try
        {
            await using var scope = services.CreateAsyncScope();
            result = await scope.ServiceProvider.GetRequiredService<SqliteImporter>().RunAsync(store, dryRun, ct);
        }
        catch (InvalidOperationException)
        {
            // The database connection setting is missing or malformed; its value never reaches the report.
            result = new SqliteImportResult(2, "failed", dryRun, Path.GetFileName(store),
                "The database connection could not be opened; check DatabaseSettings:ConnectionString.", [], []);
        }
        return await WriteAsync(output, error, result);
    }

    /// <summary>Writes the human report, then the result line, and returns the exit code.</summary>
    private static async Task<int> WriteAsync(TextWriter output, TextWriter error, SqliteImportResult result)
    {
        foreach (var table in result.Tables)
            await error.WriteLineAsync(
                $"{table.Table}: SQLite {table.SourceRows} rows {table.SourceSha256[..12]}, "
                + $"PostgreSQL {table.TargetRows} rows {table.TargetSha256[..12]}"
                + (table.Matches ? "" : " - differs"));
        foreach (var name in result.Retired)
            await error.WriteLineAsync($"retired as {name}");
        await error.WriteLineAsync(result.Reason is null
            ? $"{Name}: {result.Outcome}{(result.DryRun ? " (dry run, rolled back)" : "")}"
            : $"{Name}: {result.Outcome}: {result.Reason}");
        await output.WriteLineAsync($"import-result {JsonSerializer.Serialize(result, ResultJson)}");
        return result.Exit;
    }
}
