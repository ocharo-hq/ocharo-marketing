using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Application.Posts.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Posts.Controllers;

/// <summary>Imports an Ocharo Studio review export as a draft post.</summary>
[ApiController]
[Route("api/marketing/creative-imports")]
public sealed class CreativeImportsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(7_000_000)]
    public async Task<IActionResult> Import(
        IFormFile manifest,
        IFormFile image,
        [FromForm] string productId,
        CancellationToken ct)
    {
        var command = new CreativeImportCommand
        {
            Manifest = await ReadAsync(manifest, ct),
            Image = await ReadAsync(image, ct),
            ProductId = productId,
            OrganizationId = MarketingCaller.OrganizationId(HttpContext),
            UserId = MarketingCaller.UserId(HttpContext),
        };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }

    private static async Task<byte[]> ReadAsync(IFormFile file, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }
}
