using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Story editor API: CRUD, lifecycle (publish, schedule, unpublish, archive) and history.</summary>
[ApiController]
[Route("api/blog/articles")]
public sealed class ArticlesController(ArticleService articles, SiteService site, PublicationSiteService publications) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? topic, [FromQuery] string? q, [FromQuery] string? language, [FromQuery] string? publication, CancellationToken ct) =>
        Ok(await articles.ListAsync(status, topic, q, ct, language, publication));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await articles.GetAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(ArticleInput input, CancellationToken ct) => Ok(await articles.CreateAsync(input, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ArticleInput input, CancellationToken ct)
    {
        var current=await articles.GetAsync(id,ct);
        if(current.Document.Status is "published" or "scheduled" && HttpContext.Items["Ocharo.CanApprove"] is not true)
            return Problem(statusCode:403,detail:"Editing published or scheduled content requires Platform approval permission.");
        return Ok(await articles.UpdateAsync(id,input,ct));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, LifecycleInput input, CancellationToken ct) => Ok(await articles.PublishAsync(id, input.ExpectedRevision, ct));

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, LifecycleInput input, CancellationToken ct) =>
        Ok(await articles.ScheduleAsync(id, input.ExpectedRevision, input.PublishAt ?? throw new Application.Blog.BlogException("Choose a publication time."), ct));

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, LifecycleInput input, CancellationToken ct) => Ok(await articles.UnpublishAsync(id, input.ExpectedRevision, ct));

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, LifecycleInput input, CancellationToken ct) => Ok(await articles.ArchiveAsync(id, input.ExpectedRevision, ct));

    [HttpPost("{id:guid}/translate")]
    public async Task<IActionResult> Translate(Guid id, TranslateInput input, CancellationToken ct) => Ok(await articles.TranslateAsync(id, input.Language, ct));

    [HttpGet("{id:guid}/revisions")]
    public async Task<IActionResult> Revisions(Guid id, CancellationToken ct) => Ok(await articles.RevisionsAsync(id, ct));

    [HttpPost("{id:guid}/revisions/{revision:int}/restore")]
    public async Task<IActionResult> Restore(Guid id, int revision, LifecycleInput input, CancellationToken ct) =>
        Ok(await articles.RestoreAsync(id, revision, input.ExpectedRevision, ct));

    /// <summary>Server-rendered preview of any story, exactly as the public page would look.</summary>
    [HttpGet("/preview/{id:guid}")]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
    {
        var item = await articles.GetAsync(id, ct);
        return Content(item.Document.Publication == "blog" ? await site.PreviewAsync(id, ct) : await publications.PreviewAsync(id, ct), "text/html; charset=utf-8");
    }
}
