using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Verifies the bot and its channel posting permission through read-only Bot API calls.</summary>
public sealed class TelegramAccountVerificationBroker(
    HttpClient client, TelegramProviderOptions options, TimeProvider? timeProvider = null) : IProviderVerificationBroker
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    public string Provider => "telegram";

    public async Task<ProviderVerificationResult> VerifyAsync(ProviderVerificationRequest input, CancellationToken cancellationToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.BotToken))
            return Result(BrokerOutcome.Rejected, "not_enabled", "Telegram verification requires explicit enablement and credentials.");
        if (!ProviderInput.IsTelegramChatId(input.DestinationId) && !ProviderInput.IsTelegramUsername(input.DestinationId)
            || input.ExpectedAccountId is not null && !ProviderInput.IsTelegramAccountId(input.ExpectedAccountId))
            return Result(BrokerOutcome.Rejected, "invalid_identity", "Choose a numeric channel or a supported channel username.");
        if (options.BotId is not null && !ProviderInput.IsTelegramAccountId(options.BotId)
            || options.DestinationChatId is not null && !ProviderInput.IsTelegramChatId(options.DestinationChatId))
            return Result(BrokerOutcome.Rejected, "invalid_configuration", "Configured Telegram identities must be numeric.");
        if (options.BotId is not null && input.ExpectedAccountId is not null && options.BotId != input.ExpectedAccountId)
            return Result(BrokerOutcome.Rejected, "account_mismatch", "Requested Telegram account differs from configured identity.");

        var baseUri = "https://api.telegram.org/bot" + options.BotToken + "/";
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, baseUri + "getMe");
        var meReply = await ProviderHttpTransport.SendAsync(client, meRequest, false, cancellationToken);
        if (meReply.Outcome != BrokerOutcome.Succeeded)
            return Result(meReply.Outcome, meReply.Code, meReply.Message);
        if (!ProviderReadEvidence.TryTelegramResult(meReply.Json, out var me)
            || !ProviderReadEvidence.IsTrue(me, "is_bot")
            || ProviderHttpTransport.Id(me, "id") is not { } accountId || !ProviderInput.IsTelegramAccountId(accountId))
            return Result(BrokerOutcome.Rejected, "identity_unavailable", "Telegram did not return a usable bot identity.");
        if (input.ExpectedAccountId is not null && input.ExpectedAccountId != accountId
            || options.BotId is not null && options.BotId != accountId)
            return Result(BrokerOutcome.Rejected, "account_mismatch", "Telegram credentials identify a different bot.");

        using var chatRequest = new HttpRequestMessage(HttpMethod.Get,
            baseUri + "getChat?chat_id=" + Uri.EscapeDataString(input.DestinationId));
        var chatReply = await ProviderHttpTransport.SendAsync(client, chatRequest, false, cancellationToken);
        if (chatReply.Outcome != BrokerOutcome.Succeeded)
            return Result(chatReply.Outcome, chatReply.Code, chatReply.Message);
        if (!ProviderReadEvidence.TryTelegramResult(chatReply.Json, out var chat)
            || ProviderHttpTransport.Id(chat, "id") is not { } chatId || !ProviderInput.IsTelegramChatId(chatId))
            return Result(BrokerOutcome.Rejected, "destination_unavailable", "Telegram did not return a numeric destination.");
        if (ProviderHttpTransport.Text(chat, "type") != "channel" || !chatId.StartsWith("-", StringComparison.Ordinal))
            return Result(BrokerOutcome.Unsupported, "channel_required", "This verification path supports channel publication.");
        if (ProviderInput.IsTelegramChatId(input.DestinationId) && chatId != input.DestinationId
            || options.DestinationChatId is not null && chatId != options.DestinationChatId
            || input.DestinationId.StartsWith('@') && !string.Equals(ProviderHttpTransport.Text(chat, "username"),
                input.DestinationId[1..], StringComparison.OrdinalIgnoreCase))
            return Result(BrokerOutcome.Rejected, "destination_mismatch", "Telegram resolved a different channel identity.");

        using var memberRequest = new HttpRequestMessage(HttpMethod.Get,
            baseUri + "getChatMember?chat_id=" + Uri.EscapeDataString(chatId) + "&user_id=" + accountId);
        var memberReply = await ProviderHttpTransport.SendAsync(client, memberRequest, false, cancellationToken);
        if (memberReply.Outcome != BrokerOutcome.Succeeded)
            return Result(memberReply.Outcome, memberReply.Code, memberReply.Message);
        if (!ProviderReadEvidence.TryTelegramResult(memberReply.Json, out var member)
            || !ProviderReadEvidence.TryObject(member, "user", out var memberUser)
            || ProviderHttpTransport.Id(memberUser, "id") != accountId
            || !ProviderReadEvidence.IsTrue(memberUser, "is_bot"))
            return Result(BrokerOutcome.Rejected, "member_mismatch", "Telegram did not confirm the selected bot's channel membership.");
        var status = ProviderHttpTransport.Text(member, "status");
        if (status != "creator" && (status != "administrator" || !ProviderReadEvidence.IsTrue(member, "can_post_messages")))
            return Result(BrokerOutcome.Rejected, "posting_permission_missing", "The bot lacks channel posting permission.");

        return new(BrokerOutcome.Succeeded, Provider, "verified", "Bot and channel posting permission verified.",
            new(accountId, chatId, ProviderReadEvidence.DisplayName(ProviderHttpTransport.Text(chat, "title"), chatId),
                ["text", "photo"], clock.GetUtcNow()));
    }

    private ProviderVerificationResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
