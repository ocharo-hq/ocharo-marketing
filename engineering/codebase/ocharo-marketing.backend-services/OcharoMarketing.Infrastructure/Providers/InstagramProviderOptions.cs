namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Configures Instagram professional-account access and explicit publication opt-in.</summary>
public sealed class InstagramProviderOptions
{
    public bool Enabled { get; set; }
    public string? AccessToken { get; set; }
    public string? GraphVersion { get; set; }
    public bool UseFacebookLogin { get; set; }
}
