using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Persistence.Blog;

namespace OcharoBlog.Persistence;

/// <summary>Registers the persistence layer — the EF Core SQLite context and the blog repository.</summary>
public static class DependencyInjection
{
    private const string DefaultConnection = "Data Source=App_Data/ocharo-blog.db";

    /// <summary>Adds the SQLite context; the connection string resolves lazily and relative files anchor at the content root.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OcharoBlogDbContext>((provider, options) =>
        {
            var raw = (provider.GetService<IConfiguration>() ?? configuration)["ConnectionStrings:OcharoBlog"] ?? DefaultConnection;
            var builder = new SqliteConnectionStringBuilder(raw);
            var contentRoot = provider.GetService<IHostEnvironment>()?.ContentRootPath;
            if (builder.DataSource is { Length: > 0 } file && file != ":memory:" && !Path.IsPathRooted(file) && contentRoot is not null)
                builder.DataSource = Path.Combine(contentRoot, file);
            options.UseSqlite(builder.ToString());
        });
        services.AddScoped<IBlogRepository, BlogRepository>();
        return services;
    }

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
    }
}
