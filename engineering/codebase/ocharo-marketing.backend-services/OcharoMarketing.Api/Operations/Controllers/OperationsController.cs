using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Application.Operations.Commands;
using OcharoMarketing.Application.Operations.Models;
using OcharoMarketing.Application.Operations.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Operations.Controllers;

/// <summary>Local channel planning and observed results; these routes never publish posts or read providers.</summary>
[ApiController]
[Route("api/marketing/operations")]
public sealed class OperationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var result = await sender.SendAsync(new OperationsOverviewGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("channels")]
    public async Task<IActionResult> Channels(CancellationToken ct)
    {
        var result = await sender.SendAsync(new ChannelStrategiesGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPut("channels/{channel}")]
    public async Task<IActionResult> Save(
        string channel,
        ChannelStrategyModel input,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new ChannelStrategySaveCommand { Channel = channel, Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("observations")]
    public async Task<IActionResult> Observe(EngagementInput input, CancellationToken ct)
    {
        var actor = MarketingCaller.UserId(HttpContext) ?? "local-operator";
        var result = await sender.SendAsync(new EngagementRecordCommand { Input = input, Actor = actor }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
