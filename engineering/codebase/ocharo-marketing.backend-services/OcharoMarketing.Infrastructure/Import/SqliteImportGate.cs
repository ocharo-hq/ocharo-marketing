namespace OcharoMarketing.Infrastructure.Import;

/// <summary>Names the SQLite store an earlier release kept: <c>ConnectionStrings:OcharoMarketing</c>'s file, else
/// <c>{Marketing:DataDirectory}/marketing.db</c>.</summary>
/// <param name="Path">The store's full path.</param>
public sealed record SqliteImportSource(string Path);

/// <summary>Tells whether the SQLite store an earlier release kept still waits for <c>import-sqlite</c>: its unretired
/// file exists.</summary>
/// <remarks>
///   - while it waits, the data routes answer 503 <c>import_pending</c>, and the seed, the Files backfill and the
///     generation, publication and automation workers skip their work, so the import finds the schema empty
///   - the import renames the file once its copy commits, which opens the gate within <see cref="CheckEvery"/>
///   - the file is looked up at most once every <see cref="CheckEvery"/>
/// </remarks>
public sealed class SqliteImportGate(SqliteImportSource source, TimeProvider time)
{
    /// <summary>How long one look at the file holds.</summary>
    public static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(5);

    private readonly Lock gate = new();
    private DateTimeOffset checkedAt = DateTimeOffset.MinValue;
    private bool pending;

    /// <summary>Gets the store's path.</summary>
    public string SourcePath => source.Path;

    /// <summary>Gets whether the store's file still exists, as of the last look at most <see cref="CheckEvery"/>
    /// ago.</summary>
    public bool IsPending
    {
        get
        {
            var now = time.GetUtcNow();
            lock (gate)
            {
                if (checkedAt != DateTimeOffset.MinValue && now - checkedAt < CheckEvery)
                    return pending;
                pending = File.Exists(source.Path);
                checkedAt = now;
                return pending;
            }
        }
    }
}
