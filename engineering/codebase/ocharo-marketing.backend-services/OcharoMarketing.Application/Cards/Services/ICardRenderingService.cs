using OcharoMarketing.Application.Cards.Models;

namespace OcharoMarketing.Application.Cards.Services;

/// <summary>Defines shared card composition and immutable artwork exports.</summary>
public interface ICardRenderingService
{
    IReadOnlyList<CardPreset> GetPresets();
    CardPreview Compose(CardCompositionInput input);
    Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken);
    Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken cancellationToken);
}
