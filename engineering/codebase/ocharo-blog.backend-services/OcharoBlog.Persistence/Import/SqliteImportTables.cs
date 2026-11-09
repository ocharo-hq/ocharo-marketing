namespace OcharoBlog.Persistence.Import;

/// <summary>How a copied column is held on each side, which fixes the canonical value both sides hash to.</summary>
public enum ImportColumnKind
{
    /// <summary>SQLite <c>TEXT</c> to <c>text</c>; the string itself.</summary>
    Text,

    /// <summary>SQLite <c>TEXT</c> holding an EF Core <c>Guid</c> to <c>uuid</c>; its lowercase <c>D</c>
    /// form.</summary>
    Uuid,

    /// <summary>SQLite <c>TEXT</c> holding JSON to <c>jsonb</c>; the document with sorted keys and plain
    /// numbers.</summary>
    Document,

    /// <summary>SQLite <c>INTEGER</c> to <c>integer</c>; the number.</summary>
    Integer,

    /// <summary>SQLite <c>INTEGER</c> to <c>bigint</c>; the number.</summary>
    BigInteger,

    /// <summary>SQLite <c>INTEGER</c> holding an EF Core <c>DateTimeOffset</c> in its binary form (ticks in thousands
    /// and the offset in minutes) to <c>timestamptz</c>; UTC microseconds since the Unix epoch, PostgreSQL's
    /// resolution.</summary>
    BinaryInstant,
}

/// <summary>One copied column: its SQLite name, its PostgreSQL name and kind, and whether it may be null.</summary>
/// <param name="Source">The column in the SQLite store.</param>
/// <param name="Target">The column in the <c>blog</c> schema.</param>
/// <param name="Kind">How each side holds it.</param>
/// <param name="IsNullable">Whether a row may leave it null; a nullable column an older store lacks reads null.</param>
/// <param name="IdentityKey">The foreign key by which the column keys into Identity's published tables, or null; the
/// import proves every id it copies there is one Identity holds.</param>
public sealed record ImportColumn(
    string Source, string Target, ImportColumnKind Kind, bool IsNullable = false, string? IdentityKey = null);

/// <summary>One copied table, its columns in order and the key its rows sort by before hashing.</summary>
/// <param name="Source">The table in the SQLite store.</param>
/// <param name="Target">The table in the <c>blog</c> schema.</param>
/// <param name="Columns">Every column the EF model maps, in the order the import copies them.</param>
/// <param name="KeyCount">How many leading columns form the primary key.</param>
public sealed record ImportTable(string Source, string Target, IReadOnlyList<ImportColumn> Columns, int KeyCount = 1);

/// <summary>Lists what <c>import-sqlite</c> copies: every table the SQLite-era EF model and its boot DDL made, parents
/// first.</summary>
/// <remarks>Only the image receipts key into another of these tables (the media index), so the media index comes before
/// them; they key into Identity's organizations and accounts too, which the import checks before it copies.
/// <c>data_protection_keys</c> is no import target: the SQLite era kept its keys in files.</remarks>
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
        new("topics", "topics", [Text("Key", "key"), Number("SortOrder", "sort_order"), Json("Json", "json")]),
        new("site_settings", "site_settings", [Text("Id", "id"), Number("Revision", "revision"), Json("Json", "json")]),
        new("articles", "articles",
        [
            Uuid("Id", "id"), Text("Slug", "slug"), Text("Topic", "topic"), Text("Status", "status"),
            Instant("PublishAt", "publish_at", true), Instant("PublishedAt", "published_at", true),
            Number("Revision", "revision"), Instant("CreatedAt", "created_at"), Instant("UpdatedAt", "updated_at"),
            Json("Json", "json"),
        ]),
        new("article_revisions", "article_revisions",
        [
            Uuid("Id", "id"), Uuid("ArticleId", "article_id"), Number("Revision", "revision"),
            Instant("SavedAt", "saved_at"), Text("Summary", "summary"), Json("Json", "json"),
        ]),
        new("media_blobs", "media_blobs",
        [
            Text("Sha256", "sha256"), Text("FileName", "file_name"), Text("ContentType", "content_type"),
            new("ByteLength", "byte_length", ImportColumnKind.BigInteger), Text("BlobPath", "blob_path"),
            Text("PreviewPath", "preview_path", true), Number("Width", "width", true),
            Number("Height", "height", true), Instant("StoredAt", "stored_at"), Text("FileId", "file_id", true),
        ]),
        new("media_edits", "media_edits",
        [
            Text("Id", "id"), Text("SourceSha256", "source_sha256"), Text("Sha256", "sha256"),
            Json("RecipeJson", "recipe_json"),
            IdentityId("OrganizationId", "organization_id", "fk_media_edits_organization_id"),
            IdentityId("Actor", "actor", "fk_media_edits_actor"),
            Text("Model", "model", true), Instant("CreatedAt", "created_at"),
        ]),
    ];

    private static ImportColumn Text(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.Text, isNullable);

    private static ImportColumn Uuid(string source, string target) => new(source, target, ImportColumnKind.Uuid);

    private static ImportColumn IdentityId(string source, string target, string foreignKey) =>
        new(source, target, ImportColumnKind.Uuid, IdentityKey: foreignKey);

    private static ImportColumn Json(string source, string target) => new(source, target, ImportColumnKind.Document);

    private static ImportColumn Number(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.Integer, isNullable);

    private static ImportColumn Instant(string source, string target, bool isNullable = false) =>
        new(source, target, ImportColumnKind.BinaryInstant, isNullable);
}
