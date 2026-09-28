namespace OcharoMarketing.Infrastructure.Providers;

/// <summary>Configures Gemini access without enabling paid execution implicitly.</summary>
public sealed class GeminiProviderOptions
{
    public bool Enabled { get; set; }
    public string? ApiKey { get; set; }
    public string ImageModel { get; set; } = "gemini-3.1-flash-image";
}
