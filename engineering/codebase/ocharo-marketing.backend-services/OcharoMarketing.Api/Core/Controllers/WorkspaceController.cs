using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Core.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Core.Controllers;

/// <summary>Exposes the local single-operator marketing workspace.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class WorkspaceController(ISender sender) : ControllerBase
{
    [HttpGet("workspace")]
    public async Task<IActionResult> Workspace(CancellationToken ct)
    {
        var result = await sender.SendAsync(new WorkspaceGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
