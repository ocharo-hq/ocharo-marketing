using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Application.Scanning.Commands;
using OcharoMarketing.Application.Scanning.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Scanning.Controllers;

/// <summary>Exposes explicit image assistance and saved operator review.</summary>
[ApiController]
[Route("api/marketing/source-analysis")]
public sealed class SourceAnalysisController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.SendAsync(new SourceAnalysesGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(SourceAnalysisInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new SourceAnalysisCreateCommand { Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> Review(
        string id,
        SourceReviewInput input,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new SourceAnalysisReviewCommand { Id = id, Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
