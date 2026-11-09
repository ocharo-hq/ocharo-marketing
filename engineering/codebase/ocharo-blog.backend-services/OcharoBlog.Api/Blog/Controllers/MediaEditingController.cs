using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Api.Blog.Errors;
using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes private image previews and immutable edited copies over HTTP, protected by the access
/// middleware.</summary>
[ApiController]
[Route("api/blog/media")]
public sealed class MediaEditingController(ISender sender) : ControllerBase
{
    [HttpGet("editing-capabilities")]
    public async Task<IActionResult> Capabilities(CancellationToken ct)
    {
        var result = await sender.SendAsync(new MediaBackgroundRemovalStatusGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => Ok(new
            {
                crop = true,
                rotate = true,
                flip = true,
                resize = true,
                formats = new[] { "png", "jpeg", "webp" },
                maximumBytes = MediaEditingService.MaximumBytes,
                maximumPixels = MediaEditingService.MaximumPixels,
                maximumDimension = MediaEditingService.MaximumDimension,
                backgroundRemoval = ok.Data,
            }),
            fail => this.ToBlogProblem(fail.Error));
    }

    [HttpPost("{sha}/edits/preview")]
    [RequestSizeLimit(8192)]
    public async Task<IActionResult> Preview(string sha, MediaEditRecipe recipe, CancellationToken ct)
    {
        var result = await sender.SendAsync(new MediaEditPreviewQuery { Sha256 = sha, Recipe = recipe }, ct);

        return result.Match<IActionResult>(
            ok =>
            {
                Response.Headers.XContentTypeOptions = "nosniff";
                Response.Headers["X-Image-Width"] = ok.Data.Width.ToString(CultureInfo.InvariantCulture);
                Response.Headers["X-Image-Height"] = ok.Data.Height.ToString(CultureInfo.InvariantCulture);
                return File(ok.Data.Bytes, ok.Data.ContentType);
            },
            fail => this.ToBlogProblem(fail.Error));
    }

    [HttpPost("{sha}/edits")]
    [RequestSizeLimit(8192)]
    public async Task<IActionResult> Save(string sha, MediaEditRecipe recipe, CancellationToken ct)
    {
        var actor = HttpContext.Items["Ocharo.OrganizationId"] is string org
            && HttpContext.Items["Ocharo.UserId"] is string user
                ? new MediaEditActor(org, user)
                : null;
        var command = new MediaEditSaveCommand { Sha256 = sha, Recipe = recipe, Actor = actor };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(
            ok => Ok(new MediaView(
                ok.Data.Sha256,
                ok.Data.FileName,
                ok.Data.ContentType,
                ok.Data.ByteLength,
                ok.Data.Width,
                ok.Data.Height,
                $"/media/{ok.Data.Sha256}",
                MediaPreview.Exists(ok.Data) ? $"/media/{ok.Data.Sha256}/preview" : null)),
            fail => this.ToBlogProblem(fail.Error));
    }

    [HttpGet("{sha}/edits")]
    public async Task<IActionResult> History(string sha, CancellationToken ct)
    {
        var result = await sender.SendAsync(new MediaEditGetAllQuery { Sha256 = sha }, ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToBlogProblem(fail.Error));
    }
}
