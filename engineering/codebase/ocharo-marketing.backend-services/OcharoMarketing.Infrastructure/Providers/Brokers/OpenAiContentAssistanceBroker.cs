using System.Net.Http.Json;
using System.Text.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Integrates OpenAI Responses text and vision suggestions.</summary>
/// <remarks>Suggested copy and image observations remain unapproved until the caller reviews them.</remarks>
public sealed class OpenAiContentAssistanceBroker(HttpClient client, OpenAiProviderOptions options) : IContentAssistanceBroker
{
    public string Provider => "openai";

    public async Task<ContentAssistanceResult> AssistAsync(ContentAssistanceRequest input, CancellationToken cancellationToken)
    {
        var model = string.IsNullOrWhiteSpace(input.Model) ? options.TextModel : input.Model;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ApiKey))
            return Result(BrokerOutcome.Rejected, "not_enabled", "OpenAI assistance requires explicit enablement and credentials.", model);
        if (!ProviderInput.PromptIsValid(input.Prompt) || !ProviderInput.ImagesAreValid(input.Images) ||
            input.Operation.Length > 80 || input.Locale.Length > 20)
            return Result(BrokerOutcome.Rejected, "invalid_input", "Assistance inputs are invalid.", model);

        var content = new List<object> { new { type = "input_text", text = input.Prompt } };
        if (input.Images is not null)
            content.AddRange(input.Images.Select(image => (object)new
                { type = "input_image", image_url = ProviderInput.AsDataUrl(image) }));
        using var request = ProviderHttpTransport.Create(HttpMethod.Post, "https://api.openai.com/v1/responses", options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model,
            instructions = "Return suggestions for review. Never invent product facts, prices, materials, or photographed provenance. " +
                "Requested operation: " + input.Operation + ". Requested locale: " + input.Locale + ".",
            input = new[] { new { role = "user", content } },
            max_output_tokens = 2000
        });
        var reply = await ProviderHttpTransport.SendAsync(client, request, true, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded) return Result(reply.Outcome, reply.Code, reply.Message, model);
        if (reply.Json is not { } root || root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return Result(BrokerOutcome.Ambiguous, "missing_text", "Provider accepted the request without usable text.", model);

        var text = new List<string>();
        foreach (var item in output.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("content", out var parts) &&
                parts.ValueKind == JsonValueKind.Array)
                foreach (var part in parts.EnumerateArray())
                    if (ProviderHttpTransport.Text(part, "type") == "output_text" &&
                        ProviderHttpTransport.Text(part, "text") is { Length: > 0 } value)
                        text.Add(value);
        if (text.Count == 0)
            return Result(BrokerOutcome.Rejected, "no_suggestion", "Provider did not return a text suggestion.", model);
        var id = ProviderHttpTransport.Id(root, "id");
        return new(BrokerOutcome.Succeeded, Provider, string.Join("\n", text), "suggested",
            "Suggestion requires review.", model, id is null ? null : new(Provider, "response", id));
    }

    private ContentAssistanceResult Result(BrokerOutcome outcome, string code, string message, string? model)
        => new(outcome, Provider, null, code, message, model);
}
