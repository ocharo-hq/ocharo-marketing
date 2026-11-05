namespace OcharoBlog.Api.Access;

/// <summary>
/// Classifies Editorial's routes for sign-in. A path here is the request path in lower case. The public API
/// (<c>/api/public/*</c>), the public site and the Studio's files answer anonymously; the rest of <c>/api</c>, previews and
/// non-public media need a session of the bound organization. <see cref="Blog.TechnicalRoutes"/> names the technical routes.
/// The live export routes take a service token instead.
/// </summary>
public static class EditorialAccess
{
    /// <summary>The scope a service token needs to read a live export.</summary>
    public const string ExportScope = "editorial.export";

    /// <summary>The setting naming Editorial's <c>SignalUrl</c> as a resource at the instance, <c>{site}/signals/identity</c>; unset maps no route.</summary>
    public const string SignalUrlKey = "Identity:Resource:SignalUrl";

    /// <summary>The route that hears Identity's revocation signals for this resource.</summary>
    public const string SignalPath = "/signals/identity";

    private const string PublicApiPrefix = "/api/public/";

    /// <summary>The live export routes, the only ones where a bearer token counts.</summary>
    private static readonly HashSet<string> ExportRoutes = new(StringComparer.Ordinal)
    {
        "/api/blog/export/site.zip", "/api/blog/export/atlas.zip", "/api/blog/export/wear.zip", "/api/blog/export/catalogue.zip",
    };

    /// <summary>The path segments that make a write an approval action, so it needs the approve permission too.</summary>
    private static readonly HashSet<string> ApprovalSegments = new(StringComparer.Ordinal)
    {
        "publish", "unpublish", "archive", "schedule", "approve", "restore", "queue", "retry", "dispatch", "settings", "cancel", "reconcile",
    };

    /// <summary>Whether a request path, in any letter case, is under the public API.</summary>
    public static bool IsPublicApi(PathString path) => (path.Value ?? "").StartsWith(PublicApiPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a path is one of the Identity BFF's session routes, which answer with or without a session.</summary>
    public static bool IsSessionRoute(string path) => path == "/api/identity" || path.StartsWith("/api/identity/", StringComparison.Ordinal);

    /// <summary>Whether a path needs a session, leaving aside <c>/media/*</c>, where the media itself decides.</summary>
    public static bool IsPrivateRoute(string path) =>
        (path == "/api" || path.StartsWith("/api/", StringComparison.Ordinal)) && !path.StartsWith(PublicApiPrefix, StringComparison.Ordinal)
        || path == "/preview" || path.StartsWith("/preview/", StringComparison.Ordinal);

    /// <summary>Whether a path names media, which is public while a published article or the site settings use it.</summary>
    public static bool IsMediaRoute(string path) => path.StartsWith("/media/", StringComparison.Ordinal);

    /// <summary>Whether a path names an approval action.</summary>
    public static bool IsApproval(string path) => path.Split('/').Any(ApprovalSegments.Contains);

    /// <summary>Whether a request offers a bearer token to read a live export.</summary>
    public static bool OffersExportToken(HttpRequest request, string path) =>
        HttpMethods.IsGet(request.Method) && ExportRoutes.Contains(path)
        && request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a method may change state.</summary>
    public static bool IsWrite(string method) => !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method);
}
