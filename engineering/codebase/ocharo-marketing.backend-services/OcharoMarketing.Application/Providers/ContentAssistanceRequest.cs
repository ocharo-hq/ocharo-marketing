namespace OcharoMarketing.Application.Providers;

/// <summary>Requests suggested text or vision analysis without accepting it as fact.</summary>
public sealed record ContentAssistanceRequest(
    string Prompt,
    string Operation = "copy",
    string Locale = "en",
    IReadOnlyList<ImageReference>? Images = null,
    string? Model = null);
