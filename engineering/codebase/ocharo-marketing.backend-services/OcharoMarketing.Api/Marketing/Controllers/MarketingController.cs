using Microsoft.AspNetCore.Mvc;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Api.Marketing.Controllers;

/// <summary>Exposes the local single-operator marketing workspace.</summary>
[ApiController]
[Route("api/marketing")]
public sealed class MarketingController(IMarketingRepository repository, MarketingService service,
    IAssetService assets, ICardRenderingService cards, IExternalMarketingService external,
    IDeliveryRepository delivery, PublicationService publication, TimeProvider time) : ControllerBase
{
    [HttpGet("workspace")]
    public async Task<IActionResult> Workspace(CancellationToken ct) => Ok(new
    {
        brand = await repository.GetBrandAsync(ct), products = await repository.GetProductsAsync(ct),
        assets = await repository.GetAssetsAsync(ct), presets = cards.GetPresets(),
        drafts = await repository.GetDraftsAsync(ct), schedules = await repository.GetSchedulesAsync(ct),
        connections = await publication.ConnectionsAsync(ct), generationJobs = await repository.GetJobsAsync(ct),
        publicationOperations = await delivery.GetOperationsAsync(ct), deliverySettings = publication.Settings,
        metrics = await CachedMetricsAsync(ct)
    });

    private async Task<IReadOnlyList<MetricModel>> CachedMetricsAsync(CancellationToken ct)
    {
        var observed = await delivery.GetCachedObservationsAsync(ct);
        var metrics = observed.Select(x => new MetricModel(x.Provider, x.Name, x.Value, x.Availability, x.ObservedAt,
            x.Reason ?? "Cached provider observation; loading this workspace does not refresh it.", x.ScheduleId, x.ReceiptId, x.Scope, x.Aggregation)).ToList();
        foreach (var channel in new[] { "instagram", "telegram", "whatsapp", "x" })
            if (!observed.Any(x => x.Provider == channel)) metrics.Add(new(channel, channel == "whatsapp" ? "Delivery" : "Engagement", null,
                channel == "telegram" ? "not_supported" : "unavailable", null,
                "No observed metric is cached. A plan or publication receipt is not engagement evidence."));
        return metrics;
    }

    [HttpPost("drafts")]
    public async Task<IActionResult> CreateDraft(DraftInput input, CancellationToken ct) => Ok(await service.SaveDraftAsync(null, input, ct));

    [HttpPut("drafts/{id}")]
    public async Task<IActionResult> UpdateDraft(string id, DraftInput input, CancellationToken ct) => Ok(await service.SaveDraftAsync(id, input, ct));

    [HttpPost("drafts/{id}/approve")]
    public async Task<IActionResult> ApproveDraft(string id, RevisionInput input, CancellationToken ct) => Ok(await service.ApproveAsync(id, input.ExpectedRevision, ct));

    [HttpPost("drafts/{id}/schedule")]
    public async Task<IActionResult> ScheduleDraft(string id, ScheduleInput input, CancellationToken ct) => Ok(await service.ScheduleAsync(id, input, ct));

    [HttpPost("schedules/{id}/cancel")]
    public async Task<IActionResult> CancelSchedule(string id, CancellationToken ct) => Ok(await delivery.CancelAsync(id, time.GetUtcNow(), ct));

    [HttpGet("drafts/{id}/export")]
    public async Task<IActionResult> Export(string id, CancellationToken ct) => File(await assets.ExportDraftAsync(id, ct), "application/zip", $"ocharo-draft-{id}.zip");

    [HttpGet("assets/{id}")]
    public async Task<IActionResult> GetAsset(string id, CancellationToken ct)
    {
        var item = await assets.ReadAsync(id, ct);
        return File(item.Bytes, item.Asset.ContentType, enableRangeProcessing: false);
    }

    [HttpPost("assets")]
    [RequestSizeLimit(27_262_976)]
    [RequestFormLimits(MultipartBodyLengthLimit = 27_262_976)]
    public async Task<IActionResult> UploadAsset(IFormFile file, [FromForm] string provenance, [FromForm] string role = "reference", CancellationToken ct = default)
    {
        if (file.Length is <= 0 or > 26_214_400) throw new MarketingException("Choose a nonempty image smaller than 25 MB.", 413);
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return Ok(await assets.SaveAsync(buffer.ToArray(), file.FileName, provenance, role, ct));
    }

    [HttpPost("scan")]
    public IActionResult Scan(TextInput input) => Ok(MarketingService.Scan(input.Text));

    [HttpPost("assist/text")]
    public async Task<IActionResult> Assist(AssistanceInput input, CancellationToken ct) => Ok(await external.AssistAsync(input.Prompt, input.Operation, input.Locale, ct));

    [HttpPost("generation-jobs")]
    public async Task<IActionResult> Generate(ImageGenerationInput input, CancellationToken ct)
    {
        if (!external.CanGenerate(input.Provider)) throw new MarketingException("This image provider is not enabled and configured.", 409);
        if (string.IsNullOrWhiteSpace(input.Prompt) || input.Prompt.Length > 12000) throw new MarketingException("Provide a generation prompt under 12000 characters.");
        if (input.ProductId is not null && await repository.FindProductAsync(input.ProductId, ct) is null)
            throw new MarketingException("Product not found.", 404);
        if ((input.ReferenceAssetIds?.Length ?? 0) > 5) throw new MarketingException("Select at most five reference images.");
        foreach (var reference in input.ReferenceAssetIds ?? []) await assets.ReadAsync(reference, ct);
        var job = new GenerationJobModel(Guid.NewGuid().ToString("N"), "image", input.Provider, input.Prompt,
            input.ProductId, "queued", DateTimeOffset.UtcNow, [], null);
        await repository.EnqueueAsync(job, MarketingJson.Write(input), ct);
        return Ok(job);
    }

    [HttpPost("cards/preview")]
    public async Task<IActionResult> Preview(CardInput input, CancellationToken ct) => Ok(await service.PreviewAsync(input, ct));

    [HttpPost("cards/render")]
    public async Task<IActionResult> Render(CardInput input, CancellationToken ct) => Ok(await service.RenderAsync(input, ct));

    [HttpPost("atlas/export")]
    public async Task<IActionResult> Atlas(AtlasInput input, CancellationToken ct) => File(await service.ExportAtlasAsync(input.AssetIds, ct), "application/pdf", "ocharo-post-atlas.pdf");
}
