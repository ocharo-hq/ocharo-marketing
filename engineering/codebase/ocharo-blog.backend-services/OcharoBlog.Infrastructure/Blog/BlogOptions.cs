namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Blog host options (section <c>Blog</c>).</summary>
public sealed class BlogOptions
{
    public const string Section = "Blog";

    /// <summary>The fewest seconds between scheduled-publishing passes; <c>0</c> turns the passes off.</summary>
    public const int MinimumSchedulerSeconds = 5;

    /// <summary>The Identity organization this Editorial instance belongs to; only its members reach the private routes. Required.</summary>
    public Guid OrganizationId { get; set; }

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

    /// <summary>Ocharo Brand origin; when reachable its logo and links replace the blog's own, e.g. https://localhost:8286.</summary>
    public string? BrandBaseUrl { get; set; }

    /// <summary>Ocharo Desk origin for background removal, e.g. https://desk.example.com/; unset or not plain HTTPS leaves removal unavailable.</summary>
    public string? DeskBaseUrl { get; set; }

    /// <summary>Ocharo Marketing origin the public enquiry relay posts to; unset or not plain HTTPS answers enquiries
    /// with 503.</summary>
    public string? MarketingBaseUrl { get; set; }

    /// <summary>Other HTTPS origins, such as <c>https://studio.example.com</c>, whose writes Editorial accepts
    /// besides its own; each is scheme, host and an optional port, with no path.</summary>
    public IList<string> TrustedOrigins { get; set; } = [];

    /// <summary>Seconds between scheduled-publishing passes: <c>0</c> turns them off, else at least
    /// <see cref="MinimumSchedulerSeconds"/>.</summary>
    public int SchedulerSeconds { get; set; } = 30;

    public long MaximumImageBytes { get; set; } = 30 * 1024 * 1024;
    public int PreviewSize { get; set; } = 1400;

    /// <summary>Whether <paramref name="value"/> is an HTTPS origin: scheme, host and an optional port.</summary>
    public static bool IsHttpsOrigin(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
        && uri.UserInfo.Length == 0 && uri.GetLeftPart(UriPartial.Authority) == value;
}
