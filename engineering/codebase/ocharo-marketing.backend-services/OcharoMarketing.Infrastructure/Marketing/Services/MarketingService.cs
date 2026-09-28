using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Coordinates local draft review, immutable scheduling and deterministic card preparation.</summary>
public sealed class MarketingService(IMarketingRepository repository, IAssetService assets, ICardRenderingService cards)
{
    private static readonly string[] Channels = ["instagram", "telegram", "whatsapp", "x"];

    public async Task<DraftModel> SaveDraftAsync(string? id, DraftInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160 || input.Caption.Length > 10000)
            throw new MarketingException("Provide a title under 160 characters and caption under 10000 characters.");
        if (input.Locale is not ("en" or "ru" or "uz")) throw new MarketingException("Choose English, Russian or Uzbek.");
        if (input.Channels is null || input.Channels.Length == 0 || input.Channels.Except(Channels).Any())
            throw new MarketingException("Choose at least one supported channel.");
        var product = await repository.FindProductAsync(input.ProductId, ct) ?? throw new MarketingException("Product not found.", 404);
        if (!cards.GetPresets().Any(x => x.Id == input.PresetId)) throw new MarketingException("Preset not found.", 404);
        var mediaIds = (input.MediaIds ?? [product.HeroAssetId]).Distinct().ToArray();
        if (mediaIds.Length > 10) throw new MarketingException("Select at most ten images.");
        foreach (var assetId in mediaIds)
            if (await repository.FindAssetAsync(assetId, ct) is null) throw new MarketingException("A selected asset no longer exists.", 409);
        if (input.Composition is { } recipe)
        {
            if (string.IsNullOrWhiteSpace(recipe.Title) || recipe.Title.Length > 160 || recipe.Subtitle is null || recipe.Subtitle.Length > 200)
                throw new MarketingException("Use a card title under 160 characters and supporting text under 200 characters.");
            if (!double.IsFinite(recipe.Scale) || recipe.Scale is < 0.75 or > 1.05
                || !double.IsFinite(recipe.OffsetX) || Math.Abs(recipe.OffsetX) > 0.12
                || !double.IsFinite(recipe.OffsetY) || Math.Abs(recipe.OffsetY) > 0.06)
                throw new MarketingException("The saved composition exceeds the supported framing range.");
            if (recipe.FabricAssetId is not null && await repository.FindAssetAsync(recipe.FabricAssetId, ct) is null)
                throw new MarketingException("The selected fabric asset no longer exists.", 409);
        }
        var previous = id is null ? null : await repository.FindDraftAsync(id, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (previous is not null && input.ExpectedRevision != previous.Revision)
            throw new MarketingException("The draft changed. Reload before saving.", 409);
        var now = DateTimeOffset.UtcNow;
        var draft = new DraftModel(id ?? Guid.NewGuid().ToString("N"), input.Title.Trim(), input.ProductId, input.PresetId,
            input.Locale, input.Caption, input.Channels.Distinct().ToArray(), mediaIds, (previous?.Revision ?? 0) + 1,
            null, "draft", previous?.CreatedAt ?? now, now, input.Composition);
        return await repository.SaveDraftAsync(draft, previous?.Revision, ct);
    }

    public async Task<DraftModel> ApproveAsync(string id, int expectedRevision, CancellationToken ct)
    {
        var draft = await repository.FindDraftAsync(id, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (draft.MediaIds.Length == 0 && string.IsNullOrWhiteSpace(draft.Caption)) throw new MarketingException("Review text or select media before approving this draft.");
        foreach (var assetId in draft.MediaIds) await assets.ReadAsync(assetId, ct);
        return await repository.ApproveDraftAsync(id, expectedRevision, ct);
    }

    public async Task<ScheduleModel> ScheduleAsync(string id, ScheduleInput input, CancellationToken ct)
    {
        var draft = await repository.FindDraftAsync(id, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (!draft.Channels.Contains(input.Channel) || !Channels.Contains(input.Channel))
            throw new MarketingException("Select a channel included in this approved draft.");
        if (input.ScheduledAt <= DateTimeOffset.UtcNow)
            throw new MarketingException("Select a future date and time.");
        var schedule = new ScheduleModel(Guid.NewGuid().ToString("N"), id, input.ExpectedRevision, input.Channel,
            input.ScheduledAt.ToUniversalTime(), "planned", "Saved as a plan. Live delivery is disabled; export this approved revision for manual publishing.", null, draft.Title);
        return await repository.ScheduleAsync(schedule, ct);
    }

    public async Task<CardPreview> PreviewAsync(CardInput input, CancellationToken ct)
    {
        var product = await repository.FindProductAsync(input.ProductId, ct) ?? throw new MarketingException("Product not found.", 404);
        var hero = await assets.ReadAsync(product.HeroAssetId, ct);
        PrivateAssetContent? fabric = input.FabricAssetId is null ? null : await assets.ReadAsync(input.FabricAssetId, ct);
        return cards.Compose(new CardCompositionInput(product.Id, input.PresetId, input.Title, input.Subtitle ?? "", input.Locale,
            new CardImage(hero.Bytes, hero.Asset.ContentType), fabric is null ? null : new CardImage(fabric.Bytes, fabric.Asset.ContentType),
            input.Scale ?? 1, input.OffsetX ?? 0, input.OffsetY ?? 0));
    }

    public async Task<GenerationJobModel> RenderAsync(CardInput input, CancellationToken ct)
    {
        var preview = await PreviewAsync(input, ct);
        if (string.IsNullOrWhiteSpace(preview.Html)) throw new MarketingException(string.Join(" ", preview.Issues));
        var job = new GenerationJobModel(Guid.NewGuid().ToString("N"), "card", "local", input.Title, input.ProductId,
            "queued", DateTimeOffset.UtcNow, [], null);
        // Freeze the exact HTML so a later preset change cannot mutate this job's output.
        await repository.EnqueueAsync(job, MarketingJson.Write(new CardRenderPayload(preview.Html, input.PresetId)), ct);
        return job;
    }

    public async Task<byte[]> ExportAtlasAsync(string[] assetIds, CancellationToken ct)
    {
        if (assetIds.Length is < 1 or > 24) throw new MarketingException("Select between one and twenty-four card images.");
        var jobs = (await repository.GetJobsAsync(ct)).Where(x => x.Kind == "card" && x.Status == "completed").ToArray();
        var products = await repository.GetProductsAsync(ct);
        var presets = cards.GetPresets();
        var entries = new List<CardAtlasItem>();
        foreach (var id in assetIds.Distinct())
        {
            var asset = await assets.ReadAsync(id, ct);
            if (asset.Asset.Role != "card" || asset.Asset.ContentType != "image/png")
                throw new MarketingException("Atlas exports accept completed PNG card renders.");
            var job = jobs.FirstOrDefault(x => x.OutputAssetIds.Contains(id));
            var product = products.FirstOrDefault(x => x.Id == job?.ProductId);
            var payloadJson = job is null ? null : await repository.FindJobPayloadAsync(job.Id, ct);
            var payload = payloadJson is null ? null : MarketingJson.Read<CardRenderPayload>(payloadJson);
            var preset = presets.FirstOrDefault(x => x.Id == payload?.PresetId);
            var title = string.IsNullOrWhiteSpace(job?.Prompt) ? product?.Name ?? "Product card" : job.Prompt;
            var notes = product is null ? "Review framing, title readability and product fidelity." :
                $"{product.Name} · Compare framing, title readability and product fidelity.";
            entries.Add(new CardAtlasItem(title, preset?.Name ?? "Product card variation", asset.Bytes, notes));
        }
        return await cards.RenderAtlasPdfAsync(entries, ct);
    }

    public static ScanModel Scan(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 50000) throw new MarketingException("Paste nonempty source text under 50000 characters.");
        var lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new ScanModel($"Captured {lines.Length} source lines for review.",
            lines.Take(12).Select(x => x.Length > 300 ? x[..300] + "…" : x).ToArray(),
            ["These are pasted-text suggestions, not verified product facts.", "Review product identity, material, sizes and prices before adding them to copy."]);
    }
}
