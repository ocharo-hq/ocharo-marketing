using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes topics, site settings, the media library, brand status, the catalogue picker and the static
/// export over HTTP.</summary>
[ApiController]
[Route("api/blog")]
public sealed class BlogAdminController(ISender sender) : ControllerBase
{
    [HttpGet("topics")]
    public async Task<IActionResult> Topics(CancellationToken ct)
    {
        var result = await sender.SendAsync(new TopicGetAllQuery(), ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpPut("topics/{key}")]
    public async Task<IActionResult> SaveTopic(string key, TopicDocument topic, CancellationToken ct)
    {
        var result = await sender.SendAsync(new TopicSaveCommand { Topic = topic with { Key = key } }, ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpDelete("topics/{key}")]
    public async Task<IActionResult> DeleteTopic(string key, CancellationToken ct)
    {
        var result = await sender.SendAsync(new TopicDeleteCommand { Key = key }, ct);

        return result.Match<IActionResult>(_ => NoContent(), fail => this.ToProblem(fail.Error));
    }

    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var result = await sender.SendAsync(new SiteSettingsGetQuery(), ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings(SettingsInput input, CancellationToken ct)
    {
        var command = new SiteSettingsSaveCommand
        {
            Settings = input.Settings,
            ExpectedRevision = input.ExpectedRevision,
        };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpGet("media")]
    public async Task<IActionResult> Media(CancellationToken ct)
    {
        var result = await sender.SendAsync(new MediaGetAllQuery(), ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data.Select(View)), fail => this.ToProblem(fail.Error));
    }

    [HttpPost("media")]
    [RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        var command = new MediaUploadCommand { Bytes = buffer.ToArray(), FileName = file.FileName };
        var result = await sender.SendAsync(command, ct);

        return result.Match<IActionResult>(ok => Ok(View(ok.Data)), fail => this.ToProblem(fail.Error));
    }

    [HttpGet("brand")]
    public async Task<IActionResult> Brand(CancellationToken ct)
    {
        var result = await sender.SendAsync(new BrandStatusGetQuery(), ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpGet("catalogue")]
    public async Task<IActionResult> Catalogue(CancellationToken ct)
    {
        var result = await sender.SendAsync(new CatalogueProductGetAllQuery(), ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpGet("export/site.zip")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var result = await sender.SendAsync(new SiteExportGetQuery(), ct);

        return result.Match<IActionResult>(
            ok => File(ok.Data, "application/zip", "ocharo-blog-site.zip"),
            fail => this.ToProblem(fail.Error));
    }

    [HttpGet("/media/{sha256}")]
    public Task<IActionResult> Original(string sha256, CancellationToken ct)
    {
        return ServeAsync(sha256, false, ct);
    }

    [HttpGet("/media/{sha256}/preview")]
    public Task<IActionResult> Preview(string sha256, CancellationToken ct)
    {
        return ServeAsync(sha256, true, ct);
    }

    private async Task<IActionResult> ServeAsync(string sha256, bool preview, CancellationToken ct)
    {
        var result = await sender.SendAsync(new MediaContentGetQuery { Sha256 = sha256, Preview = preview }, ct);

        return result.Match<IActionResult>(ok => Serve(ok.Data.Content), fail => this.ToProblem(fail.Error));
    }

    private IActionResult Serve(MediaContent? opened)
    {
        // An image Editorial does not hold answers the same bare 404 as any other missing public resource.
        if (opened is null)
            return NotFound();
        // EditorialAccessMiddleware decides public or private on every request, so a redirect to a Files link is never
        // cached: the next request decides again and gets a fresh link.
        if (opened.Location is { } location)
        {
            Response.Headers.CacheControl = "no-store";
            return Redirect(location.AbsoluteUri);
        }
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(opened.Content!, opened.ContentType);
    }

    private static MediaView View(MediaBlobRecord r)
    {
        return new(
            r.Sha256,
            r.FileName,
            r.ContentType,
            r.ByteLength,
            r.Width,
            r.Height,
            $"/media/{r.Sha256}",
            MediaPreview.Exists(r) ? $"/media/{r.Sha256}/preview" : null);
    }

    public sealed record SettingsInput(SiteSettings Settings, int ExpectedRevision);
}
