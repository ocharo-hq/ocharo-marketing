using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Inquiries.Commands;
using OcharoMarketing.Application.Inquiries.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Inquiries.Controllers;

/// <summary>Takes enquiries from the public form, and lets members review and follow them up.</summary>
[ApiController]
public sealed class InquiriesController(ISender sender) : ControllerBase
{
    [HttpPost("api/public/inquiries")]
    [EnableRateLimiting("public-inquiries")]
    [RequestSizeLimit(12_000)]
    public async Task<IActionResult> Submit(InquiryInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new InquirySubmitCommand { Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Accepted(new
            {
                reference = ok.Data.Id,
                reused = ok.Data.Reused,
                message = "Your enquiry is saved for Ocharo. No payment or order was placed.",
            }),
            fail => this.ToProblem(fail.Error));
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
        var query = new InquiriesGetQuery
        {
            Status = status,
            Search = search,
            FollowUp = followUp,
            Offset = offset,
            Limit = limit,
        };
        var result = await sender.SendAsync(query, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("api/marketing/inquiries/{id}/activity")]
    public async Task<IActionResult> Activity(
        string id,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        [FromQuery] int? beforeRevision = null,
        CancellationToken ct = default)
    {
        var query = new InquiryActivityGetQuery
        {
            Id = id,
            Offset = offset,
            Limit = limit,
            BeforeRevision = beforeRevision,
        };
        var result = await sender.SendAsync(query, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPut("api/marketing/inquiries/{id}")]
    public async Task<IActionResult> Update(
        string id,
        InquiryUpdate input,
        CancellationToken ct)
    {
        var command = new InquiryUpdateCommand { Id = id, Input = input, Actor = MarketingCaller.UserId(HttpContext) };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
