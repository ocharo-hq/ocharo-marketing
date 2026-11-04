using OcharoMarketing.Api.Marketing;

namespace OcharoMarketing.Api.Access;

/// <summary>How Marketing classifies a request, and the permissions its private routes check.</summary>
public static class MarketingAccess
{
    /// <summary>The permission every write to a private route needs.</summary>
    public const string EditPermission = "marketing:edit";

    /// <summary>The permission an approval write (<see cref="NeedsApproval"/>) needs, beside <see cref="EditPermission"/>.</summary>
    public const string ApprovePermission = "marketing:approve";

    private const string SessionRoutes = "/api/identity";

    // Automation settings can enable the background planner; fill immediately schedules approved drafts.
    private static readonly HashSet<string> ApprovalPaths = new(StringComparer.Ordinal)
    {
        "/api/marketing/automation",
        "/api/marketing/automation/fill",
    };

    private static readonly HashSet<string> ApprovalSegments = new(StringComparer.Ordinal)
    {
        "publish", "unpublish", "archive", "schedule", "approve", "restore", "queue", "retry", "dispatch", "settings", "cancel", "reconcile",
    };

    /// <summary>Whether a request never reads a session: a technical route (<see cref="TechnicalRoutes"/>) or anything
    /// under <c>/api/public/</c>, writes such as the inquiry form included.</summary>
    public static bool IsPublic(HttpRequest request) =>
        TechnicalRoutes.IsPublic(request) || Normalized(request).StartsWith("/api/public/", StringComparison.Ordinal);

    /// <summary>
    /// Whether a request needs a member's session: <c>/api</c> and <c>/preview</c>, in any letter case. Public requests and
    /// the Identity session routes do not; neither do static files and the SPA shell, which sit outside both.
    /// </summary>
    public static bool IsPrivate(HttpRequest request)
    {
        if (IsPublic(request) || request.Path.StartsWithSegments(SessionRoutes)) return false;
        var path = Normalized(request);
        return path == "/api" || path.StartsWith("/api/", StringComparison.Ordinal)
            || path == "/preview" || path.StartsWith("/preview/", StringComparison.Ordinal);
    }

    /// <summary>Whether a private request changes what goes out, or when, so it needs <see cref="ApprovePermission"/>.</summary>
    public static bool NeedsApproval(HttpRequest request)
    {
        var path = Normalized(request);
        return ApprovalPaths.Contains(path.TrimEnd('/')) || path.Split('/').Any(ApprovalSegments.Contains);
    }

    /// <summary>Whether a method may change state.</summary>
    public static bool IsWrite(string method) =>
        !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method) && !HttpMethods.IsOptions(method);

    private static string Normalized(HttpRequest request) => (request.Path.Value ?? "/").ToLowerInvariant();
}
