using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog;

namespace OcharoBlog.Api.Blog.Controllers;
/// <summary>Private image preview and immutable edited-copy operations, protected by the integration boundary.</summary>
[ApiController]
[Route("api/blog/media")]
public sealed class MediaEditingController(
    IMediaEditingService editing,
    IBlogRepository repository,
    IConfiguration config) : ControllerBase
{
  [HttpGet("editing-capabilities")]
  public async Task<IActionResult> Capabilities(CancellationToken ct)
  {
    return Ok(new { crop = true, rotate = true, flip = true, resize = true, formats = new[] { "png", "jpeg", "webp" }, maximumBytes = MediaEditingService.MaximumBytes, maximumPixels = MediaEditingService.MaximumPixels, maximumDimension = MediaEditingService.MaximumDimension, backgroundRemoval = await editing.BackgroundStatusAsync(ct) });
  }

  [HttpPost("{sha}/edits/preview")]
  [RequestSizeLimit(8192)]
  public async Task<IActionResult> Preview(
      string sha,
      MediaEditRecipe recipe,
      CancellationToken ct)
  {
    var result = await editing.PreviewAsync(
        sha,
        recipe,
        ct);
    Response.Headers.XContentTypeOptions = "nosniff";
    Response.Headers["X-Image-Width"] = result.Width.ToString(System.Globalization.CultureInfo.InvariantCulture);
    Response.Headers["X-Image-Height"] = result.Height.ToString(System.Globalization.CultureInfo.InvariantCulture);
    return File(result.Bytes, result.ContentType);
  }

  [HttpPost("{sha}/edits")]
  [RequestSizeLimit(8192)]
  public async Task<IActionResult> Save(
      string sha,
      MediaEditRecipe recipe,
      CancellationToken ct)
  {
    var actor = !config.GetValue<bool>("Identity:Sso:Enabled") && (config["PlatformIntegration:Mode"] ?? "local") == "local" ? new MediaEditActor("local", "local-operator") : HttpContext.Items["Ocharo.OrganizationId"] is string org && HttpContext.Items["Ocharo.UserId"] is string user ? new MediaEditActor(org, user) : throw new BlogException("Reconnect from Platform.", 401);
    var r = await editing.SaveAsync(
        sha,
        recipe,
        actor,
        ct);
    return Ok(new MediaView(
        r.Sha256,
        r.FileName,
        r.ContentType,
        r.ByteLength,
        r.Width,
        r.Height,
        $"/media/{r.Sha256}",
        r.PreviewPath is null ? null : $"/media/{r.Sha256}/preview"));
  }

  [HttpGet("{sha}/edits")]
  public async Task<IActionResult> History(string sha, CancellationToken ct)
  {
    return Ok(await repository.GetMediaEditsAsync(sha, ct));
  }
}
