using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Domain.Marketing.Entities;

namespace OcharoMarketing.Persistence.Marketing.Repositories;

/// <summary>Persists local marketing workflow transitions with revision and lease ownership checks.</summary>
public sealed class MarketingRepository(OcharoMarketingDbContext db) : IMarketingRepository
{
    public async Task<BrandModel> GetBrandAsync(CancellationToken ct) =>
        MarketingJson.Read<BrandModel>((await db.Metadata.AsNoTracking().SingleAsync(x => x.Id == "brand", ct)).Json);

    public async Task<IReadOnlyList<ProductModel>> GetProductsAsync(CancellationToken ct) =>
        (await db.Products.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Json).ToListAsync(ct)).Select(MarketingJson.Read<ProductModel>).ToArray();

    public async Task<IReadOnlyList<AssetModel>> GetAssetsAsync(CancellationToken ct) =>
        (await db.Assets.AsNoTracking().Select(x => x.Json).ToListAsync(ct)).Select(MarketingJson.Read<AssetModel>).ToArray();

    public async Task<IReadOnlyList<DraftModel>> GetDraftsAsync(CancellationToken ct) =>
        (await db.Drafts.AsNoTracking().Select(x => x.Json).ToListAsync(ct)).Select(MarketingJson.Read<DraftModel>).OrderByDescending(x => x.UpdatedAt).ToArray();

    public async Task<IReadOnlyList<ScheduleModel>> GetSchedulesAsync(CancellationToken ct)
    {
        var schedules = (await db.Schedules.AsNoTracking().OrderBy(x => x.ScheduledAt).Select(x => x.Json).ToListAsync(ct))
            .Select(MarketingJson.Read<ScheduleModel>).ToArray();
        for (var index = 0; index < schedules.Length; index++)
        {
            var schedule = schedules[index];
            if (schedule.DraftTitle is null)
                schedules[index] = schedule with { DraftTitle = (await FindRevisionAsync(schedule.DraftId, schedule.DraftRevision, ct))?.Title };
        }
        return schedules;
    }

    public async Task<IReadOnlyList<GenerationJobModel>> GetJobsAsync(CancellationToken ct) =>
        (await db.GenerationJobs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => x.Json).ToListAsync(ct)).Select(MarketingJson.Read<GenerationJobModel>).ToArray();

    public Task<string?> FindJobPayloadAsync(string id, CancellationToken ct) =>
        db.GenerationJobs.AsNoTracking().Where(x => x.Id == id).Select(x => x.PayloadJson).SingleOrDefaultAsync(ct);

    public async Task<ProductModel?> FindProductAsync(string id, CancellationToken ct)
    {
        var row = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : MarketingJson.Read<ProductModel>(row.Json);
    }

    public Task<AssetRecord?> FindAssetAsync(string id, CancellationToken ct) =>
        db.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<DraftModel?> FindDraftAsync(string id, CancellationToken ct)
    {
        var row = await db.Drafts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : MarketingJson.Read<DraftModel>(row.Json);
    }

    public async Task<DraftModel?> FindRevisionAsync(string id, int revision, CancellationToken ct)
    {
        var row = await db.DraftRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.DraftId == id && x.Revision == revision, ct);
        return row is null ? null : MarketingJson.Read<DraftModel>(row.Json);
    }

    public async Task<DraftModel> SaveDraftAsync(DraftModel draft, int? expectedRevision, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var json = MarketingJson.Write(draft);
        if (expectedRevision is null)
        {
            db.Drafts.Add(new DraftRecord { Id = draft.Id, Revision = draft.Revision, Json = json });
        }
        else
        {
            var changed = await db.Drafts.Where(x => x.Id == draft.Id && x.Revision == expectedRevision)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Revision, draft.Revision)
                    .SetProperty(x => x.ApprovedRevision, (int?)null).SetProperty(x => x.Json, json), ct);
            if (changed != 1) throw new MarketingException("The draft changed. Reload it before saving.", 409);
        }
        db.DraftRevisions.Add(new DraftRevisionRecord { Id = $"{draft.Id}:{draft.Revision}", DraftId = draft.Id, Revision = draft.Revision, Json = json });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return draft;
    }

    public async Task<DraftModel> ApproveDraftAsync(string id, int expectedRevision, CancellationToken ct)
    {
        var draft = await FindDraftAsync(id, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (draft.Revision != expectedRevision) throw new MarketingException("The draft changed. Review the current revision.", 409);
        var approved = draft with { ApprovedRevision = expectedRevision, Status = "approved" };
        var json = MarketingJson.Write(approved);
        var changed = await db.Drafts.Where(x => x.Id == id && x.Revision == expectedRevision)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ApprovedRevision, (int?)expectedRevision)
                .SetProperty(x => x.Json, json), ct);
        if (changed != 1) throw new MarketingException("The draft changed before approval was saved.", 409);
        return approved;
    }

    public async Task<ScheduleModel> ScheduleAsync(ScheduleModel schedule, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // A conditional write establishes a SQLite write lock before checking the approved input.
        var revision = schedule.DraftRevision;
        var touched = await db.Drafts.Where(x => x.Id == schedule.DraftId && x.Revision == revision && x.ApprovedRevision == revision)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ApprovedRevision, (int?)revision), ct);
        if (touched != 1) throw new MarketingException("Approve the current draft revision before scheduling.", 409);
        db.Schedules.Add(new ScheduleRecord { Id = schedule.Id, DraftId = schedule.DraftId, DraftRevision = revision,
            ScheduledAt = schedule.ScheduledAt.ToUnixTimeMilliseconds(), Status = schedule.Status, Json = MarketingJson.Write(schedule) });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return schedule;
    }

    public async Task<ScheduleModel> CancelScheduleAsync(string id, CancellationToken ct)
    {
        var row = await db.Schedules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new MarketingException("Schedule not found.", 404);
        if (row.Status is not ("planned" or "queued" or "cancelled"))
            throw new MarketingException("Delivery has started or finished; cancellation cannot unsend it.", 409);
        var cancelled = MarketingJson.Read<ScheduleModel>(row.Json) with { Status = "cancelled", Message = "Cancelled locally; no unsend was attempted." };
        var json = MarketingJson.Write(cancelled);
        var changed = await db.Schedules.Where(x => x.Id == id && x.Status == row.Status)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "cancelled").SetProperty(x => x.Json, json), ct);
        if (changed != 1) throw new MarketingException("The delivery state changed; reload before cancelling.", 409);
        return cancelled;
    }

    public async Task SaveAssetAsync(AssetModel asset, string blobPath, CancellationToken ct)
    {
        if (await db.Assets.AnyAsync(x => x.Id == asset.Id, ct)) return;
        db.Assets.Add(new AssetRecord { Id = asset.Id, Json = MarketingJson.Write(asset), BlobPath = blobPath });
        await db.SaveChangesAsync(ct);
    }

    public async Task EnqueueAsync(GenerationJobModel job, string payloadJson, CancellationToken ct)
    {
        db.GenerationJobs.Add(new GenerationJobRecord { Id = job.Id, Kind = job.Kind, Status = job.Status,
            CreatedAt = job.CreatedAt.ToUnixTimeMilliseconds(), Json = MarketingJson.Write(job), PayloadJson = payloadJson });
        await db.SaveChangesAsync(ct);
    }

    public async Task<JobClaim?> ClaimAsync(DateTimeOffset now, TimeSpan lease, CancellationToken ct)
    {
        var timestamp = now.ToUnixTimeMilliseconds();
        var abandoned = await db.GenerationJobs.AsNoTracking()
            .Where(x => x.Status == "running" && x.LeaseExpiresAt <= timestamp).ToListAsync(ct);
        foreach (var row in abandoned)
        {
            var deterministic = row.Kind == "card";
            var state = deterministic && row.Attempts < 3 ? "queued" : deterministic ? "failed" : "unknown";
            var model = MarketingJson.Read<GenerationJobModel>(row.Json) with { Status = state,
                Message = deterministic ? "Interrupted local rendering recovered from durable state." : "External execution was interrupted; inspect the provider before retrying." };
            var json = MarketingJson.Write(model);
            await db.GenerationJobs.Where(x => x.Id == row.Id && x.Status == "running" && x.ClaimToken == row.ClaimToken && x.LeaseExpiresAt <= timestamp)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, state).SetProperty(x => x.Json, json)
                    .SetProperty(x => x.ClaimToken, (string?)null).SetProperty(x => x.LeaseExpiresAt, (long?)null), ct);
        }

        var candidate = await db.GenerationJobs.AsNoTracking().Where(x => x.Status == "queued").OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (candidate is null) return null;
        var token = Guid.NewGuid().ToString("N");
        var expiry = now.Add(lease).ToUnixTimeMilliseconds();
        var running = MarketingJson.Read<GenerationJobModel>(candidate.Json) with { Status = "running", Message = null };
        var runningJson = MarketingJson.Write(running);
        var changed = await db.GenerationJobs.Where(x => x.Id == candidate.Id && x.Status == "queued")
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "running").SetProperty(x => x.ClaimToken, token)
                .SetProperty(x => x.LeaseExpiresAt, (long?)expiry).SetProperty(x => x.Attempts, x => x.Attempts + 1)
                .SetProperty(x => x.Json, runningJson), ct);
        return changed == 1 ? new JobClaim(candidate.Id, candidate.Kind, candidate.PayloadJson, token, candidate.Attempts + 1) : null;
    }

    public async Task CheckpointAsync(JobClaim claim, string checkpointJson, CancellationToken ct)
    {
        var changed = await db.GenerationJobs.Where(x => x.Id == claim.Id && x.Status == "running" && x.ClaimToken == claim.ClaimToken)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.CheckpointJson, checkpointJson), ct);
        if (changed != 1) throw new MarketingException("The worker lost ownership before its checkpoint was saved.", 409);
    }

    public async Task CompleteAsync(JobClaim claim, string[] outputIds, CancellationToken ct) =>
        await FinishAsync(claim, "completed", outputIds, null, ct);

    public async Task FailAsync(JobClaim claim, string message, bool uncertain, CancellationToken ct) =>
        await FinishAsync(claim, uncertain ? "unknown" : "failed", [], message, ct);

    private async Task FinishAsync(JobClaim claim, string status, string[] outputs, string? message, CancellationToken ct)
    {
        var row = await db.GenerationJobs.AsNoTracking().SingleAsync(x => x.Id == claim.Id, ct);
        var model = MarketingJson.Read<GenerationJobModel>(row.Json) with { Status = status, OutputAssetIds = outputs, Message = message };
        var json = MarketingJson.Write(model);
        var changed = await db.GenerationJobs.Where(x => x.Id == claim.Id && x.Status == "running" && x.ClaimToken == claim.ClaimToken)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, status).SetProperty(x => x.Json, json)
                .SetProperty(x => x.ClaimToken, (string?)null).SetProperty(x => x.LeaseExpiresAt, (long?)null), ct);
        if (changed != 1) throw new MarketingException("This worker no longer owns the job lease.", 409);
    }

    public async Task SeedAsync(BrandModel brand, IReadOnlyList<ProductModel> products, CancellationToken ct)
    {
        if (!await db.Metadata.AnyAsync(x => x.Id == "brand", ct))
            db.Metadata.Add(new MetadataRecord { Id = "brand", Json = MarketingJson.Write(brand) });
        foreach (var product in products)
            if (!await db.Products.AnyAsync(x => x.Id == product.Id, ct))
                db.Products.Add(new ProductRecord { Id = product.Id, Json = MarketingJson.Write(product) });
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> UpsertProductAsync(ProductModel product, CancellationToken ct)
    {
        var json = MarketingJson.Write(product);
        var changed = await db.Products.Where(x => x.Id == product.Id).ExecuteUpdateAsync(update => update.SetProperty(x => x.Json, json), ct);
        if (changed == 1) return false;
        db.Products.Add(new ProductRecord { Id = product.Id, Json = json });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        return true;
    }

    public async Task<string?> GetSettingAsync(string key, CancellationToken ct) =>
        (await db.Metadata.AsNoTracking().SingleOrDefaultAsync(x => x.Id == "setting:" + key, ct))?.Json;

    public async Task SaveSettingAsync(string key, string json, CancellationToken ct)
    {
        var id = "setting:" + key;
        var changed = await db.Metadata.Where(x => x.Id == id).ExecuteUpdateAsync(update => update.SetProperty(x => x.Json, json), ct);
        if (changed == 1) return;
        db.Metadata.Add(new MetadataRecord { Id = id, Json = json });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }
}
