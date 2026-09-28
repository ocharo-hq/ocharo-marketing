using System.Net.Http.Json;
using System.Text.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Integrates OpenAI image generation and reference-image edits.</summary>
/// <remarks>Ambiguous paid requests require reconciliation; this broker never repeats them automatically.</remarks>
public sealed class OpenAiImageGenerationBroker(HttpClient client, OpenAiProviderOptions options) : IImageGenerationBroker
{
    public string Provider => "openai";

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest input, CancellationToken cancellationToken)
    {
        var model = string.IsNullOrWhiteSpace(input.Model) ? options.ImageModel : input.Model;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey))
            return Result(BrokerOutcome.Rejected, "not_enabled", "OpenAI image execution requires explicit enablement and credentials.", model);
        if (!ProviderInput.PromptIsValid(input.Prompt) || !ProviderInput.ImagesAreValid(input.References) ||
            input.Quality is not ("low" or "medium" or "high" or "auto") || !ValidSize(input.Size))
            return Result(BrokerOutcome.Rejected, "invalid_input", "Prompt, references, quality, or image size is invalid.", model);

        var editing = input.References is { Count: > 0 };
        var payload = new Dictionary<string, object?>
        {
            ["model"] = model, ["prompt"] = input.Prompt, ["size"] = input.Size,
            ["quality"] = input.Quality, ["output_format"] = "png"
        };
        if (input.TransparentBackground) payload["background"] = "transparent";
        if (editing)
            payload["images"] = input.References!.Select(image => new { image_url = ProviderInput.AsDataUrl(image) }).ToArray();
        else payload["n"] = 1;

        using var request = ProviderHttpTransport.Create(HttpMethod.Post,
            "https://api.openai.com/v1/images/" + (editing ? "edits" : "generations"), options.ApiKey);
        request.Content = JsonContent.Create(payload);
        var reply = await ProviderHttpTransport.SendAsync(client, request, true, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded)
            return Result(reply.Outcome, reply.Code, reply.Message, model);

        try
        {
            var images = new List<GeneratedImage>();
            if (reply.Json is { ValueKind: JsonValueKind.Object } root && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                foreach (var entry in data.EnumerateArray())
                {
                    var encoded = ProviderHttpTransport.Text(entry, "b64_json");
                    if (!string.IsNullOrWhiteSpace(encoded))
                    {
                        var bytes = Convert.FromBase64String(encoded);
                        if (!ProviderInput.IsGeneratedImage(bytes, "image/png"))
                            return Result(BrokerOutcome.Ambiguous, "invalid_image", "Provider returned an unreadable image.", model);
                        images.Add(new(bytes, "image/png"));
                    }
                }
            if (images.Count == 0)
                return Result(BrokerOutcome.Ambiguous, "missing_image", "Provider accepted the request without a usable image.", model);
            return new(BrokerOutcome.Succeeded, Provider, images, "generated", "Generated images require product review.",
                reply.RequestId is null ? null : new(Provider, "request", reply.RequestId), model);
        }
        catch (FormatException)
        {
            return Result(BrokerOutcome.Ambiguous, "invalid_image", "Provider returned an unreadable image.", model);
        }
    }

    private ImageGenerationResult Result(BrokerOutcome outcome, string code, string message, string? model)
        => new(outcome, Provider, [], code, message, Model: model);

    private static bool ValidSize(string size)
    {
        if (size == "auto") return true;
        var parts = size.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
            return false;
        var pixels = (long)width * height;
        return width is > 0 and <= 3840 && height is > 0 and <= 3840 &&
               width % 16 == 0 && height % 16 == 0 && pixels is >= 655360 and <= 8294400 &&
               (double)width / height is >= (1d / 3) and <= 3;
    }
}
