namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Card data for the local marketing workflow.</summary>
public sealed record CardInput(string ProductId, string PresetId, string Title, string? Subtitle = null, string Locale = "en", string? FabricAssetId = null, double? Scale = null, double? OffsetX = null, double? OffsetY = null);
