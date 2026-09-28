namespace OcharoMarketing.Infrastructure.Cards.Settings;

/// <summary>Configures the pinned browser renderer without product-specific content.</summary>
public sealed record CardRendererSettings(
    string? ExecutablePath = null,
    int TimeoutMilliseconds = 30000);
