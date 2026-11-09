using OcharoMarketing.Api.Access;

namespace OcharoMarketing.Api.Core;

/// <summary>
/// Sets Marketing's response headers and refuses cross-origin writes before sign-in runs: every response is
/// <c>nosniff</c>, the API, preview and media routes are <c>no-store</c>, and a write to any route, the public inquiry
/// form
/// included, must come from this host's own origin or the local Vite one. Who may call what is the Identity sign-in's
/// (<see cref="MarketingAccess"/>).
/// </summary>
public sealed class MarketingHttpBoundaryMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> TrustedDevelopmentOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        "https://localhost:8358",
        "https://127.0.0.1:8358",
        "https://[::1]:8358",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var path = (context.Request.Path.Value ?? "/").ToLowerInvariant();
        if (path == "/api" || path.StartsWith("/api/", StringComparison.Ordinal)
            || path.StartsWith("/preview", StringComparison.Ordinal)
                || path.StartsWith("/media/", StringComparison.Ordinal))
            NoStore(context);

        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
        {
            var origin = context.Request.Headers.Origin.ToString();
            var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
            if (origin.Length > 0 ? !IsAllowedOrigin(origin, context.Request) : fetchSite == "cross-site")
            {
                await Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Cross-origin write refused",
                    detail: "Cross-origin writes are disabled.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "cross_origin_write"
                    }).ExecuteAsync(context);
                return;
            }
        }

        await next(context);
    }

    private static void NoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
    }

    private static bool IsAllowedOrigin(string origin, HttpRequest request)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || uri.UserInfo.Length > 0 || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            return false;
        var authority = uri.GetLeftPart(UriPartial.Authority);
        return string.Equals(authority, $"{request.Scheme}://{request.Host.Value}", StringComparison.OrdinalIgnoreCase)
            || TrustedDevelopmentOrigins.Contains(authority);
    }
}
