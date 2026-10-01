namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Blog host options (section <c>Blog</c>).</summary>
public sealed class BlogOptions
{
    public const string Section = "Blog";

    public string? PlatformBaseUrl { get; set; }
    public string BrandSlug { get; set; } = "ocharo";

    /// <summary>Explicit HTTPS entry to the catalogue operator app; absent by default and excluded from static exports.</summary>
    public string? CatalogueAdminUrl { get; set; }

    /// <summary>Runtime data root for blobs; relative paths resolve against the content root.</summary>
    public string DataRoot { get; set; } = "App_Data";

    /// <summary>Seed JSON (topics + stories) imported into an empty blog.</summary>
    public string? SeedFile { get; set; } = "SeedData/journal-seed.json";

    /// <summary>Folder with the approved journal images named in the seed (read-only; hashes recorded).</summary>
    public string? SeedAssetRoot { get; set; }

    /// <summary>Ocharo Catalogue origin for the public feed, e.g. https://localhost:8278.</summary>
    public string? CatalogueBaseUrl { get; set; }

    /// <summary>Ocharo Brand origin; when reachable its logo and links replace the blog's own, e.g. http://localhost:8287.</summary>
    public string? BrandBaseUrl { get; set; }

    /// <summary>How often scheduled stories are checked.</summary>
    public int SchedulerSeconds { get; set; } = 30;

    public long MaximumImageBytes { get; set; } = 30 * 1024 * 1024;
    public int PreviewSize { get; set; } = 1400;
}
