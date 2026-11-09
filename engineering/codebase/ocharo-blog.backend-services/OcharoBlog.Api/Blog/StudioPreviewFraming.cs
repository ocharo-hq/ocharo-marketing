namespace OcharoBlog.Api.Blog;

/// <summary>
/// Lets the Studio frame its own previews: the SDK's secure-header preset denies every frame, so a preview answers
/// <c>X-Frame-Options</c> and the content security policy itself, allowing this origin only.
/// </summary>
public static class StudioPreviewFraming
{
    /// <summary>The legacy frame header.</summary>
    public const string FrameOptionsHeader = "X-Frame-Options";

    /// <summary>Allows frames from this origin only.</summary>
    public const string SameOrigin = "SAMEORIGIN";

    /// <summary>The content security policy header.</summary>
    public const string PolicyHeader = "Content-Security-Policy";

    /// <summary>The preset's policy with <c>frame-ancestors 'self'</c> in place of <c>'none'</c>.</summary>
    public const string Policy = "object-src 'none'; form-action 'self'; frame-ancestors 'self'";
}
