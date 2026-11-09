using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Api.Blog.Errors;
using OcharoBlog.Application.Blog.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes the import of the editorial research pack over HTTP.</summary>
[ApiController]
[Route("api/blog/research")]
public sealed class ResearchController(ISender sender, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("import")]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        var command = new ResearchImportCommand { ContentRoot = environment.ContentRootPath };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToBlogProblem(fail.Error));
    }
}
