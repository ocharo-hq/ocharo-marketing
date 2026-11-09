using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Deliveries.Commands;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Deliveries.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Deliveries.Controllers;

/// <summary>Exposes explicit disabled-by-default delivery commands and cached evidence.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class DeliveryController(ISender sender) : ControllerBase
{
    [HttpPost("connections/{id}/verify")]
    public async Task<IActionResult> Verify(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ConnectionVerifyCommand { Id = id }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("schedules/{id}/cancel")]
    public async Task<IActionResult> CancelSchedule(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ScheduleCancelCommand { Id = id }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("schedules/{id}/queue")]
    public async Task<IActionResult> Queue(
        string id,
        QueuePublicationInput input,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new PublicationQueueCommand { ScheduleId = id, Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    /// <summary>Answers the plan's publication operation, or 204 while the plan was never queued.</summary>
    [HttpGet("schedules/{id}/publication")]
    public async Task<IActionResult> Publication(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new PublicationGetQuery { ScheduleId = id }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data.Operation),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("schedules/{id}/reconcile")]
    public async Task<IActionResult> Reconcile(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ReceiptReconcileCommand { ScheduleId = id }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("schedules/{id}/metrics")]
    public async Task<IActionResult> Metrics(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new MetricsGetQuery { ScheduleId = id }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("schedules/{id}/metrics/refresh")]
    public async Task<IActionResult> Refresh(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new MetricsRefreshCommand { ScheduleId = id }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
