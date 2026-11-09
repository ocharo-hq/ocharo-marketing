using OcharoBlog.Api.Access;
using OcharoBlog.Infrastructure.Blog;

namespace OcharoBlog.Api.Blog;

/// <summary>
/// Sets Editorial's response headers and refuses cross-origin writes before sign-in runs: every response is
/// <c>nosniff</c>, the API, previews and media are <c>no-store</c>, and a write must come from this origin or
/// <c>Blog:TrustedOrigins</c>. Who may call what is <see cref="EditorialAccessMiddleware"/>'s.
/// </summary>
public sealed class EditorialHttpBoundaryMiddleware(RequestDelegate next, BlogOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var path = (context.Request.Path.Value ?? "/").ToLowerInvariant();
        if (path == "/api"
            || path.StartsWith("/api/", StringComparison.Ordinal)
            || path.StartsWith("/preview", StringComparison.Ordinal)
            || path.StartsWith("/media/", StringComparison.Ordinal))
            NoStore(context);

        if (EditorialAccess.IsWrite(context.Request.Method) && !IsAllowedOrigin(context.Request))
        {
            await Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Cross-origin write refused",
                detail: "Cross-origin writes are disabled.",
                extensions: new Dictionary<string, object?> { ["code"] = "cross_origin_write" }).ExecuteAsync(context);
            return;
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

    private bool IsAllowedOrigin(HttpRequest request)
    {
        var origin = request.Headers.Origin.ToString();
        if (origin.Length == 0)
            return !request.Headers.ContainsKey("Sec-Fetch-Site") || request.Headers["Sec-Fetch-Site"] == "same-origin";
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0
            || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0) return false;
        var authority = uri.GetLeftPart(UriPartial.Authority);
        if (string.Equals(authority, $"{request.Scheme}://{request.Host.Value}", StringComparison.OrdinalIgnoreCase))
            return true;
        return options.TrustedOrigins.Contains(authority, StringComparer.OrdinalIgnoreCase);
    }
}
