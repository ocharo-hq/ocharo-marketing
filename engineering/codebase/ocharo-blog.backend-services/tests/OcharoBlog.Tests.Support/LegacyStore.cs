using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Persistence;

namespace OcharoBlog.Tests.Support;

/// <summary>
/// Writes SQLite stores the way the SQLite-era release kept them: the tables its EF model and boot DDL made, in WAL
/// mode, Guids as upper-case text, instants in EF's binary form and documents as the release serialized them.
/// </summary>
public static class LegacyStore
{
    /// <summary>The rows one store holds.</summary>
    public sealed record Rows(
        IReadOnlyList<ArticleRecord> Articles,
        IReadOnlyList<ArticleRevisionRecord> Revisions,
        IReadOnlyList<TopicRecord> Topics,
        IReadOnlyList<SiteSettingsRecord> Settings,
        IReadOnlyList<MediaBlobRecord> Media,
        IReadOnlyList<MediaEditRecord> Edits);

    /// <summary>The schedule of the story typed with Tashkent's offset, as an editor would.</summary>
    public static readonly DateTimeOffset ScheduledAt = new(2026, 10, 1, 14, 30, 0, TimeSpan.FromHours(5));

    /// <summary>The Files id the first image carries.</summary>
    public const string FileId = "0193f7a2-5c1e-7b3d-9e4f-1a2b3c4d5e6f";

    private static readonly DateTimeOffsetToBinaryConverter Instants = new();
    private static readonly Lazy<Task<Rows>> Journal = new(ReadJournalAsync);

    /// <summary>Gets the journal a store from a live environment holds: the thirty published stories the seed made with
    /// their topics, settings and images, plus a story scheduled with an offset and saved twice, an image already in
    /// WoW2 Files and an edit receipt.</summary>
    public static Task<Rows> JournalAsync() => Journal.Value;

    /// <summary>Writes the journal to a new store at <paramref name="path"/>.</summary>
    public static async Task<Rows> WriteAsync(string path)
    {
        var rows = await JournalAsync();
        Write(path, rows);
        return rows;
    }

    /// <summary>Writes <paramref name="rows"/> to a new store at <paramref name="path"/>.</summary>
    public static void Write(string path, Rows rows)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var store = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false };
        using var connection = new SqliteConnection(store.ToString());
        connection.Open();
        using (var schema = connection.CreateCommand())
        {
            schema.CommandText = Schema;
            schema.ExecuteNonQuery();
        }

        using var transaction = connection.BeginTransaction();
        foreach (var topic in rows.Topics)
            Insert(transaction, "topics", ("Key", topic.Key), ("SortOrder", topic.SortOrder), ("Json", topic.Json));
        foreach (var settings in rows.Settings)
            Insert(transaction, "site_settings",
                ("Id", settings.Id), ("Revision", settings.Revision), ("Json", settings.Json));
        foreach (var article in rows.Articles)
            Insert(transaction, "articles", ("Id", Text(article.Id)), ("Slug", article.Slug), ("Topic", article.Topic),
                ("Status", article.Status), ("PublishAt", Binary(article.PublishAt)),
                ("PublishedAt", Binary(article.PublishedAt)), ("Revision", article.Revision),
                ("CreatedAt", Binary(article.CreatedAt)), ("UpdatedAt", Binary(article.UpdatedAt)),
                ("Json", article.Json));
        foreach (var revision in rows.Revisions)
            Insert(transaction, "article_revisions", ("Id", Text(revision.Id)), ("ArticleId", Text(revision.ArticleId)),
                ("Revision", revision.Revision), ("SavedAt", Binary(revision.SavedAt)), ("Summary", revision.Summary),
                ("Json", revision.Json));
        foreach (var media in rows.Media)
            Insert(transaction, "media_blobs", ("Sha256", media.Sha256), ("FileName", media.FileName),
                ("ContentType", media.ContentType), ("ByteLength", media.ByteLength), ("BlobPath", media.BlobPath),
                ("PreviewPath", media.PreviewPath), ("Width", media.Width), ("Height", media.Height),
                ("StoredAt", Binary(media.StoredAt)), ("FileId", media.FileId));
        foreach (var edit in rows.Edits)
            Insert(transaction, "media_edits",
                ("Id", edit.Id), ("SourceSha256", edit.SourceSha256), ("Sha256", edit.Sha256),
                ("RecipeJson", edit.RecipeJson), ("OrganizationId", edit.OrganizationId), ("Actor", edit.Actor),
                ("Model", edit.Model), ("CreatedAt", Binary(edit.CreatedAt)));
        transaction.Commit();
    }

    /// <summary>Reads what a seeded host holds, rewrites each document as the SQLite-era release wrote it, and adds the
    /// rows the seed never makes.</summary>
    private static async Task<Rows> ReadJournalAsync()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var db = harness.Get<OcharoBlogDbContext>();
        var articles = (await db.Articles.AsNoTracking().ToListAsync()).Select(Rewritten).ToList();
        var revisions = (await db.ArticleRevisions.AsNoTracking().ToListAsync()).Select(Rewritten).ToList();
        var topics = (await db.Topics.AsNoTracking().ToListAsync())
            .Select(topic =>
            {
                topic.Json = BlogJson.Write(BlogJson.Read<TopicDocument>(topic.Json));
                return topic;
            }).ToList();
        var settings = (await db.Settings.AsNoTracking().ToListAsync())
            .Select(row => { row.Json = BlogJson.Write(BlogJson.Read<SiteSettings>(row.Json)); return row; }).ToList();
        var media = await db.MediaBlobs.AsNoTracking().OrderBy(row => row.Sha256).ToListAsync();

        // A story an editor scheduled from Tashkent and saved twice: its instants carry +05:00 into the store.
        var source = BlogJson.Read<ArticleDocument>(articles[0].Json);
        var scheduled = source with
        {
            Slug = "scheduled-from-tashkent", Title = "Scheduled from Tashkent", Status = ArticleStatus.Scheduled,
            PublishAt = ScheduledAt, PublishedAt = null,
        };
        var id = Guid.CreateVersion7();
        var saved = ScheduledAt.AddDays(-2);
        var draft = scheduled with { Status = ArticleStatus.Draft, PublishAt = null };
        var first = ArticleService.ToRecord(id, draft, 1, saved, saved);
        var current = ArticleService.ToRecord(id, scheduled, 2, saved, saved.AddHours(1));
        articles.Add(current);
        revisions.Add(Snapshot(first, "Created"));
        revisions.Add(Snapshot(current, "Scheduled"));

        // An image already in WoW2 Files, and the receipt of an edit made from another image.
        media[0].FileId = FileId;
        var edit = new MediaEditRecord
        {
            Id = new string('e', 64), SourceSha256 = media[1].Sha256, Sha256 = media[0].Sha256,
            RecipeJson = """{"crop":null,"rotate":90,"flipHorizontal":false,"output":{"format":"png","quality":90}}""",
            OrganizationId = EditorialSignIn.Organization.ToString("D"), Actor = EditorialSignIn.Owner.Subject,
            Model = null, CreatedAt = saved.AddMinutes(5),
        };
        return new Rows(articles, revisions, topics, settings, media, [edit]);
    }

    private static ArticleRecord Rewritten(ArticleRecord record)
    {
        record.Json = BlogJson.Write(BlogJson.Read<ArticleDocument>(record.Json));
        return record;
    }

    private static ArticleRevisionRecord Rewritten(ArticleRevisionRecord record)
    {
        record.Json = BlogJson.Write(BlogJson.Read<ArticleDocument>(record.Json));
        return record;
    }

    private static ArticleRevisionRecord Snapshot(ArticleRecord record, string summary) => new()
    {
        Id = Guid.CreateVersion7(), ArticleId = record.Id, Revision = record.Revision, SavedAt = record.UpdatedAt,
        Summary = summary, Json = record.Json,
    };

    /// <summary>A Guid as EF Core's SQLite provider stores it.</summary>
    private static string Text(Guid id) => id.ToString("D").ToUpperInvariant();

    /// <summary>An instant as the SQLite-era model stored it: EF's binary form, offset included.</summary>
    private static long? Binary(DateTimeOffset? value) =>
        value is { } instant ? (long)Instants.ConvertToProvider(instant)! : null;

    private static void Insert(SqliteTransaction transaction, string table,
        params (string Column, object? Value)[] values)
    {
        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"INSERT INTO \"{table}\" ({string.Join(", ", values.Select(v => $"\"{v.Column}\""))}) "
            + $"VALUES ({string.Join(", ", values.Select((_, index) => $"$p{index}"))})";
        for (var index = 0; index < values.Length; index++)
            command.Parameters.AddWithValue($"$p{index}", values[index].Value ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    /// <summary>The store's schema as the SQLite-era release left it: EnsureCreated's tables, then the boot DDL's
    /// receipts and Files column.</summary>
    private const string Schema = """
        PRAGMA journal_mode=WAL;
        CREATE TABLE "article_revisions" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_article_revisions" PRIMARY KEY, "ArticleId" TEXT NOT NULL,
            "Revision" INTEGER NOT NULL, "SavedAt" INTEGER NOT NULL, "Summary" TEXT NOT NULL, "Json" TEXT NOT NULL);
        CREATE TABLE "articles" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_articles" PRIMARY KEY, "Slug" TEXT NOT NULL, "Topic" TEXT NOT NULL,
            "Status" TEXT NOT NULL, "PublishAt" INTEGER NULL, "PublishedAt" INTEGER NULL, "Revision" INTEGER NOT NULL,
            "CreatedAt" INTEGER NOT NULL, "UpdatedAt" INTEGER NOT NULL, "Json" TEXT NOT NULL);
        CREATE TABLE "media_blobs" (
            "Sha256" TEXT NOT NULL CONSTRAINT "PK_media_blobs" PRIMARY KEY, "FileName" TEXT NOT NULL,
            "ContentType" TEXT NOT NULL, "ByteLength" INTEGER NOT NULL, "BlobPath" TEXT NOT NULL,
            "PreviewPath" TEXT NULL, "Width" INTEGER NULL, "Height" INTEGER NULL, "StoredAt" INTEGER NOT NULL);
        CREATE TABLE "site_settings" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_site_settings" PRIMARY KEY, "Revision" INTEGER NOT NULL,
            "Json" TEXT NOT NULL);
        CREATE TABLE "topics" (
            "Key" TEXT NOT NULL CONSTRAINT "PK_topics" PRIMARY KEY, "SortOrder" INTEGER NOT NULL, "Json" TEXT NOT NULL);
        CREATE UNIQUE INDEX "IX_article_revisions_ArticleId_Revision" ON "article_revisions" ("ArticleId", "Revision");
        CREATE UNIQUE INDEX "IX_articles_Slug" ON "articles" ("Slug");
        CREATE INDEX "IX_articles_Status_PublishAt" ON "articles" ("Status", "PublishAt");
        CREATE TABLE media_edits (
            Id TEXT NOT NULL PRIMARY KEY, SourceSha256 TEXT NOT NULL, Sha256 TEXT NOT NULL,
            RecipeJson TEXT NOT NULL, OrganizationId TEXT NOT NULL, Actor TEXT NOT NULL,
            Model TEXT NULL, CreatedAt INTEGER NOT NULL,
            FOREIGN KEY (SourceSha256) REFERENCES media_blobs(Sha256),
            FOREIGN KEY (Sha256) REFERENCES media_blobs(Sha256));
        CREATE INDEX media_edits_target ON media_edits(Sha256, CreatedAt DESC);
        CREATE TRIGGER media_edits_no_update BEFORE UPDATE ON media_edits
        BEGIN SELECT RAISE(ABORT, 'Image receipts are immutable'); END;
        CREATE TRIGGER media_edits_no_delete BEFORE DELETE ON media_edits
        BEGIN SELECT RAISE(ABORT, 'Image receipts are immutable'); END;
        ALTER TABLE media_blobs ADD COLUMN FileId TEXT NULL;
        """;
}
