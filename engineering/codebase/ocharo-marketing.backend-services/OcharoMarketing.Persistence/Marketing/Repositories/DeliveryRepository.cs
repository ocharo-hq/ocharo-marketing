using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Domain.Marketing.Entities;

namespace OcharoMarketing.Persistence.Marketing.Repositories;

/// <summary>Persists explicit delivery authorization and observations with conditional SQLite ownership.</summary>
public sealed class DeliveryRepository(OcharoMarketingDbContext db) : IDeliveryRepository
{
    public Task<DeliveryConnectionRecord?> FindConnectionAsync(string id, CancellationToken ct) => db.DeliveryConnections.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task InvalidateConnectionAsync(string id, string message, CancellationToken ct)
    {
        await db.DeliveryConnections.Where(x => x.Id == id).ExecuteUpdateAsync(update => update
            .SetProperty(x => x.Revision, x => x.Revision + 1).SetProperty(x => x.VerificationStatus, "verification-failed")
            .SetProperty(x => x.SafeMessage, message), ct);
    }

    public async Task<DeliveryIdentityModel> SaveVerifiedConnectionAsync(DeliveryIdentityModel identity, string stamp, CancellationToken ct)
    {
        var row = await db.DeliveryConnections.SingleOrDefaultAsync(x => x.Id == identity.ConnectionId, ct);
        if (row is null) { row = new() { Id = identity.ConnectionId }; db.DeliveryConnections.Add(row); }
        var saved = identity with { Revision = row.Revision + 1 };
        row.VerificationStatus = "verified"; row.SafeMessage = "Account and destination verified.";
        row.Revision = saved.Revision; row.ConfigurationStamp = stamp; row.Json = MarketingJson.Write(saved);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new MarketingException("Account verification changed concurrently. Reload the connection before verifying again.", 409); }
        catch (DbUpdateException) { throw new MarketingException("Another account verification is being saved. Reload the connection before verifying again.", 409); }
        return saved;
    }

    public async Task<ScheduleModel?> FindScheduleAsync(string id, CancellationToken ct)
    {
        var json = await db.Schedules.AsNoTracking().Where(x => x.Id == id).Select(x => x.Json).SingleOrDefaultAsync(ct);
        return json is null ? null : MarketingJson.Read<ScheduleModel>(json);
    }

    public async Task<PublicationOperationModel?> FindOperationAsync(string scheduleId, CancellationToken ct)
    {
        var json = await db.PublicationOperations.AsNoTracking().Where(x => x.ScheduleId == scheduleId).Select(x => x.Json).SingleOrDefaultAsync(ct);
        return json is null ? null : MarketingJson.Read<PublicationOperationModel>(json);
    }

    public async Task<IReadOnlyList<PublicationOperationModel>> GetOperationsAsync(CancellationToken ct) =>
        (await db.PublicationOperations.AsNoTracking().OrderByDescending(x => x.DueAt).Select(x => x.Json).ToListAsync(ct)).Select(MarketingJson.Read<PublicationOperationModel>).ToArray();

    public async Task<PublicationOperationModel> QueueAsync(PublicationOperationModel operation, string stamp, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var touched = await db.Schedules.Where(x => x.Id == operation.ScheduleId && x.Status == "planned")
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "planned"), ct);
        var existing = await FindOperationAsync(operation.ScheduleId, ct);
        if (existing is not null)
        {
            if (existing.Id == operation.Id && existing.ConnectionId == operation.ConnectionId
                && existing.ConnectionRevision == operation.ConnectionRevision && existing.Snapshot.Draft.Revision == operation.Snapshot.Draft.Revision)
                return existing;
            throw new MarketingException("This plan already has a publication operation. Create a new plan for fresh authorization.", 409);
        }
        if (touched != 1 || await db.PublicationOperations.AnyAsync(x => x.Id == operation.Id, ct))
            throw new MarketingException("This plan or operation identifier is no longer available for queueing.", 409);
        var revision = operation.Snapshot.Draft.Revision;
        if (await db.Drafts.Where(x => x.Id == operation.Snapshot.Draft.Id && x.Revision == revision && x.ApprovedRevision == revision)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ApprovedRevision, (int?)revision), ct) != 1)
            throw new MarketingException("Review and approve the current draft, then create a new plan.", 409);
        if (!await db.DeliveryConnections.AnyAsync(x => x.Id == operation.ConnectionId && x.Revision == operation.ConnectionRevision && x.VerificationStatus == "verified" && x.ConfigurationStamp == stamp, ct))
            throw new MarketingException("The verified connection changed before queue authorization.", 409);
        db.PublicationOperations.Add(new() { Id = operation.Id, ScheduleId = operation.ScheduleId, Provider = operation.Provider,
            ConnectionId = operation.ConnectionId, ConnectionRevision = operation.ConnectionRevision, ConfigurationStamp = stamp,
            Status = "queued", DueAt = operation.DueAt.ToUnixTimeMilliseconds(), Json = MarketingJson.Write(operation) });
        await MirrorScheduleAsync(operation, ct);
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return operation;
    }

    public async Task RecoverExpiredAsync(DateTimeOffset now, CancellationToken ct)
    {
        var timestamp = now.ToUnixTimeMilliseconds();
        var abandoned = await db.PublicationOperations.AsNoTracking().Where(x => x.Status == "running" && x.LeaseExpiresAt <= timestamp).ToListAsync(ct);
        foreach (var row in abandoned)
        {
            var status = row.DispatchStartedAt is null ? "queued" : "unknown";
            var model = MarketingJson.Read<PublicationOperationModel>(row.Json) with { Status = status,
                Phase = status == "unknown" ? "reconciliation_required" : "authorized", UpdatedAt = now,
                Message = status == "unknown" ? "Delivery was interrupted after dispatch began. Inspect provider history; no automatic retry will occur." : "Recovered an unstarted operation." };
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var changed = await db.PublicationOperations.Where(x => x.Id == row.Id && x.Status == "running" && x.ClaimToken == row.ClaimToken && x.LeaseExpiresAt <= timestamp)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, status).SetProperty(x => x.Json, MarketingJson.Write(model))
                    .SetProperty(x => x.ClaimToken, (string?)null).SetProperty(x => x.LeaseExpiresAt, (long?)null), ct);
            if (changed == 1) await MirrorScheduleAsync(model, ct);
            await transaction.CommitAsync(ct);
        }
    }

    public async Task<PublicationClaim?> ClaimDueAsync(DateTimeOffset now, TimeSpan lease, CancellationToken ct)
    {
        await RecoverExpiredAsync(now, ct);
        var timestamp = now.ToUnixTimeMilliseconds();
        var candidate = await db.PublicationOperations.AsNoTracking().Where(x => x.Status == "queued" && x.DueAt <= timestamp)
            .OrderBy(x => x.DueAt).FirstOrDefaultAsync(ct);
        if (candidate is null) return null;
        var token = Guid.NewGuid().ToString("N");
        var running = MarketingJson.Read<PublicationOperationModel>(candidate.Json) with { Status = "running", Phase = "claimed", UpdatedAt = now };
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var changed = await db.PublicationOperations.Where(x => x.Id == candidate.Id && x.Status == "queued")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "running").SetProperty(x => x.ClaimToken, token)
                    .SetProperty(x => x.LeaseExpiresAt, now.Add(lease).ToUnixTimeMilliseconds()).SetProperty(x => x.Attempts, x => x.Attempts + 1)
                    .SetProperty(x => x.Json, MarketingJson.Write(running)), ct);
            if (changed != 1) return null;
            await MirrorScheduleAsync(running, ct); await transaction.CommitAsync(ct);
        }
        return new(candidate.Id, token, running);
    }

    public async Task<bool> ConnectionMatchesAsync(PublicationClaim claim, string stamp, CancellationToken ct) =>
        await db.PublicationOperations.AnyAsync(x => x.Id == claim.Id && x.ConfigurationStamp == stamp, ct)
        && await db.DeliveryConnections.AnyAsync(x => x.Id == claim.Operation.ConnectionId && x.Revision == claim.Operation.ConnectionRevision && x.VerificationStatus == "verified" && x.ConfigurationStamp == stamp, ct);

    public Task MarkDispatchAsync(PublicationClaim claim, DateTimeOffset now, CancellationToken ct) =>
        UpdateClaimAsync(claim, model => model with { Phase = "dispatch_started", DispatchStartedAt = model.DispatchStartedAt ?? now, UpdatedAt = now }, now, false, ct);

    public Task CheckpointAsync(PublicationClaim claim, ProviderReceipt receipt, DateTimeOffset now, CancellationToken ct) =>
        UpdateClaimAsync(claim, model => model with { Phase = receipt.Stage, Receipt = receipt, UpdatedAt = now }, now, false, ct);

    public Task FinishAsync(PublicationClaim claim, string status, string message, ProviderReceipt? receipt, DateTimeOffset now, CancellationToken ct) =>
        UpdateClaimAsync(claim, model => model with { Status = status, Phase = status, Message = message, Receipt = receipt ?? model.Receipt, UpdatedAt = now }, now, true, ct);

    private async Task UpdateClaimAsync(PublicationClaim claim, Func<PublicationOperationModel, PublicationOperationModel> transform, DateTimeOffset now, bool release, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var row = await db.PublicationOperations.AsNoTracking().SingleAsync(x => x.Id == claim.Id, ct);
        var model = transform(MarketingJson.Read<PublicationOperationModel>(row.Json));
        var changed = await db.PublicationOperations.Where(x => x.Id == claim.Id && x.Status == "running" && x.ClaimToken == claim.ClaimToken && x.LeaseExpiresAt > now.ToUnixTimeMilliseconds())
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, model.Status).SetProperty(x => x.Json, MarketingJson.Write(model))
                .SetProperty(x => x.DispatchStartedAt, model.DispatchStartedAt == null ? (long?)null : model.DispatchStartedAt.Value.ToUnixTimeMilliseconds())
                .SetProperty(x => x.ClaimToken, release ? null : claim.ClaimToken)
                .SetProperty(x => x.LeaseExpiresAt, release ? (long?)null : now.AddMinutes(10).ToUnixTimeMilliseconds()), ct);
        if (changed != 1) throw new MarketingException("The publication worker no longer owns this operation.", 409);
        await MirrorScheduleAsync(model, ct); await transaction.CommitAsync(ct);
    }

    public async Task HoldQueuedAsync(string? provider, string message, DateTimeOffset now, CancellationToken ct)
    {
        var rows = await db.PublicationOperations.AsNoTracking().Where(x => x.Status == "queued" && (provider == null || x.Provider == provider)).ToListAsync(ct);
        foreach (var row in rows)
        {
            var held = MarketingJson.Read<PublicationOperationModel>(row.Json) with { Status = "held", Phase = "authorization_revoked", Message = message, UpdatedAt = now };
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (await db.PublicationOperations.Where(x => x.Id == row.Id && x.Status == "queued")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "held").SetProperty(x => x.Json, MarketingJson.Write(held)), ct) == 1)
                await MirrorScheduleAsync(held, ct);
            await transaction.CommitAsync(ct);
        }
    }

    public async Task<ScheduleModel> CancelAsync(string scheduleId, DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var row = await db.PublicationOperations.AsNoTracking().SingleOrDefaultAsync(x => x.ScheduleId == scheduleId, ct);
        if (row is null) { var plan = await new MarketingRepository(db).CancelScheduleAsync(scheduleId, ct); await transaction.CommitAsync(ct); return plan; }
        var cancelled = MarketingJson.Read<PublicationOperationModel>(row.Json) with { Status = "cancelled", Phase = "cancelled", Message = "Cancelled before dispatch. No unsend was attempted.", UpdatedAt = now };
        var changed = await db.PublicationOperations.Where(x => x.Id == row.Id && (x.Status == "queued" || x.Status == "held" || x.Status == "running" && x.DispatchStartedAt == null))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "cancelled").SetProperty(x => x.Json, MarketingJson.Write(cancelled))
                .SetProperty(x => x.ClaimToken, (string?)null).SetProperty(x => x.LeaseExpiresAt, (long?)null), ct);
        if (changed != 1) throw new MarketingException("Dispatch has started or this operation is terminal. Cancellation cannot unsend it.", 409);
        await MirrorScheduleAsync(cancelled, ct); await transaction.CommitAsync(ct);
        return (await FindScheduleAsync(scheduleId, ct))!;
    }

    private async Task MirrorScheduleAsync(PublicationOperationModel operation, CancellationToken ct)
    {
        var schedule = await FindScheduleAsync(operation.ScheduleId, ct) ?? throw new MarketingException("Plan not found.", 404);
        var model = schedule with { Status = operation.Status == "held" ? "planned" : operation.Status, Message = operation.Message,
            ExternalId = operation.Status == "published" ? operation.Receipt?.ExternalId : schedule.ExternalId,
            DraftTitle = operation.Snapshot.Draft.Title };
        await db.Schedules.Where(x => x.Id == operation.ScheduleId).ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, model.Status).SetProperty(x => x.Json, MarketingJson.Write(model)), ct);
    }

    public async Task<PublicationOperationModel> ApplyReconciliationAsync(string scheduleId, string expectedReceiptId, string connectionId,
        int connectionRevision, string stamp, ProviderReceipt? verifiedReceipt, string message, DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.DeliveryConnections.Where(x => x.Id == connectionId && x.Revision == connectionRevision && x.ConfigurationStamp == stamp && x.VerificationStatus == "verified")
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Revision, connectionRevision), ct) != 1)
            throw new MarketingException("The verified account changed during receipt reconciliation.", 409);
        var row = await db.PublicationOperations.AsNoTracking().SingleAsync(x => x.ScheduleId == scheduleId, ct);
        var previous = MarketingJson.Read<PublicationOperationModel>(row.Json);
        if (previous.Status == "published" && previous.Receipt?.ExternalId == expectedReceiptId) return previous;
        if (!previous.CanReconcile || previous.Receipt?.ExternalId != expectedReceiptId || previous.ConnectionId != connectionId)
            throw new MarketingException("This operation no longer has the selected candidate receipt.", 409);
        var updated = previous with { Status = verifiedReceipt is null ? "unknown" : "published", Phase = verifiedReceipt is null ? "reconciliation_required" : "reconciled",
            Receipt = verifiedReceipt ?? previous.Receipt, Message = message, UpdatedAt = now };
        if (await db.PublicationOperations.Where(x => x.Id == row.Id && x.Status == "unknown" && x.Json == row.Json)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, updated.Status).SetProperty(x => x.Json, MarketingJson.Write(updated)), ct) != 1)
            throw new MarketingException("Receipt state changed during reconciliation. Reload the operation.", 409);
        await MirrorScheduleAsync(updated, ct); await transaction.CommitAsync(ct);
        return updated;
    }

    public async Task<MetricCacheModel> GetMetricsAsync(string scheduleId, CancellationToken ct)
    {
        var rows = await db.MetricObservations.AsNoTracking().Where(x => x.ScheduleId == scheduleId).OrderByDescending(x => x.RecordedAt).Select(x => x.Json).ToListAsync(ct);
        var selected = rows.Select(MarketingJson.Read<MetricObservationModel>).GroupBy(x => x.Name)
            .Select(group => group.FirstOrDefault(x => x.Value is not null) ?? group.First()).ToArray();
        var attemptJson = await db.MetricReadAttempts.AsNoTracking().Where(x => x.ScheduleId == scheduleId && x.Purpose == "metrics").OrderByDescending(x => x.AttemptedAt).Select(x => x.Json).FirstOrDefaultAsync(ct);
        return new(scheduleId, selected, attemptJson is null ? null : MarketingJson.Read<MetricAttemptModel>(attemptJson))
        { History = rows.Take(250).Select(MarketingJson.Read<MetricObservationModel>).ToArray(), HistoryTruncated = rows.Count > 250 };
    }

    public async Task<IReadOnlyList<MetricObservationModel>> GetCachedObservationsAsync(CancellationToken ct)
    {
        var ids = await db.MetricObservations.AsNoTracking().Select(x => x.ScheduleId).Distinct().ToArrayAsync(ct);
        var observations = new List<MetricObservationModel>();
        foreach (var id in ids) observations.AddRange((await GetMetricsAsync(id, ct)).Observations);
        return observations;
    }

    public async Task<string> ReserveMetricReadAsync(string scheduleId, string connectionId, int revision, DateTimeOffset now, int maximumPerHour, int cooldownSeconds, CancellationToken ct, string purpose = "metrics")
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await db.DeliveryConnections.Where(x => x.Id == connectionId && x.Revision == revision && x.VerificationStatus == "verified")
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Revision, revision), ct) != 1)
            throw new MarketingException("Reverify the original account before reading metrics.", 409);
        if (maximumPerHour <= 0 || await db.MetricReadAttempts.CountAsync(x => x.AttemptedAt >= now.AddHours(-1).ToUnixTimeMilliseconds(), ct) >= maximumPerHour)
            throw new MarketingException("The configured hourly metric-read budget is exhausted.", 429);
        if (await db.MetricReadAttempts.AnyAsync(x => x.ScheduleId == scheduleId && x.AttemptedAt >= now.AddSeconds(-Math.Max(1, cooldownSeconds)).ToUnixTimeMilliseconds(), ct))
            throw new MarketingException("A metric refresh was requested recently. Keep the cached observations until the cooldown ends.", 429);
        var id = Guid.NewGuid().ToString("N");
        db.MetricReadAttempts.Add(new() { Id = id, ScheduleId = scheduleId, Purpose = purpose, AttemptedAt = now.ToUnixTimeMilliseconds(), Json = MarketingJson.Write(new MetricAttemptModel(now, "pending", "An explicit bounded provider read was requested.")) });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return id;
    }

    public async Task CompleteMetricReadAsync(string attemptId, MetricAttemptModel attempt, IReadOnlyList<MetricObservationModel> observations, CancellationToken ct)
    {
        var row = await db.MetricReadAttempts.SingleAsync(x => x.Id == attemptId, ct);
        if (MarketingJson.Read<MetricAttemptModel>(row.Json).Status != "pending") return;
        row.Json = MarketingJson.Write(attempt);
        foreach (var observation in observations)
            db.MetricObservations.Add(new() { Id = observation.Id, ScheduleId = observation.ScheduleId, Name = observation.Name,
                RecordedAt = attempt.AttemptedAt.ToUnixTimeMilliseconds(), Json = MarketingJson.Write(observation) });
        await db.SaveChangesAsync(ct);
    }
}
