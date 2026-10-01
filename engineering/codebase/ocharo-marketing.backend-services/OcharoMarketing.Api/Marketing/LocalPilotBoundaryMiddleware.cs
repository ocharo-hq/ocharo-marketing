using System.Net;

namespace OcharoMarketing.Api.Marketing;

/// <summary>Restricts the unauthenticated development pilot to local browser requests.</summary>
public sealed class LocalPilotBoundaryMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> TrustedDevelopmentOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        "https://localhost:8358",
        "https://127.0.0.1:8358",
        "https://[::1]:8358"
    };

    /// <summary>Rejects non-local requests and cross-origin state changes before application work.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        var localHost = context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
        if (!localHost || address is not null && !IPAddress.IsLoopback(address))
        {
            await RejectAsync(context, "This development pilot accepts loopback requests only.");
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsPatch(context.Request.Method) || HttpMethods.IsDelete(context.Request.Method))
        {
            var origin = context.Request.Headers.Origin.ToString();
            var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
            if (origin.Length > 0 ? !IsAllowedOrigin(origin, context.Request) : fetchSite == "cross-site")
            {
                await RejectAsync(context, "Cross-origin writes are disabled.");
                return;
            }
        }

        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.Headers.CacheControl = "no-store";
        await next(context);
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

    private static Task RejectAsync(HttpContext context, string detail) =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Local pilot boundary", detail: detail)
            .ExecuteAsync(context);
}
