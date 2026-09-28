namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Configures OpenAI access without enabling paid execution implicitly.</summary>
public sealed class OpenAiProviderOptions
{
    public bool Enabled { get; set; }
    public string? ApiKey { get; set; }
    public string ImageModel { get; set; } = "gpt-image-2.5-flare";
    public string TextModel { get; set; } = "gpt-6-sol";
}
