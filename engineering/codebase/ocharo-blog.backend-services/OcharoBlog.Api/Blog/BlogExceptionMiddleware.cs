using Microsoft.EntityFrameworkCore;
using Npgsql;
using OcharoBlog.Application.Blog;

namespace OcharoBlog.Api.Blog;

/// <summary>Maps rejected blog commands to ProblemDetails responses.</summary>
public sealed class BlogExceptionMiddleware(RequestDelegate next)
{
    /// <summary>The PostgreSQL states of a save that lost a race with another: a unique key taken meanwhile, a
    /// serialization failure, a deadlock or a lock not granted.</summary>
    private static readonly HashSet<string> RaceStates = new(StringComparer.Ordinal)
    {
        PostgresErrorCodes.UniqueViolation, PostgresErrorCodes.SerializationFailure,
        PostgresErrorCodes.DeadlockDetected, PostgresErrorCodes.LockNotAvailable,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (BlogException exception) when (!context.Response.HasStarted)
        {
            await Results.Problem(statusCode: exception.StatusCode, title: "Blog command rejected", detail: exception.Message).ExecuteAsync(context);
        }
        catch (Exception exception) when (IsLostRace(exception) && !context.Response.HasStarted)
        {
            await Results.Problem(statusCode: 409, title: "Blog busy", detail: "Another save is finishing. Reload and retry.").ExecuteAsync(context);
        }
    }

    /// <summary>Whether a save failed only because another save to the same record finished first.</summary>
    private static bool IsLostRace(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => true,
        DbUpdateException { InnerException: PostgresException inner } => RaceStates.Contains(inner.SqlState),
        PostgresException postgres => RaceStates.Contains(postgres.SqlState),
        _ => false,
    };
}
