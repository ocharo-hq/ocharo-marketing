using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Api.Posts.Models;
using OcharoMarketing.Application.Posts.Commands;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Application.Posts.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Posts.Controllers;

/// <summary>Exposes draft review, approval, planning and export, and the pasted-text scan.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class DraftsController(ISender sender) : ControllerBase
{
    [HttpPost("drafts")]
    public async Task<IActionResult> CreateDraft(DraftInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new DraftSaveCommand { Id = null, Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPut("drafts/{id}")]
    public async Task<IActionResult> UpdateDraft(
        string id,
        DraftInput input,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new DraftSaveCommand { Id = id, Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("drafts/{id}/approve")]
    public async Task<IActionResult> ApproveDraft(
        string id,
        RevisionInput input,
        CancellationToken ct)
    {
        var command = new DraftApproveCommand { Id = id, ExpectedRevision = input.ExpectedRevision };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("drafts/{id}/schedule")]
    public async Task<IActionResult> ScheduleDraft(
        string id,
        ScheduleInput input,
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new DraftScheduleCommand { Id = id, Input = input }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    /// <summary>Downloads an approved revision's ZIP as bytes, never a redirect: the desk reads it with
    /// <c>fetch()</c>, which a cross-origin redirect to a WoW2 Files link would break, since Files' links answer no
    /// <c>Access-Control-Allow-Origin</c>.</summary>
    [HttpGet("drafts/{id}/export")]
    public async Task<IActionResult> Export(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new DraftExportGetQuery { Id = id }, ct);

        return result.Match<IActionResult>(
            ok => File(ok.Data, "application/zip", $"ocharo-draft-{id}.zip"),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan(TextInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new TextScanQuery { Text = input.Text }, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
