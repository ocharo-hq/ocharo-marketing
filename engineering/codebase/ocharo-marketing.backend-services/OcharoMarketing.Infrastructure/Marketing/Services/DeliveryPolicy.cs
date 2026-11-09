using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Providers;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Reads live kill switches while binding operations to server-owned credential identities.</summary>
/// <remarks>The switches and identities in the monitors follow a settings reload; <paramref name="telegram"/>
/// and <paramref name="x"/> stay as the brokers loaded them, so a reloaded identity holds delivery until a
/// restart.</remarks>
/// <param name="marketing">The live <c>Marketing</c> switches and limits.</param>
/// <param name="liveTelegram">The live <c>Providers:Telegram</c> section.</param>
/// <param name="liveX">The live <c>Providers:X</c> section.</param>
/// <param name="telegram">The Telegram identity the brokers loaded at boot.</param>
/// <param name="x">The X identity the brokers loaded at boot.</param>
public sealed class DeliveryPolicy(
    IOptionsMonitor<MarketingOptions> marketing,
    IOptionsMonitor<TelegramProviderOptions> liveTelegram,
    IOptionsMonitor<XProviderOptions> liveX,
    TelegramProviderOptions telegram,
    XProviderOptions x)
{
    public DeliverySettingsModel Settings => new(marketing.CurrentValue.EnableLivePublishing,
        marketing.CurrentValue.EnableMetricReads, marketing.CurrentValue.EnableAccountVerification);
    public int MaximumMetricReadsPerHour => marketing.CurrentValue.MaximumMetricReadsPerHour;
    public int MetricCooldownSeconds => marketing.CurrentValue.MetricRefreshCooldownSeconds;
    public ConfiguredDelivery? Destination(string provider)
    {
        var revision = marketing.CurrentValue.ConnectionRevisions.GetValueOrDefault(provider, 1);
        var nowTelegram = liveTelegram.CurrentValue;
        var nowX = liveX.CurrentValue;
        var configured = provider switch
        {
            "telegram" => Make(provider, telegram.BotId, telegram.DestinationChatId, telegram.BotToken,
                telegram.Enabled && nowTelegram.Enabled, false, revision),
            "x" => Make(provider, x.UserId, x.UserId, x.AccessToken, x.Enabled && nowX.Enabled,
                x.EnableMetricReads && nowX.EnableMetricReads, revision),
            _ => null
        };
        if (configured is null) return null;
        var matches = provider == "telegram"
            ? Matches(nowTelegram.BotId, telegram.BotId)
                && Matches(nowTelegram.DestinationChatId, telegram.DestinationChatId)
                && Matches(nowTelegram.BotToken, telegram.BotToken)
            : Matches(nowX.UserId, x.UserId) && Matches(nowX.AccessToken, x.AccessToken);
        return matches ? configured : configured with { Configured = false, ReloadRequired = true };
    }
    private static bool Matches(string? supplied, string? loaded) => supplied is null || supplied == loaded;
    private static ConfiguredDelivery Make(string provider, string? account, string? destination, string? secret, bool publish, bool metrics, int revision)
    {
        var configured = !string.IsNullOrWhiteSpace(account) && !string.IsNullOrWhiteSpace(destination) && !string.IsNullOrWhiteSpace(secret);
        // This internal stamp is never returned through API DTOs or logs.
        var stamp = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", provider, account, destination, secret, revision))));
        return new(provider, account ?? "", destination ?? "", configured, publish, metrics, stamp);
    }
}

/// <summary>Holds internal configured identity guards; never serialize this record in API responses.</summary>
public sealed record ConfiguredDelivery(string Provider, string AccountId, string DestinationId, bool Configured, bool PublishEnabled, bool MetricReadsEnabled, string ConfigurationStamp, bool ReloadRequired = false);
