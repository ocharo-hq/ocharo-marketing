namespace OcharoMarketing.Application.Providers;

/// <summary>Integrates an external image generation capability.</summary>
public interface IImageGenerationBroker
{
    string Provider { get; }
    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken cancellationToken);
}
