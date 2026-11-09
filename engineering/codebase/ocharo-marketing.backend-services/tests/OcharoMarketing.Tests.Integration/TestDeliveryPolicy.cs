using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OcharoMarketing.Infrastructure.Core;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using OcharoMarketing.Infrastructure.Providers;

namespace OcharoMarketing.Tests.Integration;

/// <summary>Builds a <see cref="DeliveryPolicy"/> whose live switches read a test configuration on every call, as the
/// host's option monitors do once a settings change reloads.</summary>
internal static class TestDeliveryPolicy
{
    /// <summary>A policy reading <paramref name="configuration"/> live, with the identities the brokers
    /// loaded.</summary>
    public static DeliveryPolicy Create(
        IConfiguration configuration, TelegramProviderOptions telegram, XProviderOptions x) =>
        new(Live<MarketingOptions>(configuration, MarketingOptions.SectionName),
            Live<TelegramProviderOptions>(configuration, "Providers:Telegram"),
            Live<XProviderOptions>(configuration, "Providers:X"),
            telegram,
            x);

    private static LiveMonitor<T> Live<T>(IConfiguration configuration, string section)
        where T : class, new() => new(configuration.GetSection(section));

    /// <summary>Binds the section afresh on every read.</summary>
    private sealed class LiveMonitor<T>(IConfiguration section) : IOptionsMonitor<T>
        where T : class, new()
    {
        public T CurrentValue => section.Get<T>() ?? new T();

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
