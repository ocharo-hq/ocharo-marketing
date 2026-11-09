
namespace OcharoMarketing.Application.Posts.Models;

/// <summary>Represents the draft a Studio review export became.</summary>
/// <param name="Draft">The draft; none when an earlier import's draft is gone.</param>
/// <param name="Reused">Whether the export was imported before, so its first draft answers.</param>
public sealed record CreativeImportModel(DraftModel? Draft, bool Reused);
