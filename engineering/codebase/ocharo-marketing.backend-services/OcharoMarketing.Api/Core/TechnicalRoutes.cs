namespace OcharoMarketing.Api.Core;

/// <summary>
/// Names the technical routes Marketing answers without a session: health, service status and the public runtime
/// settings.
/// </summary>
public static class TechnicalRoutes
{
    /// <summary>The technical paths, without a trailing slash.</summary>
    private static readonly HashSet<string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/api/system/status",
        "/api/runtime-config",
    };

    /// <summary>True for a GET or HEAD of a technical route, in any letter case, with or without a trailing
    /// slash.</summary>
    public static bool IsPublic(HttpRequest request)
    {
        return (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
            && Paths.Contains((request.Path.Value ?? "/").TrimEnd('/'));
    }
}
