namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Draft data for the local marketing workflow.</summary>
public sealed record DraftModel(string Id, string Title, string ProductId, string PresetId, string Locale, string Caption, string[] Channels, string[] MediaIds, int Revision, int? ApprovedRevision, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, CardRecipeModel? Composition = null);
