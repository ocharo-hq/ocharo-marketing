using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Persistence;

/// <summary>EF Core context for the blog store (SQLite): document rows plus indexed scalars.</summary>
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
        // SQLite cannot ORDER BY a TEXT-stored DateTimeOffset — store it as a sortable binary long instead.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleRecord>(e =>
        {
            e.ToTable("articles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.Status, x.PublishAt });
            e.Property(x => x.Json).IsRequired();
            e.Property(x => x.Revision).IsConcurrencyToken();
        });
        modelBuilder.Entity<ArticleRevisionRecord>(e =>
        {
            e.ToTable("article_revisions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ArticleId, x.Revision }).IsUnique();
        });
        modelBuilder.Entity<TopicRecord>(e =>
        {
            e.ToTable("topics");
            e.HasKey(x => x.Key);
        });
        modelBuilder.Entity<SiteSettingsRecord>(e =>
        {
            e.ToTable("site_settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Revision).IsConcurrencyToken();
        });
        modelBuilder.Entity<MediaEditRecord>(e =>
        {
            e.ToTable("media_edits"); e.HasKey(x => x.Id);
            e.HasOne<MediaBlobRecord>().WithMany().HasForeignKey(x => x.SourceSha256).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MediaBlobRecord>().WithMany().HasForeignKey(x => x.Sha256).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<MediaBlobRecord>(e =>
        {
            e.ToTable("media_blobs");
            e.HasKey(x => x.Sha256);
        });
    }
}
