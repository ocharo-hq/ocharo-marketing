namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Configures WhatsApp business-phone access and explicit messaging opt-in.</summary>
public sealed class WhatsAppProviderOptions
{
    public bool Enabled { get; set; }
    public string? AccessToken { get; set; }
    public string? PhoneNumberId { get; set; }
    public string? GraphVersion { get; set; }
}
