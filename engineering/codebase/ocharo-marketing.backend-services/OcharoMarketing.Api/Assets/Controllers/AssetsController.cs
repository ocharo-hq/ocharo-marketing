using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Assets.Commands;
using OcharoMarketing.Application.Assets.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoMarketing.Api.Assets.Controllers;

/// <summary>Serves and stores marketing assets.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class AssetsController(ISender sender) : ControllerBase
{
    /// <summary>Serves an asset to a member: a redirect to a short-lived WoW2 Files link, or the local bytes of an
    /// asset
    /// stored before Files. The access middleware decides each request and the HTTP boundary keeps every API answer
    /// <c>no-store</c>, so a redirect is asked again each time.</summary>
    [HttpGet("assets/{id}")]
    public async Task<IActionResult> GetAsset(string id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new AssetGetQuery { Id = id }, ct);

        return result.Match<IActionResult>(
            ok => ok.Data.Location is { } location
                ? Redirect(location.AbsoluteUri)
                : File(ok.Data.Bytes!, ok.Data.Asset.ContentType, enableRangeProcessing: false),
            fail => this.ToProblem(fail.Error));
    }

    [HttpPost("assets")]
    [RequestSizeLimit(27_262_976)]
    [RequestFormLimits(MultipartBodyLengthLimit = 27_262_976)]
    public async Task<IActionResult> UploadAsset(
        IFormFile file,
        [FromForm] string provenance,
        [FromForm] string role = "reference",
        CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var command = new AssetSaveCommand
        {
            Bytes = buffer.ToArray(),
            FileName = file.FileName,
            Provenance = provenance,
            Role = role,
        };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(ok.Data),
            fail => this.ToProblem(fail.Error));
    }
}
