using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Core.Models;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Application.Products.Models;
using OcharoMarketing.Domain.Assets.Entities;

namespace OcharoMarketing.Application.Core.Repositories;

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
    /// <summary>Saves a new asset: its local blob, or its WoW2 Files file with an empty blob path.</summary>
    Task SaveAssetAsync(AssetModel asset, string blobPath, CancellationToken cancellationToken, string? fileId = null);
    /// <summary>Lists the assets stored before WoW2 Files, which still have no file id.</summary>
    Task<IReadOnlyList<AssetRecord>> GetAssetsWithoutFileAsync(CancellationToken cancellationToken);
    /// <summary>Records the WoW2 Files file that holds an asset's bytes.</summary>
    Task SetAssetFileAsync(string id, string fileId, CancellationToken cancellationToken);
    /// <summary>Finds the WoW2 Files file of a draft revision's export; null while none was recorded.</summary>
    Task<ExportFileRecord?> FindExportFileAsync(string draftId, int revision, CancellationToken cancellationToken);
    /// <summary>Records a draft revision's export file, and returns the one recorded first, which stays that revision's
    /// export.</summary>
    Task<ExportFileRecord> SaveExportFileAsync(ExportFileRecord file, CancellationToken cancellationToken);
    Task EnqueueAsync(
        GenerationJobModel job,
        string payloadJson,
        JobRequester? requestedBy,
        CancellationToken cancellationToken);
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
