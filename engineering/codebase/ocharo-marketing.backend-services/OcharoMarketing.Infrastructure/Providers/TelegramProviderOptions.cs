namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Configures explicitly enabled Telegram access with verified numeric account and destination bindings.</summary>
public sealed class TelegramProviderOptions
{
    public bool Enabled { get; set; }
    public string? BotToken { get; set; }
    public string? BotId { get; set; }
    public string? DestinationChatId { get; set; }
}
