using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Api.Blog.Errors;
using OcharoBlog.Application.Blog.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes the public blog over HTTP, rendered live from published stories (the same pages the static export
/// contains).</summary>
[ApiController]
public sealed class PublicSiteController(ISender sender) : ControllerBase
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
    [HttpGet("/boot-splash.css")]
    public Task<IActionResult> Resource(CancellationToken ct)
    {
        return PageAsync(ct);
    }

    private async Task<IActionResult> PageAsync(CancellationToken ct, bool directory = false)
    {
        var path = Request.Path.Value ?? "/";
        if (directory && !path.EndsWith('/'))
            return RedirectPermanent(path + "/" + Request.QueryString);
        var result = await sender.SendAsync(new SitePageGetQuery { Path = path }, ct);

        // A path no page answers stays the same bare 404 as any other missing public resource.
        return result.Match<IActionResult>(
            ok => ok.Data.Page is { } page ? Content(page.Content, page.ContentType) : NotFound(),
            fail => this.ToBlogProblem(fail.Error));
    }
}
