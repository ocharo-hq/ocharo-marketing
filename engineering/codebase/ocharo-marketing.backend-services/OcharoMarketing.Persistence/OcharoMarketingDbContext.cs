using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OcharoMarketing.Domain.Assets.Entities;
using OcharoMarketing.Domain.Core.Entities;
using OcharoMarketing.Domain.Deliveries.Entities;
using OcharoMarketing.Domain.Generations.Entities;
using OcharoMarketing.Domain.Posts.Entities;
using OcharoMarketing.Domain.Products.Entities;
using OcharoMarketing.Domain.Scanning;

namespace OcharoMarketing.Persistence;

/// <summary>Maps Marketing's revisions, assets and durable work onto the SQL-owned <c>marketing</c> schema.</summary>
/// <remarks>
///   - <c>Migrations/NNN-name/Apply.sql</c> owns the schema; EF never creates or alters it
///   - the host adds snake_case naming, so <c>ExportFiles.DraftId</c> reads <c>export_files.draft_id</c>
///   - documents are <c>jsonb</c>, and the entities' Unix milliseconds are UTC <c>timestamptz</c> instants
///   - a job's requester is a <c>uuid</c> keyed into Identity's accounts and organizations
/// </remarks>
public sealed class OcharoMarketingDbContext(DbContextOptions<OcharoMarketingDbContext> options) : DbContext(options)
{
    private const string Document = "jsonb";
    private const string Instant = "timestamp with time zone";

    /// <summary>Converts the entities' Unix milliseconds to the instants the columns hold, and back.</summary>
    private static readonly ValueConverter<long, DateTimeOffset> UnixMilliseconds = new(
        milliseconds => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds),
        instant => instant.ToUnixTimeMilliseconds());

    /// <summary>Converts the entities' Identity ids, kept as strings, to the <c>uuid</c> keys the columns
    /// hold.</summary>
    private static readonly ValueConverter IdentityId = new ValueConverter<string, Guid>(
        id => Guid.Parse(id), id => id.ToString("D"));

    /// <summary>Writes an instant at offset zero, the only offset a <c>timestamptz</c> parameter takes.</summary>
    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> Utc = new(
        instant => instant.ToUniversalTime(), instant => instant);

    public DbSet<ProductRecord> Products => Set<ProductRecord>();
    public DbSet<MetadataRecord> Metadata => Set<MetadataRecord>();
    public DbSet<AssetRecord> Assets => Set<AssetRecord>();
    public DbSet<ExportFileRecord> ExportFiles => Set<ExportFileRecord>();
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
        model.HasDefaultSchema(MarketingSchema.Name);

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
        model.Entity<ExportFileRecord>().HasKey(x => new { x.DraftId, x.Revision });
        model.Entity<DraftRecord>().HasKey(x => x.Id);
        model.Entity<DraftRecord>().Property(x => x.Revision).IsConcurrencyToken();
        model.Entity<DraftRevisionRecord>().HasKey(x => x.Id);
        model.Entity<DraftRevisionRecord>().HasIndex(x => new { x.DraftId, x.Revision }).IsUnique();
        model.Entity<ScheduleRecord>().HasKey(x => x.Id);
        model.Entity<ScheduleRecord>().HasIndex(x => new { x.Status, x.ScheduledAt });
        model.Entity<GenerationJobRecord>().HasKey(x => x.Id);
        model.Entity<GenerationJobRecord>().HasIndex(x => new { x.Status, x.CreatedAt });

        MapDocuments(model);
        MapInstants(model);
        model.Entity<GenerationJobRecord>().Property(x => x.RequestedBySubject).HasConversion(IdentityId)
            .HasColumnType("uuid");
        model.Entity<GenerationJobRecord>().Property(x => x.RequestedInOrganization).HasConversion(IdentityId)
            .HasColumnType("uuid");
    }

    /// <summary>Maps every JSON document column onto <c>jsonb</c>.</summary>
    private static void MapDocuments(ModelBuilder model)
    {
        model.Entity<ProductRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<MetadataRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<AssetRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<DraftRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<DraftRevisionRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<ScheduleRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<GenerationJobRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<GenerationJobRecord>().Property(x => x.PayloadJson).HasColumnType(Document);
        model.Entity<GenerationJobRecord>().Property(x => x.CheckpointJson).HasColumnType(Document);
        model.Entity<DeliveryConnectionRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<PublicationOperationRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<MetricObservationRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<MetricReadAttemptRecord>().Property(x => x.Json).HasColumnType(Document);
        model.Entity<SourceAnalysisRecord>().Property(x => x.SourcesJson).HasColumnType(Document);
    }

    /// <summary>Maps every instant onto a UTC <c>timestamptz</c>: the entities' Unix milliseconds through a
    /// converter.</summary>
    private static void MapInstants(ModelBuilder model)
    {
        model.Entity<ScheduleRecord>().Property(x => x.ScheduledAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<GenerationJobRecord>().Property(x => x.CreatedAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<GenerationJobRecord>().Property(x => x.LeaseExpiresAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<PublicationOperationRecord>().Property(x => x.DueAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<PublicationOperationRecord>().Property(x => x.LeaseExpiresAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<PublicationOperationRecord>().Property(x => x.DispatchStartedAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<MetricObservationRecord>().Property(x => x.RecordedAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<MetricReadAttemptRecord>().Property(x => x.AttemptedAt).HasConversion(UnixMilliseconds)
            .HasColumnType(Instant);
        model.Entity<SourceAnalysisRecord>().Property(x => x.CreatedAt).HasConversion(Utc).HasColumnType(Instant);
        model.Entity<SourceAnalysisRecord>().Property(x => x.UpdatedAt).HasConversion(Utc).HasColumnType(Instant);
        model.Entity<SourceAnalysisRecord>().Property(x => x.ReviewedAt).HasConversion(Utc).HasColumnType(Instant);
    }
}
