namespace OcharoMarketing.Application.Cards.Models;

/// <summary>Describes the immutable compositioninput input or output of card rendering.</summary>
public sealed record CardCompositionInput(
    string ProductId,
    string PresetId,
    string Title,
    string Subtitle,
    string Locale,
    CardImage HeroImage,
    CardImage? FabricImage = null,
    double Scale = 1,
    double OffsetX = 0,
    double OffsetY = 0,
    string BrandName = "OCHARO");
