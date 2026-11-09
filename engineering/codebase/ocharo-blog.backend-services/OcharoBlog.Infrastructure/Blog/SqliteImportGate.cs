using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Holds Editorial's writes back while the SQLite store an earlier release kept still waits for its one-time
/// <c>import-sqlite</c>, so the empty <c>blog</c> schema never takes a row the import would then refuse.</summary>
/// <remarks>
///   - the store is pending while its file exists; the import renames it, and this host sees that without a restart
///   - once the file is gone the gate stays open for the life of the process
///   - a host with no store configured (<c>:memory:</c> or blank) is never pending
/// </remarks>
/// <param name="storePath">The store's full path, or null when none is configured.</param>
public sealed class SqliteImportGate(string? storePath)
{
    private readonly ConcurrentDictionary<string, bool> logged = new(StringComparer.Ordinal);
    private volatile bool retired = storePath is null;

    /// <summary>Gets the store's full path, or null when none is configured.</summary>
    public string? StorePath { get; } = storePath;

    /// <summary>Gets whether the store still waits for its import.</summary>
    public bool IsPending
    {
        get
        {
            if (retired)
                return false;
            if (File.Exists(StorePath))
                return true;
            retired = true;
            return false;
        }
    }

    /// <summary>Tells whether <paramref name="work"/> must skip because the import is pending, logging one line the
    /// first time it skips.</summary>
    /// <param name="work">What skips, such as <c>seeding</c>.</param>
    /// <param name="logger">Receives the line.</param>
    public bool Holds(string work, ILogger logger)
    {
        if (!IsPending)
            return false;
        if (logged.TryAdd(work, true))
            logger.LogInformation("Editorial skips {Work}: the SQLite store at {Store} waits for import-sqlite.",
                work, Path.GetFileName(StorePath));
        return true;
    }
}
