using System.Net.Http.Json;
using System.Text.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Integrates Gemini Nano Banana image interactions with supplied references.</summary>
/// <remarks>Generated scenes require review; transparent output and unverified background execution are unsupported.</remarks>
public sealed class GeminiImageGenerationBroker(HttpClient client, GeminiProviderOptions options) : IImageGenerationBroker
{
    public string Provider => "gemini";

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest input, CancellationToken cancellationToken)
    {
        var model = string.IsNullOrWhiteSpace(input.Model) ? options.ImageModel : input.Model;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey))
            return Result(BrokerOutcome.Rejected, "not_enabled", "Gemini execution requires explicit enablement and credentials.", model);
        if (!ProviderInput.PromptIsValid(input.Prompt) || !ProviderInput.ImagesAreValid(input.References) ||
            input.AspectRatio is not ("1:1" or "2:3" or "3:2" or "3:4" or "4:3" or "4:5" or "5:4" or "9:16" or "16:9" or "21:9"))
            return Result(BrokerOutcome.Rejected, "invalid_input", "Prompt, references, or aspect ratio is invalid.", model);
        if (input.TransparentBackground)
            return Result(BrokerOutcome.Unsupported, "transparency_unverified", "This adapter does not guarantee transparent Gemini output.", model);

        var content = new List<object>();
        if (input.References is not null)
            content.AddRange(input.References.Select(image => (object)new
                { type = "image", mime_type = image.ContentType, data = Convert.ToBase64String(image.Data) }));
        content.Add(new { type = "text", text = input.Prompt });
        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://generativelanguage.googleapis.com/v1beta/interactions");
        request.Headers.Add("x-goog-api-key", options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model, input = content,
            response_format = new { type = "image", mime_type = "image/png", aspect_ratio = input.AspectRatio, image_size = "2K" }
        });
        var reply = await ProviderHttpTransport.SendAsync(client, request, true, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded) return Result(reply.Outcome, reply.Code, reply.Message, model);
        if (reply.Json is not { } root)
            return Result(BrokerOutcome.Ambiguous, "missing_response", "Provider response has no usable interaction.", model);

        var id = ProviderHttpTransport.Id(root, "id");
        var receipt = id is null ? null : new ProviderReceipt(Provider, "interaction", id);
        if (receipt is not null && input.SaveCheckpointAsync is not null)
        {
            try { await input.SaveCheckpointAsync(receipt, cancellationToken); }
            catch (Exception) { return new(BrokerOutcome.Ambiguous, Provider, [], "checkpoint_failed",
                "Generated interaction could not be checkpointed.", receipt, model); }
        }

        try
        {
            var status = ProviderHttpTransport.Text(root, "status");
            if (status is "failed" or "cancelled")
                return new(BrokerOutcome.Rejected, Provider, [], "interaction_failed", "Gemini interaction did not complete.", receipt, model);
            if (status == "in_progress")
                return new(receipt is null ? BrokerOutcome.Ambiguous : BrokerOutcome.Pending, Provider, [],
                    "interaction_processing", "Gemini interaction is still processing.", receipt, model);
            var images = new List<GeneratedImage>();
            ReadImages(root, images);
            if (images.Count > 0 && status == "completed")
                return new(BrokerOutcome.Succeeded, Provider, images, "generated",
                    "Generated images require product review.", receipt, model);
            return new(status == "in_progress" && receipt is not null ? BrokerOutcome.Pending : BrokerOutcome.Ambiguous,
                Provider, [], "missing_image", "Interaction has no completed image artifact.", receipt, model);
        }
        catch (FormatException)
        {
            return new(BrokerOutcome.Ambiguous, Provider, [], "invalid_image", "Provider image is unreadable.", receipt, model);
        }
    }

    private static void ReadImages(JsonElement interaction, List<GeneratedImage> images)
    {
        if (interaction.ValueKind != JsonValueKind.Object ||
            !interaction.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array) return;
        foreach (var step in steps.EnumerateArray())
        {
            if (ProviderHttpTransport.Text(step, "type") != "model_output" ||
                !step.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var block in content.EnumerateArray())
            {
                if (ProviderHttpTransport.Text(block, "type") != "image" ||
                    ProviderHttpTransport.Text(block, "data") is not { Length: > 0 } encoded) continue;
                var mime = ProviderHttpTransport.Text(block, "mime_type");
                var contentType = ProviderInput.IsImage(mime) ? mime! : "image/png";
                var bytes = Convert.FromBase64String(encoded);
                if (!ProviderInput.IsGeneratedImage(bytes, contentType)) throw new FormatException("Invalid image signature.");
                images.Add(new(bytes, contentType));
            }
        }
    }

    private ImageGenerationResult Result(BrokerOutcome outcome, string code, string message, string? model)
        => new(outcome, Provider, [], code, message, Model: model);
}
