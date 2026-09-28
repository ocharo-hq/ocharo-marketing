using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Infrastructure.Providers;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Reads live kill switches while binding operations to server-owned credential identities.</summary>
public sealed class DeliveryPolicy(IConfiguration configuration, TelegramProviderOptions telegram, XProviderOptions x)
{
    public DeliverySettingsModel Settings => new(configuration.GetValue("Marketing:EnableLivePublishing", false),
        configuration.GetValue("Marketing:EnableMetricReads", false), configuration.GetValue("Marketing:EnableAccountVerification", false));
    public int MaximumMetricReadsPerHour => Math.Clamp(configuration.GetValue("Marketing:MaximumMetricReadsPerHour", 10), 0, 100);
    public int MetricCooldownSeconds => Math.Max(1, configuration.GetValue("Marketing:MetricRefreshCooldownSeconds", 60));
    public ConfiguredDelivery? Destination(string provider)
    {
        var revision = configuration.GetValue($"Marketing:ConnectionRevisions:{provider}", 1);
        var configured = provider switch
        {
            "telegram" => Make(provider, telegram.BotId, telegram.DestinationChatId, telegram.BotToken, telegram.Enabled && configuration.GetValue("Providers:Telegram:Enabled", false), false, revision),
            "x" => Make(provider, x.UserId, x.UserId, x.AccessToken, x.Enabled && configuration.GetValue("Providers:X:Enabled", false), x.EnableMetricReads && configuration.GetValue("Providers:X:EnableMetricReads", false), revision),
            _ => null
        };
        if (configured is null) return null;
        var matches = provider == "telegram"
            ? Matches("Providers:Telegram:BotId", telegram.BotId) && Matches("Providers:Telegram:DestinationChatId", telegram.DestinationChatId) && Matches("Providers:Telegram:BotToken", telegram.BotToken)
            : Matches("Providers:X:UserId", x.UserId) && Matches("Providers:X:AccessToken", x.AccessToken);
        return matches ? configured : configured with { Configured = false, ReloadRequired = true };
    }
    private bool Matches(string key, string? loaded) => configuration[key] is not { } supplied || supplied == loaded;
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
