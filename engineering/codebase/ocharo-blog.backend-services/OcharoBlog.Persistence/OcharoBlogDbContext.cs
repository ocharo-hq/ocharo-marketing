using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Persistence;

/// <summary>EF Core context for the blog store: a pure mapper over the SQL-owned <c>blog</c> schema, on snake_case
/// columns; document rows plus indexed scalars.</summary>
/// <remarks>
///   - the schema is owned by <c>Migrations/NNN-name/Apply.sql</c>; EF never creates or alters it
///   - every instant is written as UTC, the only offset a <c>timestamptz</c> parameter takes
/// </remarks>
public sealed class OcharoBlogDbContext(DbContextOptions<OcharoBlogDbContext> options) : DbContext(options)
{
    public DbSet<ArticleRecord> Articles => Set<ArticleRecord>();
    public DbSet<ArticleRevisionRecord> ArticleRevisions => Set<ArticleRevisionRecord>();
    public DbSet<TopicRecord> Topics => Set<TopicRecord>();
    public DbSet<SiteSettingsRecord> Settings => Set<SiteSettingsRecord>();
    public DbSet<MediaBlobRecord> MediaBlobs => Set<MediaBlobRecord>();
    public DbSet<MediaEditRecord> MediaEdits => Set<MediaEditRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Npgsql writes a DateTimeOffset to timestamptz only at offset zero; a schedule typed with another offset is
        // the same instant in UTC.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(BlogSchema.Name);
        modelBuilder.Entity<ArticleRecord>(e =>
        {
            e.ToTable("articles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Json).IsRequired().HasColumnType("jsonb");
            e.Property(x => x.Revision).IsConcurrencyToken();
        });
        modelBuilder.Entity<ArticleRevisionRecord>(e =>
        {
            e.ToTable("article_revisions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Json).HasColumnType("jsonb");
        });
        modelBuilder.Entity<TopicRecord>(e =>
        {
            e.ToTable("topics");
            e.HasKey(x => x.Key);
            e.Property(x => x.Json).HasColumnType("jsonb");
        });
        modelBuilder.Entity<SiteSettingsRecord>(e =>
        {
            e.ToTable("site_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.Property(x => x.Json).HasColumnType("jsonb");
        });
        modelBuilder.Entity<MediaEditRecord>(e =>
        {
            e.ToTable("media_edits");
            e.HasKey(x => x.Id);
            e.Property(x => x.RecipeJson).HasColumnType("jsonb");
            // Identity's organization and person ids, uuid keys into its published tables; the record keeps their text.
            e.Property(x => x.OrganizationId).HasConversion(IdentityId);
            e.Property(x => x.Actor).HasConversion(IdentityId);
            e.HasOne<MediaBlobRecord>().WithMany().HasForeignKey(x => x.SourceSha256).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MediaBlobRecord>().WithMany().HasForeignKey(x => x.Sha256).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<MediaBlobRecord>(e =>
        {
            e.ToTable("media_blobs");
            e.HasKey(x => x.Sha256);
        });
    }

    /// <summary>Maps an Identity id the domain holds as text onto its <c>uuid</c> column.</summary>
    private static readonly ValueConverter<string, Guid> IdentityId = new(
        value => Guid.Parse(value), value => value.ToString("D"));

    /// <summary>Converts an instant to its UTC form on the way in; PostgreSQL hands it back at offset zero.</summary>
    private sealed class UtcConverter() : ValueConverter<DateTimeOffset, DateTimeOffset>(
        value => value.ToUniversalTime(), value => value);
}
