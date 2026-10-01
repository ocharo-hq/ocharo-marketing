using Microsoft.Data.Sqlite;
using OcharoBlog.Application.Blog;

namespace OcharoBlog.Api.Blog;

/// <summary>Maps rejected blog commands to ProblemDetails responses.</summary>
public sealed class BlogExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (BlogException exception) when (!context.Response.HasStarted)
        {
            await Results.Problem(statusCode: exception.StatusCode, title: "Blog command rejected", detail: exception.Message).ExecuteAsync(context);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6 && !context.Response.HasStarted)
        {
            await Results.Problem(statusCode: 409, title: "Blog busy", detail: "Another save is finishing. Reload and retry.").ExecuteAsync(context);
        }
    }
}
