using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Automation;

namespace OcharoMarketing.Api.Marketing.Controllers;
/// <summary>Catalogue sync, bulk post creation, posting slots and blog-story posts.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class AutomationController(
    AutomationRunner runner,
    AutomationService automation,
    BlogStoryService blog) : ControllerBase
{
  [HttpPost("catalogue/sync")]
  public async Task<IActionResult> SyncCatalogue(CancellationToken ct)
  {
    return Ok(await runner.SyncCatalogueAsync(ct));
  }

  [HttpGet("automation/status")]
  public async Task<IActionResult> Status(CancellationToken ct)
  {
    return Ok(await runner.StatusAsync(ct));
  }

  [HttpGet("automation")]
  public async Task<IActionResult> Get(CancellationToken ct)
  {
    return Ok(await automation.GetAsync(ct));
  }

  [HttpPut("automation")]
  public async Task<IActionResult> Save(AutomationSettingsModel settings, CancellationToken ct)
  {
    return Ok(await automation.SaveAsync(settings, ct));
  }

  [HttpPost("automation/fill")]
  public async Task<IActionResult> Fill(FillInput input, CancellationToken ct)
  {
    return Ok(await automation.FillAsync(input.Days, ct));
  }

  [HttpPost("automation/drafts")]
  public async Task<IActionResult> CreateDrafts(BulkDraftInput input, CancellationToken ct)
  {
    return Ok(await automation.CreateDraftsAsync(input, ct));
  }

  [HttpPost("automation/caption")]
  public async Task<IActionResult> Caption(CaptionInput input, CancellationToken ct)
  {
    return Ok(new
    {
      caption = await automation.CaptionAsync(
        input.ProductId,
        input.Locale,
        ct)
    });
  }

  [HttpGet("blog/stories")]
  public async Task<IActionResult> Stories(CancellationToken ct)
  {
    return Ok(await blog.ListAsync(ct));
  }

  [HttpPost("blog/stories/draft")]
  public async Task<IActionResult> StoryDraft(StoryDraftInput input, CancellationToken ct)
  {
    return Ok(await blog.DraftAsync(input, ct));
  }

  public sealed record FillInput(int Days = 14);
  public sealed record CaptionInput(string ProductId, string Locale);
}
