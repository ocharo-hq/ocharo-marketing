using System.Net;

namespace OcharoBlog.Api.Blog;

/// <summary>
/// Keeps the unauthenticated pilot (admin API and preview) on loopback and rejects cross-origin writes.
/// The public site is published by static export; hosting it live needs an authenticated deployment first.
/// </summary>
public sealed class LocalPilotBoundaryMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        var localHost = context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
        if (!localHost || address is not null && !IPAddress.IsLoopback(address))
        {
            await RejectAsync(context, "This pilot accepts loopback requests only.");
            return;
        }

        var method = context.Request.Method;
        if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method))
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
        if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/preview"))
            context.Response.Headers.CacheControl = "no-store";
        await next(context);
    }

    private bool IsAllowedOrigin(string origin, HttpRequest request)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.AbsolutePath != "/") return false;
        var authority = uri.GetLeftPart(UriPartial.Authority);
        if (string.Equals(authority, $"{request.Scheme}://{request.Host.Value}", StringComparison.OrdinalIgnoreCase)) return true;
        return (configuration.GetSection("Blog:TrustedOrigins").Get<string[]>() ?? []).Contains(authority, StringComparer.OrdinalIgnoreCase);
    }

    private static Task RejectAsync(HttpContext context, string detail) =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Local pilot boundary", detail: detail).ExecuteAsync(context);
}
