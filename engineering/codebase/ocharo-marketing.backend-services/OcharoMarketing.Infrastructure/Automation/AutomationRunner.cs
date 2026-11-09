using Microsoft.Extensions.Logging;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Automation;

/// <summary>
/// One entry point for catalogue sync (+ the new-product draft rule) and unattended passes, shared by the API and the
/// host's periodic automation job (<see cref="RunScheduledAsync"/>). Unattended passes only sync, draft and plan; they
/// never queue delivery.
/// </summary>
public sealed class AutomationRunner(
    CatalogueSyncService catalogue,
    AutomationService automation,
    IMarketingRepository repository,
    TimeProvider time,
    WorkerHealthService health,
    ILogger<AutomationRunner> logger)
{
    private const string RunKey = "automation-run";

    /// <summary>The worker name the operations overview reports the unattended passes under.</summary>
    private const string Worker = "automation";

    public async Task<CatalogueSyncReport> SyncCatalogueAsync(CancellationToken ct)
    {
        var report = await catalogue.SyncAsync(ct);
        var settings = await automation.GetAsync(ct);
        if (settings.DraftNewCatalogueProducts && report.NewProductIds.Length > 0)
        {
            var drafts = await automation.CreateDraftsAsync(new BulkDraftInput(report.NewProductIds, settings.DefaultLocale, settings.DefaultPresetId,
                settings.DefaultChannels, null), ct);
            report = report with { DraftIds = drafts.Select(d => d.Id).ToArray() };
        }

        await SaveRunAsync(run => run with { LastSyncAt = time.GetUtcNow(), LastSync = report }, ct);
        return report;
    }

    public async Task<AutomationRunModel> StatusAsync(CancellationToken ct) =>
        await repository.GetSettingAsync(RunKey, ct) is { } json ? MarketingJson.Read<AutomationRunModel>(json) : new(null, null, null, 0, null, null);

    /// <summary>Runs whatever unattended work is due; returns true when something ran.</summary>
    public async Task<bool> RunDueAsync(CancellationToken ct)
    {
        var settings = await automation.GetAsync(ct);
        var run = await StatusAsync(ct);
        var now = time.GetUtcNow();
        var ran = false;
        if (settings.AutoSyncHours > 0 && (run.LastSyncAt is null || now - run.LastSyncAt >= TimeSpan.FromHours(settings.AutoSyncHours)))
        {
            ran = true;
            try { await SyncCatalogueAsync(ct); }
            catch (MarketingException exception)
            {
                // Record and retry after the interval; a stopped catalogue must not stop planning.
                await SaveRunAsync(r => r with { LastError = exception.Message, LastErrorAt = now, LastSyncAt = now }, ct);
            }
        }

        if (settings.AutoFillDays > 0 && (run.LastFillAt is null || now - run.LastFillAt >= TimeSpan.FromHours(1)))
        {
            ran = true;
            try
            {
                var fill = await automation.FillAsync(settings.AutoFillDays, ct);
                await SaveRunAsync(r => r with { LastFillAt = now, LastFillPlanned = fill.Planned.Length }, ct);
            }
            catch (MarketingException exception)
            {
                await SaveRunAsync(r => r with { LastError = exception.Message, LastErrorAt = now, LastFillAt = now }, ct);
            }
        }

        return ran;
    }

    /// <summary>Runs one unattended pass for the periodic job and records its outcome in the worker health; a
    /// failed pass still fails, so the job logs it and runs the next on schedule.</summary>
    public async Task RunScheduledAsync(CancellationToken ct)
    {
        var completed = false;
        try
        {
            if (await RunDueAsync(ct))
                logger.LogInformation("Unattended marketing automation pass completed");
            completed = true;
        }
        finally
        {
            if (completed) health.Completed(Worker);
            else if (!ct.IsCancellationRequested) health.Failed(Worker);
        }
    }

    private async Task SaveRunAsync(Func<AutomationRunModel, AutomationRunModel> change, CancellationToken ct) =>
        await repository.SaveSettingAsync(RunKey, MarketingJson.Write(change(await StatusAsync(ct))), ct);
}
