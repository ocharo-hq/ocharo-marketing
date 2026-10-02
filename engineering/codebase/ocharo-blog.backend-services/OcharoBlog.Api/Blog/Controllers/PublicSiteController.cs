using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Site;

namespace OcharoBlog.Api.Blog.Controllers;
/// <summary>The public blog, rendered live from published stories (same pages the static export contains).</summary>
[ApiController]
public sealed class PublicSiteController(SiteService site) : ControllerBase
{
  [HttpGet("/")]
  public Task<IActionResult> Home(CancellationToken ct)
  {
    return PageAsync(ct);
  }

  // Uzbek and Russian editions mirror the English routes under /uz/ and /ru/.
  [HttpGet("/{language:regex(^(uz|ru)$)}")]
  [HttpGet("/{language:regex(^(uz|ru)$)}/journal")]
  [HttpGet("/{language:regex(^(uz|ru)$)}/journal/{slug}")]
  [HttpGet("/{language:regex(^(uz|ru)$)}/topics/{key}")]
  public Task<IActionResult> Edition(CancellationToken ct)
  {
    return PageAsync(ct, directory: true);
  }

  [HttpGet("/{language:regex(^(uz|ru)$)}/feed.xml")]
  public Task<IActionResult> EditionFeed(CancellationToken ct)
  {
    return PageAsync(ct);
  }

  // Routing treats the trailing slash as optional; directory-style pages canonicalize to it (static export parity).
  [HttpGet("/journal")]
  [HttpGet("/journal/{slug}")]
  [HttpGet("/topics/{key}")]
  public Task<IActionResult> Directory(CancellationToken ct)
  {
    return PageAsync(ct, directory: true);
  }

  [HttpGet("/feed.xml")]
  [HttpGet("/sitemap.xml")]
  [HttpGet("/robots.txt")]
  [HttpGet("/site.css")]
  public Task<IActionResult> Resource(CancellationToken ct)
  {
    return PageAsync(ct);
  }

  private async Task<IActionResult> PageAsync(CancellationToken ct, bool directory = false)
  {
    var path = Request.Path.Value ?? "/";
    if (directory && !path.EndsWith('/'))
      return RedirectPermanent(path + "/" + Request.QueryString);
    var page = await site.RenderAsync(path, ct);
    return page is null ? NotFound() : Content(page.Content, page.ContentType);
  }
}
