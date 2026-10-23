using System.Net;

namespace OcharoBlog.Api.Blog;

/// <summary>
/// Keeps the unauthenticated pilot (admin API and preview) on loopback unless it is public (<see cref="IsPublic"/>), and
/// rejects cross-origin writes.
/// </summary>
public sealed class LocalPilotBoundaryMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        var localHost = context.Request.Host.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
        if (!IsPublic(configuration) && (!localHost || address is not null && !IPAddress.IsLoopback(address)))
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

    /// <summary>
    /// Whether any caller may reach the Editorial host without sign-in. The owner opened the deployed hosts to the public on
    /// 2026-10-24, until the Ocharo identity server lands: <c>Blog:PublicAccess</c> decides, and unset it is on for a
    /// deployed host (<c>DEPLOY_ENVIRONMENT</c>) and off for a local run, which keeps the loopback boundary.
    /// </summary>
    public static bool IsPublic(IConfiguration configuration) =>
        configuration.GetValue<bool?>("Blog:PublicAccess")
        ?? !string.IsNullOrWhiteSpace(configuration["DEPLOY_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("DEPLOY_ENVIRONMENT"));

    private bool IsAllowedOrigin(string origin, HttpRequest request)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.AbsolutePath != "/") return false;
        var authority = uri.GetLeftPart(UriPartial.Authority);
        if (string.Equals(authority, $"{request.Scheme}://{request.Host.Value}", StringComparison.OrdinalIgnoreCase)) return true;
        return (configuration.GetSection("Blog:TrustedOrigins").Get<string[]>() ?? []).Contains(authority, StringComparer.OrdinalIgnoreCase);
    }

    private static Task RejectAsync(HttpContext context, string detail) =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Local pilot boundary", detail: detail).ExecuteAsync(context);
}
