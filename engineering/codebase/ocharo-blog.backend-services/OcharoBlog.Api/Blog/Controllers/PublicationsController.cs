using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Api.Blog.Errors;
using OcharoBlog.Application.Blog.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.SecureHeaders;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes the public apps Atlas, Wear and the Catalogue over HTTP, with their previews and static
/// exports.</summary>
[ApiController]
public sealed class PublicationsController(ISender sender) : ControllerBase
{
    [HttpGet("/{publication:regex(^(atlas|wear|catalogue)$)}")]
    [HttpGet("/{publication:regex(^(atlas|wear|catalogue)$)}/{**tail}")]
    public Task<IActionResult> Public(string publication, string? tail, CancellationToken ct)
    {
        return RenderAsync(publication, tail, false, ct);
    }

    [HttpGet("/preview/apps/{publication:regex(^(atlas|wear|catalogue)$)}")]
    [HttpGet("/preview/apps/{publication:regex(^(atlas|wear|catalogue)$)}/{**tail}")]
    [SecureHeaderOverride(StudioPreviewFraming.FrameOptionsHeader, StudioPreviewFraming.SameOrigin)]
    [SecureHeaderOverride(StudioPreviewFraming.PolicyHeader, StudioPreviewFraming.Policy)]
    public Task<IActionResult> Preview(string publication, string? tail, CancellationToken ct)
    {
        return RenderAsync(publication, tail, true, ct);
    }

    [HttpGet("/api/blog/export/{publication:regex(^(atlas|wear|catalogue)$)}.zip")]
    public async Task<IActionResult> Export(string publication, [FromQuery] string? locale, CancellationToken ct)
    {
        var query = new PublicationExportGetQuery { Publication = publication, Locale = locale ?? "en" };
        var result = await sender.SendAsync(query, ct);

        return result.Match<IActionResult>(
            ok => File(ok.Data, "application/zip", $"ocharo-{publication}.zip"),
            fail => this.ToBlogProblem(fail.Error));
    }

    private async Task<IActionResult> RenderAsync(string publication, string? tail, bool preview, CancellationToken ct)
    {
        var path = "/" + publication + "/" + tail;
        if (path.EndsWith('/') == false && !Path.HasExtension(path))
            return RedirectPermanent(Request.Path + "/" + Request.QueryString);
        var query = new PublicationPageGetQuery
        {
            Path = path,
            Arguments = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Preview = preview,
        };
        var result = await sender.SendAsync(query, ct);

        return result.Match<IActionResult>(
            ok =>
            {
                if (preview || publication == "catalogue")
                    Response.Headers.CacheControl = "no-store";
                // A path no page answers stays the same bare 404 as any other missing public resource.
                return ok.Data.Page is { } page ? Content(page.Content, page.ContentType) : NotFound();
            },
            fail => this.ToBlogProblem(fail.Error));
    }
}
