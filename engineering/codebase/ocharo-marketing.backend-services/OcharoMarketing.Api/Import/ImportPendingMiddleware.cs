using OcharoMarketing.Api.Access;
using OcharoMarketing.Infrastructure.Marketing;

namespace OcharoMarketing.Api.Import;

/// <summary>Answers Marketing's data routes 503 with the problem <c>code: import_pending</c> while the SQLite store an
/// earlier release kept waits for <c>import-sqlite</c> (<see cref="SqliteImportGate"/>).</summary>
/// <remarks>Health, service status, runtime config, the Identity session routes and the SPA stay open, so the deploy's
/// checks pass and the desk can say why it waits. The public inquiry form is a data route: it writes.</remarks>
public sealed class ImportPendingMiddleware(RequestDelegate next, SqliteImportGate gate)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsDataRoute(context.Request) && gate.IsPending)
        {
            context.Response.Headers.RetryAfter = "60";
            await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Import pending",
                detail: "Marketing's earlier records are waiting for their import. Retry once it has run.",
                extensions: new Dictionary<string, object?> { ["code"] = "import_pending" }).ExecuteAsync(context);
            return;
        }
        await next(context);
    }

    /// <summary>Whether a request reads or writes Marketing's records: a private route, or anything under
    /// <c>/api/public/</c>.</summary>
    private static bool IsDataRoute(HttpRequest request) =>
        MarketingAccess.IsPrivate(request)
        || (request.Path.Value ?? "/").StartsWith("/api/public/", StringComparison.OrdinalIgnoreCase);
}
