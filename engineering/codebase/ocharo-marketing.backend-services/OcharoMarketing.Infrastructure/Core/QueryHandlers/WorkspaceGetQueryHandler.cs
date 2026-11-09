using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Core.Models;
using OcharoMarketing.Application.Core.Queries;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Deliveries.Repositories;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Core.QueryHandlers;

/// <summary>Handles <see cref="WorkspaceGetQuery"/>: reads every record in turn, and explains each channel no metric is
/// cached for.</summary>
internal sealed class WorkspaceGetQueryHandler(
    IMarketingRepository repository,
    ICardRenderingService cards,
    IDeliveryRepository delivery,
    PublicationService publication)
    : IQueryHandler<WorkspaceGetQuery, AppResult<WorkspaceModel>>
{
    private static readonly string[] MetricChannels = ["instagram", "telegram", "whatsapp", "x"];

    /// <inheritdoc />
    public async ValueTask<AppResult<WorkspaceModel>> HandleAsync(WorkspaceGetQuery request, CancellationToken ct)
    {
        var brand = await repository.GetBrandAsync(ct);
        var products = await repository.GetProductsAsync(ct);
        var assets = await repository.GetAssetsAsync(ct);
        var presets = cards.GetPresets();
        var drafts = await repository.GetDraftsAsync(ct);
        var schedules = await repository.GetSchedulesAsync(ct);
        var connections = await publication.ConnectionsAsync(ct);
        var jobs = await repository.GetJobsAsync(ct);
        var operations = await delivery.GetOperationsAsync(ct);
        var settings = publication.Settings;
        var metrics = await CachedMetricsAsync(ct);
        var workspace = new WorkspaceModel(
            brand,
            products,
            assets,
            presets,
            drafts,
            schedules,
            connections,
            jobs,
            operations,
            settings,
            metrics);

        return AppResult<WorkspaceModel>.Ok(workspace);
    }

    private async Task<IReadOnlyList<MetricModel>> CachedMetricsAsync(CancellationToken ct)
    {
        var observed = await delivery.GetCachedObservationsAsync(ct);
        var metrics = observed.Select(x => new MetricModel(
            x.Provider,
            x.Name,
            x.Value,
            x.Availability,
            x.ObservedAt,
            x.Reason ?? "Cached provider observation; loading this workspace does not refresh it.",
            x.ScheduleId,
            x.ReceiptId,
            x.Scope,
            x.Aggregation)).ToList();
        foreach (var channel in MetricChannels)
        {
            if (!observed.Any(x => x.Provider == channel))
            {
                metrics.Add(new(
                    channel,
                    channel == "whatsapp" ? "Delivery" : "Engagement",
                    null,
                    channel == "telegram" ? "not_supported" : "unavailable",
                    null,
                    "No observed metric is cached. A plan or publication receipt is not engagement evidence."));
            }
        }

        return metrics;
    }
}
