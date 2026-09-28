using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Providers;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Coordinates explicitly configured provider preparation and durable generation outcomes.</summary>
public sealed class ExternalMarketingService(IEnumerable<IImageGenerationBroker> imageBrokers,
    IEnumerable<IContentAssistanceBroker> textBrokers, IMarketingRepository repository, IAssetService assets,
    OpenAiProviderOptions openAi, GeminiProviderOptions gemini, TelegramProviderOptions telegram,
    XProviderOptions x, InstagramProviderOptions instagram, WhatsAppProviderOptions whatsApp) : IExternalMarketingService
{
    public IReadOnlyList<ConnectionModel> GetConnections() => [
        new("openai", "OpenAI", "image", Ready(openAi.Enabled, openAi.ApiKey), ["image-generation", "text-assistance"], "Configure Providers:OpenAi server-side and explicitly enable it.", "https://platform.openai.com/docs/guides/image-generation"),
        new("gemini", "Gemini", "image", Ready(gemini.Enabled, gemini.ApiKey), ["image-generation"], "Configure Providers:Gemini server-side and explicitly enable it.", "https://ai.google.dev/gemini-api/docs/image-generation"),
        new("instagram", "Instagram", "social", Ready(instagram.Enabled, instagram.AccessToken, instagram.GraphVersion), ["image-publishing"], "Professional account credentials and GraphVersion are required. Delivery remains manual in this pilot.", "https://developers.facebook.com/docs/instagram-platform/content-publishing/"),
        new("telegram", "Telegram", "social", Ready(telegram.Enabled, telegram.BotToken), ["text-publishing", "image-publishing"], "Configure a bot with destination permission. Delivery remains manual in this pilot.", "https://core.telegram.org/bots/api"),
        new("whatsapp", "WhatsApp Business", "social", Ready(whatsApp.Enabled, whatsApp.AccessToken, whatsApp.PhoneNumberId, whatsApp.GraphVersion), ["business-messaging"], "Business phone, consent, template/window rules apply. Delivery remains manual in this pilot.", "https://developers.facebook.com/docs/whatsapp/cloud-api/"),
        new("x", "X", "social", Ready(x.Enabled, x.AccessToken, x.UserId), ["text-publishing", "image-publishing"], "Configure authorized user access. Delivery remains manual in this pilot.", "https://docs.x.com/x-api/posts/create-post")
    ];

    public bool CanGenerate(string provider) => provider switch
    {
        "openai" => Ready(openAi.Enabled, openAi.ApiKey) == "configured",
        "gemini" => Ready(gemini.Enabled, gemini.ApiKey) == "configured",
        _ => false
    };

    public async Task<object> AssistAsync(string prompt, string operation, string locale, CancellationToken ct)
    {
        if (!CanGenerate("openai")) throw new MarketingException("Text assistance is not enabled and configured.", 409);
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 16000) throw new MarketingException("Provide a prompt under 16000 characters.");
        var broker = textBrokers.Single(item => item.Provider == "openai");
        var result = await broker.AssistAsync(new ContentAssistanceRequest(prompt, operation, locale), ct);
        if (result.Outcome != BrokerOutcome.Succeeded || result.Text is null)
            throw new MarketingException(result.Message, result.Outcome == BrokerOutcome.Unsupported ? 422 : 502);
        return new { text = result.Text, provider = result.Provider, model = result.Model };
    }

    public async Task ExecuteGenerationAsync(JobClaim claim, CancellationToken ct)
    {
        var request = MarketingJson.Read<ImageGenerationInput>(claim.PayloadJson);
        if (!CanGenerate(request.Provider))
        {
            await repository.FailAsync(claim, "The provider was disabled before this job started.", false, ct);
            return;
        }
        var references = new List<ImageReference>();
        foreach (var id in request.ReferenceAssetIds ?? [])
        {
            var asset = await assets.ReadAsync(id, ct);
            references.Add(new ImageReference(asset.Bytes, asset.Asset.ContentType, asset.Asset.FileName));
        }
        var broker = imageBrokers.Single(item => item.Provider == request.Provider);
        var result = await broker.GenerateAsync(new ImageGenerationRequest(request.Prompt, references, request.Model,
            SaveCheckpointAsync: (receipt, token) => repository.CheckpointAsync(claim, MarketingJson.Write(receipt), token)), ct);
        if (result.Outcome != BrokerOutcome.Succeeded || result.Images.Count == 0)
        {
            await repository.FailAsync(claim, result.Message, result.Outcome is BrokerOutcome.Ambiguous or BrokerOutcome.Pending, ct);
            return;
        }
        var outputIds = new List<string>();
        for (var index = 0; index < result.Images.Count; index++)
        {
            var image = result.Images[index];
            var extension = image.ContentType == "image/jpeg" ? "jpg" : image.ContentType == "image/webp" ? "webp" : "png";
            var asset = await assets.SaveAsync(image.Data, $"{request.Provider}-{claim.Id}-{index + 1}.{extension}",
                $"Generated by {result.Provider}; model {result.Model ?? "provider default"}; job {claim.Id}. Requires product-fidelity review.",
                "generated", ct, $"image-{claim.Id}-{index + 1}");
            outputIds.Add(asset.Id);
        }
        await repository.CompleteAsync(claim, outputIds.ToArray(), ct);
    }

    private static string Ready(bool enabled, params string?[] settings) =>
        enabled && settings.All(value => !string.IsNullOrWhiteSpace(value)) ? "configured" : "not-configured";
}
