using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Scanning;

namespace OcharoMarketing.Api.Scanning;

/// <summary>Exposes explicit image assistance and saved operator review.</summary>
[ApiController]
[Route("api/marketing/source-analysis")]
public sealed class SourceAnalysisController(ISourceAnalysisService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        return Ok(await service.ListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(SourceAnalysisInput input, CancellationToken ct)
    {
        return Ok(await service.AnalyzeAsync(input, ct));
    }

    [HttpPost("{id}/review")]
    public async Task<IActionResult> Review(string id, SourceReviewInput input, CancellationToken ct)
    {
        return Ok(await service.ReviewAsync(id, input, ct));
    }
}
