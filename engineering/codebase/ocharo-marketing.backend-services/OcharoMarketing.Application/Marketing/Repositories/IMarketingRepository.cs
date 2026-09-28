using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Domain.Marketing.Entities;

namespace OcharoMarketing.Application.Marketing.Repositories;

/// <summary>Defines durable product, revision, asset, schedule and work access.</summary>
public interface IMarketingRepository
{
    Task<BrandModel> GetBrandAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductModel>> GetProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetModel>> GetAssetsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DraftModel>> GetDraftsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ScheduleModel>> GetSchedulesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<GenerationJobModel>> GetJobsAsync(CancellationToken cancellationToken);
    Task<string?> FindJobPayloadAsync(string id, CancellationToken cancellationToken);
    Task<ProductModel?> FindProductAsync(string id, CancellationToken cancellationToken);
    Task<AssetRecord?> FindAssetAsync(string id, CancellationToken cancellationToken);
    Task<DraftModel?> FindDraftAsync(string id, CancellationToken cancellationToken);
    Task<DraftModel?> FindRevisionAsync(string id, int revision, CancellationToken cancellationToken);
    Task<DraftModel> SaveDraftAsync(DraftModel draft, int? expectedRevision, CancellationToken cancellationToken);
    Task<DraftModel> ApproveDraftAsync(string id, int expectedRevision, CancellationToken cancellationToken);
    Task<ScheduleModel> ScheduleAsync(ScheduleModel schedule, CancellationToken cancellationToken);
    Task<ScheduleModel> CancelScheduleAsync(string id, CancellationToken cancellationToken);
    Task SaveAssetAsync(AssetModel asset, string blobPath, CancellationToken cancellationToken);
    Task EnqueueAsync(GenerationJobModel job, string payloadJson, CancellationToken cancellationToken);
    Task<JobClaim?> ClaimAsync(DateTimeOffset now, TimeSpan lease, CancellationToken cancellationToken);
    Task CheckpointAsync(JobClaim claim, string checkpointJson, CancellationToken cancellationToken);
    Task CompleteAsync(JobClaim claim, string[] outputIds, CancellationToken cancellationToken);
    Task FailAsync(JobClaim claim, string message, bool uncertain, CancellationToken cancellationToken);
    Task SeedAsync(BrandModel brand, IReadOnlyList<ProductModel> products, CancellationToken cancellationToken);
    /// <summary>Inserts or replaces a product imported from Ocharo Catalogue; returns true when it was new.</summary>
    Task<bool> UpsertProductAsync(ProductModel product, CancellationToken cancellationToken);
    /// <summary>Reads a JSON workspace setting (automation slots, announced stories), or null.</summary>
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken);
    /// <summary>Writes a JSON workspace setting.</summary>
    Task SaveSettingAsync(string key, string json, CancellationToken cancellationToken);
}
