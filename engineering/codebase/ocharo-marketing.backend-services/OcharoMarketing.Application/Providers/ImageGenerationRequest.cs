namespace OcharoMarketing.Application.Providers;

/// <summary>Requests an image or an edit using supplied image references.</summary>
public sealed record ImageGenerationRequest(
    string Prompt,
    IReadOnlyList<ImageReference>? References = null,
    string? Model = null,
    string Size = "1024x1280",
    string AspectRatio = "4:5",
    string Quality = "low",
    bool TransparentBackground = false,
    Func<ProviderReceipt, CancellationToken, Task>? SaveCheckpointAsync = null);
