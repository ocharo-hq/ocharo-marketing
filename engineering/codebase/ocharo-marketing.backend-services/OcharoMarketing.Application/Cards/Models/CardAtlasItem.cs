namespace OcharoMarketing.Application.Cards.Models;

/// <summary>Describes the immutable atlasitem input or output of card rendering.</summary>
public sealed record CardAtlasItem(
    string Title,
    string PresetName,
    byte[] PngBytes,
    string Notes = "");
