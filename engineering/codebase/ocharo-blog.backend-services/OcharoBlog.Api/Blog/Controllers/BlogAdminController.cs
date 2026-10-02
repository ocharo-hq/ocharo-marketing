using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Api.Blog.Controllers;
/// <summary>Topics, site settings, media library, brand status, catalogue picker and static export.</summary>
[ApiController]
[Route("api/blog")]
public sealed class BlogAdminController(
    TopicService topics,
    SiteSettingsService settings,
    IMediaStore media,
    IBlogRepository repository,
    ICatalogueFeed catalogue,
    IBrandKit brand,
    SiteService site) : ControllerBase
{
  [HttpGet("topics")]
  public async Task<IActionResult> Topics(CancellationToken ct)
  {
    return Ok(await topics.ListAsync(ct));
  }

  [HttpPut("topics/{key}")]
  public async Task<IActionResult> SaveTopic(
      string key,
      TopicDocument topic,
      CancellationToken ct)
  {
    return Ok(await topics.SaveAsync(topic with { Key = key }, ct));
  }

  [HttpDelete("topics/{key}")]
  public async Task<IActionResult> DeleteTopic(string key, CancellationToken ct)
  {
    await topics.DeleteAsync(key, ct);
    return NoContent();
  }

  [HttpGet("settings")]
  public async Task<IActionResult> Settings(CancellationToken ct)
  {
    return Ok(await settings.GetAsync(ct));
  }

  [HttpPut("settings")]
  public async Task<IActionResult> SaveSettings(SettingsInput input, CancellationToken ct)
  {
    return Ok(await settings.SaveAsync(
        input.Settings,
        input.ExpectedRevision,
        ct));
  }

  [HttpGet("media")]
  public async Task<IActionResult> Media(CancellationToken ct)
  {
    return Ok((await repository.GetMediaAsync(ct)).Select(View));
  }

  [HttpPost("media")]
  [RequestSizeLimit(21 * 1024 * 1024)]
  public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
  {
    if (file.Length is <= 0 or > 20 * 1024 * 1024)
      return Problem(statusCode: 413, title: "Images must contain 1 byte to 20 MiB.");
    await using var stream = file.OpenReadStream();
    using var buffer = new MemoryStream();
    await stream.CopyToAsync(buffer, ct);
    return Ok(View(await media.StoreAsync(
        buffer.ToArray(),
        file.FileName,
        ct)));
  }

  [HttpGet("brand")]
  public async Task<IActionResult> Brand(CancellationToken ct)
  {
    return Ok(await brand.GetAsync(ct) is { } kit ? new BrandStatus(
        true,
        kit.Release,
        kit.LogoSha256,
        kit.Links) : new BrandStatus(
        false,
        0,
        null,
        []));
  }

  [HttpGet("catalogue")]
  public async Task<IActionResult> Catalogue(CancellationToken ct)
  {
    return Ok(await catalogue.GetProductsAsync(ct));
  }

  [HttpGet("export/site.zip")]
  public async Task<IActionResult> Export(CancellationToken ct)
  {
    return File(
        await site.ExportAsync(ct),
        "application/zip",
        "ocharo-blog-site.zip");
  }

  [HttpGet("/media/{sha256}")]
  public Task<IActionResult> Original(string sha256, CancellationToken ct)
  {
    return ServeAsync(
        sha256,
        false,
        ct);
  }

  [HttpGet("/media/{sha256}/preview")]
  public Task<IActionResult> Preview(string sha256, CancellationToken ct)
  {
    return ServeAsync(
        sha256,
        true,
        ct);
  }

  private async Task<IActionResult> ServeAsync(
      string sha256,
      bool preview,
      CancellationToken ct)
  {
    var opened = await media.OpenAsync(
        sha256,
        preview,
        ct);
    if (opened is null)
      return NotFound();
    var (record, content) = opened.Value;
    Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    return File(content, preview && record.PreviewPath is not null ? "image/webp" : record.ContentType);
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
        r.PreviewPath is null ? null : $"/media/{r.Sha256}/preview");
  }

  public sealed record SettingsInput(SiteSettings Settings, int ExpectedRevision);
}
