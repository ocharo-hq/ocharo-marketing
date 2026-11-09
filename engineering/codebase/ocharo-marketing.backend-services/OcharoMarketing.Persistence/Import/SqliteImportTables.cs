namespace OcharoMarketing.Persistence.Import;

/// <summary>How a copied column is held on each side, which fixes the canonical value both sides hash to.</summary>
public enum ImportColumnKind
{
    /// <summary>SQLite <c>TEXT</c> to <c>text</c>; the string itself.</summary>
    Text,

    /// <summary>SQLite <c>TEXT</c> holding JSON to <c>jsonb</c>; the document with sorted keys and plain
    /// numbers.</summary>
    Document,

    /// <summary>SQLite <c>INTEGER</c> to <c>integer</c>; the number.</summary>
    Integer,

    /// <summary>SQLite <c>INTEGER</c> Unix milliseconds to <c>timestamptz</c>; microseconds since the Unix
    /// epoch.</summary>
    UnixMilliseconds,

    /// <summary>SQLite <c>TEXT</c> holding an EF Core <c>DateTimeOffset</c> to <c>timestamptz</c>; UTC microseconds
    /// since the Unix epoch, PostgreSQL's resolution.</summary>
    OffsetText,

    /// <summary>SQLite <c>TEXT</c> holding an Identity id to a <c>uuid</c> keyed into Identity; its lowercase <c>D</c>
    /// form. A text that is no uuid stays as it is, and the import counts it among the ids Identity does not
    /// hold.</summary>
    IdentityId,
}

/// <summary>One copied column: its SQLite name, its PostgreSQL name and kind, and whether it may be null.</summary>
/// <param name="Source">The column in the SQLite store.</param>
/// <param name="Target">The column in the <c>marketing</c> schema.</param>
/// <param name="Kind">How each side holds it.</param>
/// <param name="IsNullable">Whether a row may leave it null; a nullable column an older store lacks reads null.</param>
public sealed record ImportColumn(string Source, string Target, ImportColumnKind Kind, bool IsNullable = false);

/// <summary>One copied table, its columns in order and the key its rows sort by before hashing.</summary>
/// <param name="Source">The table in the SQLite store.</param>
/// <param name="Target">The table in the <c>marketing</c> schema.</param>
/// <param name="Columns">Every column the EF model maps, in the order the import copies them.</param>
/// <param name="KeyCount">How many leading columns form the primary key.</param>
public sealed record ImportTable(string Source, string Target, IReadOnlyList<ImportColumn> Columns, int KeyCount = 1);

/// <summary>Lists what <c>import-sqlite</c> copies: every table the SQLite-era EF model mapped, parents
/// first.</summary>
/// <remarks>The SQLite store carries no foreign keys and neither does the set, so the order only keeps referenced rows
/// (drafts, schedules, connections) ahead of the rows naming them. <c>data_protection_keys</c> is no import target: the
/// SQLite era kept its keys in files.</remarks>
public static class SqliteImportTables
{
    /// <summary>The SQLite tables the import reads past: EF's own bookkeeping.</summary>
    public static IReadOnlySet<string> Ignored { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "__EFMigrationsHistory",
        "__EFMigrationsLock",
    };

    /// <summary>Every copied table, parents first.</summary>
    public static IReadOnlyList<ImportTable> All { get; } =
    [
        new("Products", "products", [Key("Id", "id"), Json("Json", "json")]),
        new("Metadata", "metadata", [Key("Id", "id"), Json("Json", "json")]),
        new("Assets", "assets",
            [Key("Id", "id"), Json("Json", "json"), Text("BlobPath", "blob_path"), Text("FileId", "file_id", true)]),
        new("Drafts", "drafts",
        [
            Key("Id", "id"), Number("Revision", "revision"), Number("ApprovedRevision", "approved_revision", true),
            Json("Json", "json"),
        ]),
        new("DraftRevisions", "draft_revisions",
            [Key("Id", "id"), Text("DraftId", "draft_id"), Number("Revision", "revision"), Json("Json", "json")]),
        new("ExportFiles", "export_files",
        [
            Key("DraftId", "draft_id"), Number("Revision", "revision"), Text("Sha256", "sha256"),
            Text("FileId", "file_id"),
        ], KeyCount: 2),
        new("Schedules", "schedules",
        [
            Key("Id", "id"), Text("DraftId", "draft_id"), Number("DraftRevision", "draft_revision"),
            Milliseconds("ScheduledAt", "scheduled_at"), Text("Status", "status"), Json("Json", "json"),
        ]),
        new("GenerationJobs", "generation_jobs",
        [
            Key("Id", "id"), Text("Kind", "kind"), Text("Status", "status"), Json("PayloadJson", "payload_json"),
            Json("Json", "json"), Milliseconds("CreatedAt", "created_at"),
            Milliseconds("LeaseExpiresAt", "lease_expires_at", true), Text("ClaimToken", "claim_token", true),
            Number("Attempts", "attempts"), Json("CheckpointJson", "checkpoint_json", true),
            Identity("RequestedBySubject", "requested_by_subject"),
            Identity("RequestedInOrganization", "requested_in_organization"),
        ]),
        new("DeliveryConnections", "delivery_connections",
        [
            Key("Id", "id"), Number("Revision", "revision"), Text("VerificationStatus", "verification_status"),
            Text("SafeMessage", "safe_message"), Text("ConfigurationStamp", "configuration_stamp"),
            Json("Json", "json"),
        ]),
        new("PublicationOperations", "publication_operations",
        [
            Key("Id", "id"), Text("ScheduleId", "schedule_id"), Text("Provider", "provider"),
            Text("ConnectionId", "connection_id"), Number("ConnectionRevision", "connection_revision"),
            Text("ConfigurationStamp", "configuration_stamp"), Text("Status", "status"),
            Milliseconds("DueAt", "due_at"), Text("ClaimToken", "claim_token", true),
            Milliseconds("LeaseExpiresAt", "lease_expires_at", true),
            Milliseconds("DispatchStartedAt", "dispatch_started_at", true), Number("Attempts", "attempts"),
            Json("Json", "json"),
        ]),
        new("MetricObservations", "metric_observations",
        [
            Key("Id", "id"), Text("ScheduleId", "schedule_id"), Text("Name", "name"),
            Milliseconds("RecordedAt", "recorded_at"), Json("Json", "json"),
        ]),
        new("MetricReadAttempts", "metric_read_attempts",
        [
            Key("Id", "id"), Text("Purpose", "purpose"), Text("ScheduleId", "schedule_id"),
            Milliseconds("AttemptedAt", "attempted_at"), Json("Json", "json"),
        ]),
        new("SourceAnalyses", "source_analyses",
        [
            Key("Id", "id"), Number("Revision", "revision"), Text("Status", "status"), Text("Provider", "provider"),
            Text("Model", "model", true), Text("Locale", "locale"), Text("Prompt", "prompt"),
            Json("SourcesJson", "sources_json"), Text("RequestFingerprint", "request_fingerprint"),
            Text("SuggestedText", "suggested_text", true), Text("ReviewedText", "reviewed_text", true),
            Text("Message", "message"), Offset("CreatedAt", "created_at"), Offset("UpdatedAt", "updated_at"),
            Offset("ReviewedAt", "reviewed_at", true),
        ]),
    ];

    private static ImportColumn Key(string source, string target) => new(source, target, ImportColumnKind.Text);

    private static ImportColumn Text(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.Text, isNullable);

    private static ImportColumn Json(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.Document, isNullable);

    private static ImportColumn Number(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.Integer, isNullable);

    private static ImportColumn Milliseconds(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.UnixMilliseconds, isNullable);

    private static ImportColumn Offset(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.OffsetText, isNullable);

    private static ImportColumn Identity(string source, string target) =>
        new(source, target, ImportColumnKind.IdentityId, IsNullable: true);
}
