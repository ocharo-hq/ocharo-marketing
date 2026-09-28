using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Domain.Marketing.Entities;
using OcharoMarketing.Domain.Scanning;

namespace OcharoMarketing.Persistence;

/// <summary>Stores local marketing revisions, original assets and durable work in SQLite.</summary>
public sealed class OcharoMarketingDbContext(DbContextOptions<OcharoMarketingDbContext> options) : DbContext(options)
{
    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<MetadataRecord> Metadata => Set<MetadataRecord>();
    public DbSet<AssetRecord> Assets => Set<AssetRecord>();
    public DbSet<DraftRecord> Drafts => Set<DraftRecord>();
    public DbSet<DraftRevisionRecord> DraftRevisions => Set<DraftRevisionRecord>();
    public DbSet<ScheduleRecord> Schedules => Set<ScheduleRecord>();
    public DbSet<GenerationJobRecord> GenerationJobs => Set<GenerationJobRecord>();

    public DbSet<DeliveryConnectionRecord> DeliveryConnections => Set<DeliveryConnectionRecord>();
    public DbSet<PublicationOperationRecord> PublicationOperations => Set<PublicationOperationRecord>();
    public DbSet<MetricObservationRecord> MetricObservations => Set<MetricObservationRecord>();
    public DbSet<MetricReadAttemptRecord> MetricReadAttempts => Set<MetricReadAttemptRecord>();

    public DbSet<SourceAnalysisRecord> SourceAnalyses => Set<SourceAnalysisRecord>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<DeliveryConnectionRecord>().HasKey(x => x.Id);
        model.Entity<PublicationOperationRecord>().HasKey(x => x.Id);
        model.Entity<MetricObservationRecord>().HasKey(x => x.Id);
        model.Entity<MetricReadAttemptRecord>().HasKey(x => x.Id);
        model.Entity<PublicationOperationRecord>().HasIndex(x => x.ScheduleId).IsUnique();
        model.Entity<PublicationOperationRecord>().HasIndex(x => new { x.Status, x.DueAt });
        model.Entity<MetricObservationRecord>().HasIndex(x => new { x.ScheduleId, x.RecordedAt });
        model.Entity<MetricReadAttemptRecord>().HasIndex(x => x.AttemptedAt);
        model.Entity<SourceAnalysisRecord>().HasKey(x => x.Id);
        model.Entity<SourceAnalysisRecord>().Property(x => x.Revision).IsConcurrencyToken();
        model.Entity<SourceAnalysisRecord>().HasIndex(x => x.Status);
        model.Entity<DeliveryConnectionRecord>().Property(x => x.Revision).IsConcurrencyToken();
        model.Entity<ProductRecord>().HasKey(x => x.Id);
        model.Entity<MetadataRecord>().HasKey(x => x.Id);
        model.Entity<AssetRecord>().HasKey(x => x.Id);
        model.Entity<DraftRecord>().HasKey(x => x.Id);
        model.Entity<DraftRecord>().Property(x => x.Revision).IsConcurrencyToken();
        model.Entity<DraftRevisionRecord>().HasKey(x => x.Id);
        model.Entity<DraftRevisionRecord>().HasIndex(x => new { x.DraftId, x.Revision }).IsUnique();
        model.Entity<ScheduleRecord>().HasKey(x => x.Id);
        model.Entity<ScheduleRecord>().HasIndex(x => new { x.Status, x.ScheduledAt });
        model.Entity<GenerationJobRecord>().HasKey(x => x.Id);
        model.Entity<GenerationJobRecord>().HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
