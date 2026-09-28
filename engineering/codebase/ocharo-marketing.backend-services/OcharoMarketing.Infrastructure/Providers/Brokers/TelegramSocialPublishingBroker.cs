using System.Net.Http.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Publishes one approved Telegram message after numeric identity and durable dispatch checks.</summary>
/// <remarks>Receipts prove provider acceptance in the matched channel; they never establish reach.</remarks>
public sealed class TelegramSocialPublishingBroker(HttpClient client, TelegramProviderOptions options) : ISocialPublishingBroker
{
    public string Provider => "telegram";
    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string> { "text", "photo" };

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest input, CancellationToken cancellationToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.BotToken))
            return Result(BrokerOutcome.Rejected, "not_enabled", "Telegram publication requires explicit enablement and credentials.");
        if (!ProviderInput.IsTelegramAccountId(options.BotId) || input.AccountId != options.BotId
            || !ProviderInput.IsTelegramChatId(options.DestinationChatId) || !options.DestinationChatId!.StartsWith('-')
            || input.DestinationId != options.DestinationChatId)
            return Result(BrokerOutcome.Rejected, "identity_mismatch", "Use the verified numeric bot and configured channel destination.");
        if (string.IsNullOrWhiteSpace(input.Text) || input.Template is not null)
            return Result(BrokerOutcome.Rejected, "invalid_input", "Approved text or requested messaging format is invalid.");
        if (input.ResumeReceipt is not null)
            return Result(BrokerOutcome.Unsupported, "reconcile_required", "An existing Telegram dispatch receipt requires reconciliation before another send.");
        if (input.Media is { Count: > 1 })
            return Result(BrokerOutcome.Unsupported, "album_not_supported", "This adapter publishes one photo per request.");

        var photo = input.Media?.FirstOrDefault();
        if (input.Text.Length > (photo is null ? 4096 : 1024))
            return Result(BrokerOutcome.Rejected, "text_too_long", "Telegram text or photo caption exceeds the supported limit.");
        if (photo is not null && (!ProviderInput.IsImage(photo.ContentType) || photo.Data.Length is 0 or > 10_485_760))
            return Result(BrokerOutcome.Rejected, "invalid_photo", "Telegram photo data is invalid or exceeds ten megabytes.");
        if (cancellationToken.IsCancellationRequested)
            return Result(BrokerOutcome.Rejected, "cancelled_before_send", "Operation cancelled before dispatch.");
        if (input.SaveCheckpointAsync is null)
            return Result(BrokerOutcome.Rejected, "checkpoint_required", "Persist Telegram dispatch before sending.");

        var dispatch = new ProviderReceipt(Provider, "publication_started", input.DestinationId,
            new Dictionary<string, string> { ["account_id"] = input.AccountId!, ["chat_id"] = input.DestinationId });
        try { await input.SaveCheckpointAsync(dispatch, cancellationToken); }
        catch (Exception) { return new(BrokerOutcome.Rejected, Provider, "checkpoint_failed",
            "Dispatch checkpoint failed; Telegram publication was not attempted.", dispatch); }

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.telegram.org/bot" + options.BotToken + "/" + (photo is null ? "sendMessage" : "sendPhoto"));
        if (photo is null)
            request.Content = JsonContent.Create(new { chat_id = input.DestinationId, text = input.Text });
        else
        {
            var body = new MultipartFormDataContent();
            body.Add(new StringContent(input.DestinationId), "chat_id");
            body.Add(new StringContent(input.Text), "caption");
            var media = new ByteArrayContent(photo.Data);
            media.Headers.ContentType = new(photo.ContentType);
            body.Add(media, "photo", "card" + (photo.ContentType == "image/jpeg" ? ".jpg" : ".png"));
            request.Content = body;
        }
        var reply = await ProviderHttpTransport.SendAsync(client, request, true, cancellationToken);
        if (reply.Outcome != BrokerOutcome.Succeeded)
            return new(reply.Outcome, Provider, reply.Code, reply.Message,
                reply.Outcome == BrokerOutcome.Ambiguous ? dispatch with { Stage = "publication_unknown" } : dispatch);
        if (reply.Json is { ValueKind: System.Text.Json.JsonValueKind.Object } rejected && rejected.TryGetProperty("ok", out var ok)
            && ok.ValueKind == System.Text.Json.JsonValueKind.False)
            return new(BrokerOutcome.Rejected, Provider, "provider_rejected", "Telegram did not accept the message.", dispatch);
        if (!ProviderReadEvidence.TryTelegramResult(reply.Json, out var result)
            || ProviderHttpTransport.Id(result, "message_id") is not { } id || !ProviderInput.IsTelegramAccountId(id)
            || !ProviderReadEvidence.TryObject(result, "chat", out var chat)
            || ProviderHttpTransport.Id(chat, "id") is not { } chatId || !ProviderInput.IsTelegramChatId(chatId))
            return new(BrokerOutcome.Ambiguous, Provider, "missing_receipt",
                "Telegram response did not establish a usable message and channel receipt.", dispatch with { Stage = "publication_unknown" });
        var related = new Dictionary<string, string>
        {
            ["account_id"] = input.AccountId!, ["chat_id"] = chatId, ["expected_chat_id"] = input.DestinationId
        };
        if (chatId != input.DestinationId)
            return new(BrokerOutcome.Ambiguous, Provider, "destination_mismatch",
                "Telegram returned a message receipt for a different channel; reconcile before retrying.",
                new(Provider, "publication_unknown", id, related));
        return new(BrokerOutcome.Succeeded, Provider, "published", "Telegram message accepted in the selected channel.",
            new(Provider, "published", id, related));
    }

    private SocialPublishResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
