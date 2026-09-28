using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Api.Marketing.Controllers;

/// <summary>Exposes explicit disabled-by-default delivery commands and cached evidence.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class DeliveryController(IDeliveryRepository repository, PublicationService publication, MetricObservationService metrics, ReceiptReconciliationService reconciliation) : ControllerBase
{
    [HttpPost("connections/{id}/verify")]
    public async Task<IActionResult> Verify(string id, CancellationToken ct) => Ok(await publication.VerifyAsync(id, ct));

    [HttpPost("schedules/{id}/queue")]
    public async Task<IActionResult> Queue(string id, QueuePublicationInput input, CancellationToken ct) => Ok(await publication.QueueAsync(id, input, ct));

    [HttpGet("schedules/{id}/publication")]
    public async Task<IActionResult> Publication(string id, CancellationToken ct) => Ok(await repository.FindOperationAsync(id, ct));

    [HttpPost("schedules/{id}/reconcile")]
    public async Task<IActionResult> Reconcile(string id, CancellationToken ct)
        => Ok(await reconciliation.ReconcileAsync(id, ct));

    [HttpGet("schedules/{id}/metrics")]
    public async Task<IActionResult> Metrics(string id, CancellationToken ct) => Ok(await repository.GetMetricsAsync(id, ct));

    [HttpPost("schedules/{id}/metrics/refresh")]
    public async Task<IActionResult> Refresh(string id, CancellationToken ct) => Ok(await metrics.RefreshAsync(id, ct));
}
