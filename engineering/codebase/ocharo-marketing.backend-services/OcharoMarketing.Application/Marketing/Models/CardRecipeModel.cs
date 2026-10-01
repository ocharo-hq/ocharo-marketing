namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Preserves the editable card composition together with its draft revision.</summary>
public sealed record CardRecipeModel(string Title, string Subtitle, string? FabricAssetId, double Scale = 1, double OffsetX = 0, double OffsetY = 0, string[]? BoundMediaIds = null);
