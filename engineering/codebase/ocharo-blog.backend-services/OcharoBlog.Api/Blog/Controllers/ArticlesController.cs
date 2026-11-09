using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;
using WoW.Two.Sdk.Beta.Be.Web.SecureHeaders;

namespace OcharoBlog.Api.Blog.Controllers;

/// <summary>Exposes stories over HTTP: CRUD, lifecycle (publish, schedule, unpublish, archive) and history.</summary>
[ApiController]
[Route("api/blog/articles")]
public sealed class ArticlesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? topic,
        [FromQuery] string? q,
        [FromQuery] string? language,
        [FromQuery] string? publication,
        CancellationToken ct)
    {
        var query = new ArticleGetAllQuery
        {
            Status = status,
            Topic = topic,
            Search = q,
            Language = language,
            Publication = publication,
        };
        var result = await sender.SendAsync(query, ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ArticleGetByIdQuery { Id = id }, ct);

        return Detail(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ArticleInput input, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ArticleCreateCommand { Input = input }, ct);

        return Detail(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ArticleInput input, CancellationToken ct)
    {
        var command = new ArticleUpdateCommand
        {
            Id = id,
            Input = input,
            CanApprove = HttpContext.Items["Ocharo.CanApprove"] is true,
        };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, LifecycleInput input, CancellationToken ct)
    {
        var command = new ArticlePublishCommand { Id = id, ExpectedRevision = input.ExpectedRevision };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, LifecycleInput input, CancellationToken ct)
    {
        var command = new ArticleScheduleCommand
        {
            Id = id,
            ExpectedRevision = input.ExpectedRevision,
            PublishAt = input.PublishAt,
        };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, LifecycleInput input, CancellationToken ct)
    {
        var command = new ArticleUnpublishCommand { Id = id, ExpectedRevision = input.ExpectedRevision };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, LifecycleInput input, CancellationToken ct)
    {
        var command = new ArticleArchiveCommand { Id = id, ExpectedRevision = input.ExpectedRevision };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    [HttpPost("{id:guid}/translate")]
    public async Task<IActionResult> Translate(Guid id, TranslateInput input, CancellationToken ct)
    {
        var command = new ArticleTranslateCommand { Id = id, Language = input.Language };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    [HttpGet("{id:guid}/revisions")]
    public async Task<IActionResult> Revisions(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ArticleRevisionGetAllQuery { ArticleId = id }, ct);

        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }

    [HttpPost("{id:guid}/revisions/{revision:int}/restore")]
    public async Task<IActionResult> Restore(Guid id, int revision, LifecycleInput input, CancellationToken ct)
    {
        var command = new ArticleRevisionRestoreCommand
        {
            Id = id,
            Revision = revision,
            ExpectedRevision = input.ExpectedRevision,
        };
        var result = await sender.SendAsync(command, ct);

        return Detail(result);
    }

    /// <summary>Server-rendered preview of any story, exactly as the public page would look.</summary>
    [HttpGet("/preview/{id:guid}")]
    [SecureHeaderOverride(StudioPreviewFraming.FrameOptionsHeader, StudioPreviewFraming.SameOrigin)]
    [SecureHeaderOverride(StudioPreviewFraming.PolicyHeader, StudioPreviewFraming.Policy)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
    {
        var result = await sender.SendAsync(new ArticlePreviewGetQuery { Id = id }, ct);

        return result.Match<IActionResult>(
            ok => Content(ok.Data, "text/html; charset=utf-8"),
            fail => this.ToProblem(fail.Error));
    }

    private IActionResult Detail(WoW.Two.Sdk.Beta.Be.Mediator.Result.AppResult<ArticleDetail> result)
    {
        return result.Match<IActionResult>(ok => Ok(ok.Data), fail => this.ToProblem(fail.Error));
    }
}
