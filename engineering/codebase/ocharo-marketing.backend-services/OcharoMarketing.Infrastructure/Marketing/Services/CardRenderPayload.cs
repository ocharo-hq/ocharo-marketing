namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Stores the exact self-contained HTML approved for a local render request.</summary>
public sealed record CardRenderPayload(string Html, string PresetId);
