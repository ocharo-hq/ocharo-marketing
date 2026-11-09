using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Automation.Commands;
using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Application.Automation.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Automation.Controllers;

/// <summary>Catalogue sync, bulk post creation, posting slots and blog-story posts.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class AutomationController(ISender sender) : ControllerBase
{
    [HttpPost("catalogue/sync")]
    public async Task<IActionResult> SyncCatalogue(CancellationToken ct)
    {
        var result = await sender.SendAsync(new CatalogueSyncCommand(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("automation/status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var result = await sender.SendAsync(new AutomationRunGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("automation")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.SendAsync(new AutomationSettingsGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPut("automation")]
    public async Task<IActionResult> Save(AutomationSettingsModel settings, CancellationToken ct)
    {
        var result = await sender.SendAsync(new AutomationSettingsSaveCommand { Settings = settings }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("automation/fill")]
    public async Task<IActionResult> Fill(FillInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new PostingSlotsFillCommand { Days = input.Days }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("automation/drafts")]
    public async Task<IActionResult> CreateDrafts(BulkDraftInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ProductDraftsCreateCommand { Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("automation/caption")]
    public async Task<IActionResult> Caption(CaptionInput input, CancellationToken ct)
    {
        var query = new CaptionGetQuery { ProductId = input.ProductId, Locale = input.Locale };
        var result = await sender.SendAsync(query, ct);

        return result.Match<IActionResult>(
            ok => Ok(new { caption = ok.Data }),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("blog/stories")]
    public async Task<IActionResult> Stories(CancellationToken ct)
    {
        var result = await sender.SendAsync(new BlogStoriesGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("blog/stories/draft")]
    public async Task<IActionResult> StoryDraft(StoryDraftInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new BlogStoryDraftCreateCommand { Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    public sealed record FillInput(int Days = 14);
    public sealed record CaptionInput(string ProductId, string Locale);
}
