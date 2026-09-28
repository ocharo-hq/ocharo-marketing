namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Draft data for the local marketing workflow.</summary>
public sealed record DraftInput(string Title, string ProductId, string PresetId, string Locale, string Caption, string[] Channels, string[]? MediaIds = null, int? ExpectedRevision = null, CardRecipeModel? Composition = null);
