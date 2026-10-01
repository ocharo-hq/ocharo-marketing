namespace OcharoBlog.Domain.Blog;

/// <summary>Immutable transformation receipt, separate from deduplicated content-addressed image bytes.</summary>
public sealed class MediaEditRecord
{
    public string Id { get; set; } = "";
    public string SourceSha256 { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string RecipeJson { get; set; } = "";
    public string OrganizationId { get; set; } = "";
    public string Actor { get; set; } = "";
    public string? Model { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
