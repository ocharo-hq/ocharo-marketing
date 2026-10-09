using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Marketing;

namespace OcharoMarketing.Api.Marketing.Controllers;

[ApiController]
public sealed class InquiriesController(InquiryService service, IConfiguration configuration) : ControllerBase
{
  [HttpPost("api/public/inquiries")]
  [EnableRateLimiting("public-inquiries")]
  [RequestSizeLimit(12_000)]
  public async Task<IActionResult> Submit(InquiryInput input, CancellationToken ct)
  {
    var result = await service.SubmitAsync(input, ct);
    return Accepted(new { reference = result.Id, reused = result.Reused, message = "Your enquiry is saved for Ocharo. No payment or order was placed." });
  }

  [HttpGet("api/marketing/inquiries")]
  public async Task<IActionResult> List(
      [FromQuery] string? status,
      [FromQuery] string? search,
      [FromQuery] string? followUp,
      [FromQuery] int offset = 0,
      [FromQuery] int limit = 50,
      CancellationToken ct = default)
  {
    return Ok(await service.ListAsync(
        status,
        search,
        followUp,
        offset,
        limit,
        ct));
  }

  [HttpGet("api/marketing/inquiries/{id}/activity")]
  public async Task<IActionResult> Activity(
      string id,
      [FromQuery] int offset = 0,
      [FromQuery] int limit = 50,
      [FromQuery] int? beforeRevision = null,
      CancellationToken ct = default)
  {
    return Ok(await service.ActivityAsync(
        id,
        offset,
        limit,
        ct,
        beforeRevision));
  }

  [HttpPut("api/marketing/inquiries/{id}")]
  public async Task<IActionResult> Update(
      string id,
      InquiryUpdate input,
      CancellationToken ct)
  {
    var actor = HttpContext.Items["Ocharo.UserId"] as string;
    if ((configuration.GetValue<bool>("Identity:Sso:Enabled") || (configuration["PlatformIntegration:Mode"] ?? "local") != "local") && string.IsNullOrWhiteSpace(actor))
      throw new MarketingException("A current operator identity is required.", 401);
    return Ok(await service.UpdateAsync(
        id,
        input,
        ct,
        actor));
  }
}
