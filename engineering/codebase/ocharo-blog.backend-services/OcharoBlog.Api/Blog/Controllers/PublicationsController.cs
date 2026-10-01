using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Site;
namespace OcharoBlog.Api.Blog.Controllers;

[ApiController]
public sealed class PublicationsController(PublicationSiteService publications) : ControllerBase
{
    [HttpGet("/{publication:regex(^(atlas|wear|catalogue)$)}")]
    [HttpGet("/{publication:regex(^(atlas|wear|catalogue)$)}/{**tail}")]
    public Task<IActionResult> Public(string publication, string? tail, CancellationToken ct) => RenderAsync(publication, tail, false, ct);

    [HttpGet("/preview/apps/{publication:regex(^(atlas|wear|catalogue)$)}")]
    [HttpGet("/preview/apps/{publication:regex(^(atlas|wear|catalogue)$)}/{**tail}")]
    public Task<IActionResult> Preview(string publication, string? tail, CancellationToken ct) => RenderAsync(publication, tail, true, ct);

    [HttpGet("/api/blog/export/{publication:regex(^(atlas|wear|catalogue)$)}.zip")]
    public async Task<IActionResult> Export(string publication, [FromQuery] string? locale, CancellationToken ct) => File(await publications.ExportAsync(publication, ct, locale ?? "en"), "application/zip", $"ocharo-{publication}.zip");

    private async Task<IActionResult> RenderAsync(string publication, string? tail, bool preview, CancellationToken ct)
    {
        var path = "/" + publication + "/" + tail;
        if (path.EndsWith('/') == false && !Path.HasExtension(path)) return RedirectPermanent(Request.Path + "/" + Request.QueryString);
        var query = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
        var page = await publications.RenderAsync(path, query, ct, preview);
        if (preview || publication == "catalogue") Response.Headers.CacheControl = "no-store";
        if (page is null) return NotFound();
        return Content(page.Content, page.ContentType);
    }
}
