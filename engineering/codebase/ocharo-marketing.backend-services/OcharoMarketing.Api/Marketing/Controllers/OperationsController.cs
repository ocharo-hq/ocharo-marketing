using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Api.Marketing.Controllers;
/// <summary>Local channel planning and observed results; these routes never publish posts or read providers.</summary>
[ApiController]
[Route("api/marketing/operations")]
public sealed class OperationsController(OperationsService operations) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> Overview(CancellationToken ct)
  {
    return Ok(await operations.OverviewAsync(ct));
  }

  [HttpGet("channels")]
  public async Task<IActionResult> Channels(CancellationToken ct)
  {
    return Ok(await operations.ChannelsAsync(ct));
  }

  [HttpPut("channels/{channel}")]
  public async Task<IActionResult> Save(
      string channel,
      ChannelStrategyModel input,
      CancellationToken ct)
  {
    return Ok(await operations.SaveChannelAsync(
        channel,
        input,
        ct));
  }

  [HttpPost("observations")]
  public async Task<IActionResult> Observe(EngagementInput input, CancellationToken ct)
  {
    return Ok(await operations.RecordAsync(
        input,
        HttpContext.Items["Ocharo.UserId"] as string ?? "local-operator",
        ct));
  }
}
