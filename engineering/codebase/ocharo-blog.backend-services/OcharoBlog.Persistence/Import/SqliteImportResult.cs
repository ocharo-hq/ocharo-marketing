namespace OcharoBlog.Persistence.Import;

/// <summary>What one <c>import-sqlite</c> run did: the <c>import-result</c> line's content.</summary>
/// <param name="Exit">The exit code: <c>0</c> done or nothing to do, <c>1</c> refused before any write, <c>2</c> failed
/// and rolled back.</param>
/// <param name="Outcome">One word: <c>nothing-to-import</c>, <c>imported</c>, <c>verified</c> (a dry run that matched),
/// <c>already-imported</c>, <c>refused</c>, <c>mismatch</c> or <c>failed</c>.</param>
/// <param name="DryRun">Whether the run rolled back by request and renamed nothing.</param>
/// <param name="Source">The SQLite file's name, without its folder.</param>
/// <param name="Reason">Why a run refused or failed; never row data or a secret.</param>
/// <param name="Tables">Each copied table's counts and hashes, from SQLite and from PostgreSQL read back.</param>
/// <param name="Retired">The names the store's files took once the copy committed.</param>
public sealed record SqliteImportResult(
    int Exit,
    string Outcome,
    bool DryRun,
    string? Source,
    string? Reason,
    IReadOnlyList<SqliteImportTableResult> Tables,
    IReadOnlyList<string> Retired);

/// <summary>One table's digests on both sides.</summary>
/// <param name="Table">The table in the <c>blog</c> schema.</param>
/// <param name="SourceRows">The rows SQLite holds.</param>
/// <param name="SourceSha256">The SHA-256 over SQLite's rows in canonical form.</param>
/// <param name="TargetRows">The rows PostgreSQL holds, read back in the import's transaction.</param>
/// <param name="TargetSha256">The SHA-256 over PostgreSQL's rows in the same canonical form.</param>
public sealed record SqliteImportTableResult(
    string Table, int SourceRows, string SourceSha256, int TargetRows, string TargetSha256)
{
    /// <summary>Whether both sides hold the same rows.</summary>
    public bool Matches => SourceRows == TargetRows && SourceSha256 == TargetSha256;
}
