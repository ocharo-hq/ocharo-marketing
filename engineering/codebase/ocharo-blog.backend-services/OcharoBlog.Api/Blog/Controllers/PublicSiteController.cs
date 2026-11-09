using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Api.Blog.Extensions;
using OcharoBlog.Application.Blog.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;
using WoW.Two.Sdk.Beta.Be.Web.ProblemDetails;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes the public blog over HTTP, rendered live from published stories (the same pages the static export
/// contains). Its pages are documents: a miss answers the site's own not-found page, not problem JSON, so the
/// controller keeps its error responses and carries no <c>[ApiController]</c>.</summary>
[DocumentResponses]
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

    /// <summary>Answers every other public path with the not-found page. The Studio, the API, media, previews, health
    /// and <c>.well-known</c> stay outside it, so their own 404s and the Studio's shell keep answering there.</summary>
    [HttpGet(PublicMiss)]
    [HttpHead(PublicMiss)]
    public Task<IActionResult> Missing(CancellationToken ct)
    {
        return this.NotFoundPageAsync(sender, Request.Path.Value ?? "/", ct);
    }

    /// <summary>Matches a public path outside the prefixes that answer their own misses.</summary>
    private const string PublicMiss = "/{**path:regex(^(?!(studio|api|media|preview|health|\\.well-known)(/|$)).+)}";

    private async Task<IActionResult> PageAsync(CancellationToken ct, bool directory = false)
    {
        var path = Request.Path.Value ?? "/";
        if (directory && !path.EndsWith('/'))
            return RedirectPermanent(path + "/" + Request.QueryString);
        var result = await sender.SendAsync(new SitePageGetQuery { Path = path }, ct);

        return await result.Match<Task<IActionResult>>(
            ok => ok.Data.Page is { } page
                ? Task.FromResult<IActionResult>(Content(page.Content, page.ContentType))
                : this.NotFoundPageAsync(sender, path, ct),
            fail => Task.FromResult<IActionResult>(this.ToProblem(fail.Error)));
    }
}
