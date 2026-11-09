using OcharoBlog.Api.Access;
using OcharoBlog.Infrastructure.Blog;

namespace OcharoBlog.Api.Blog;

/// <summary>
/// Answers <c>503</c> with <c>code: import_pending</c> while the SQLite store an earlier release kept still waits for
/// <c>import-sqlite</c>: the data routes, the public site and media, rather than an empty blog or a write the import
/// would then refuse. The technical routes (<see cref="TechnicalRoutes"/>), sign-in (<c>/api/identity/*</c>),
/// Identity's revocation signals and the Studio's own files stay open.
/// </summary>
public sealed class SqliteImportPendingMiddleware(RequestDelegate next, SqliteImportGate gate)
{
    /// <summary>The seconds a client waits before it asks again.</summary>
    private const string RetryAfterSeconds = "60";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!gate.IsPending || IsOpen(context.Request))
        {
            await next(context);
            return;
        }

        context.Response.Headers.RetryAfter = RetryAfterSeconds;
        await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Editorial is importing its records",
            detail: "The records an earlier release kept are being moved; try again shortly.",
            extensions: new Dictionary<string, object?> { ["code"] = "import_pending" }).ExecuteAsync(context);
    }

    /// <summary>Whether a request reaches no blog record: a technical route, sign-in, a signal or the Studio's
    /// files.</summary>
    private static bool IsOpen(HttpRequest request)
    {
        var path = (request.Path.Value ?? "/").ToLowerInvariant();
        return TechnicalRoutes.IsPublic(request)
            || EditorialAccess.IsSessionRoute(path)
            || path == EditorialAccess.SignalPath
            || path == "/studio" || path.StartsWith("/studio/", StringComparison.Ordinal);
    }
}
