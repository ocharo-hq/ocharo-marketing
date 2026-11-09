using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace OcharoBlog.Persistence;

/// <summary>Initializes the SQLite blog schema and its additive pilot tables.</summary>
public static class BlogDatabaseInitialization
{
    /// <summary>Creates the SQLite schema on boot.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OcharoBlogDbContext>();
        var source = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (!string.IsNullOrEmpty(source) && source != ":memory:" && Path.GetDirectoryName(Path.GetFullPath(source)) is { } folder)
            Directory.CreateDirectory(folder);
        await db.Database.EnsureCreatedAsync();
        // Additive adoption for existing SQLite pilots; original media/story tables remain untouched.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS media_edits (
                Id TEXT NOT NULL PRIMARY KEY, SourceSha256 TEXT NOT NULL, Sha256 TEXT NOT NULL,
                RecipeJson TEXT NOT NULL, OrganizationId TEXT NOT NULL, Actor TEXT NOT NULL,
                Model TEXT NULL, CreatedAt INTEGER NOT NULL,
                FOREIGN KEY (SourceSha256) REFERENCES media_blobs(Sha256),
                FOREIGN KEY (Sha256) REFERENCES media_blobs(Sha256)
            );
            CREATE INDEX IF NOT EXISTS media_edits_target ON media_edits(Sha256, CreatedAt DESC);
            CREATE TRIGGER IF NOT EXISTS media_edits_no_update BEFORE UPDATE ON media_edits
            BEGIN SELECT RAISE(ABORT, 'Image receipts are immutable'); END;
            CREATE TRIGGER IF NOT EXISTS media_edits_no_delete BEFORE DELETE ON media_edits
            BEGIN SELECT RAISE(ABORT, 'Image receipts are immutable'); END;
            """);
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        // A store made before WoW2 Files lacks the file-id column; EnsureCreated never alters an existing table.
        var hasFileId = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info('media_blobs') WHERE name = 'FileId'")
            .SingleAsync();
        if (hasFileId == 0)
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE media_blobs ADD COLUMN FileId TEXT NULL;");
    }
}
