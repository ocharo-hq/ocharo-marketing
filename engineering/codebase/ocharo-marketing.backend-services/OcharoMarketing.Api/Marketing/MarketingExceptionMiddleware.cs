using Microsoft.Data.Sqlite;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Cards;

namespace OcharoMarketing.Api.Marketing;

/// <summary>Maps rejected marketing commands to explicit ProblemDetails responses.</summary>
public sealed class MarketingExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (MarketingException exception) when (!context.Response.HasStarted)
        {
            await Results.Problem(statusCode: exception.StatusCode, title: "Marketing command rejected", detail: exception.Message).ExecuteAsync(context);
        }
        catch (CardRenderingException exception) when (!context.Response.HasStarted)
        {
            await Results.Problem(statusCode: 422, title: "Card needs revision", detail: exception.Message).ExecuteAsync(context);
        }
        catch (ArgumentException) when (!context.Response.HasStarted)
        {
            await Results.Problem(statusCode: 400, title: "Invalid composition", detail: "The supplied composition is invalid.").ExecuteAsync(context);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode is 5 or 6 && !context.Response.HasStarted)
        {
            await Results.Problem(statusCode: 409, title: "Workspace busy", detail: "Another update is finishing. Reload and retry.").ExecuteAsync(context);
        }
    }
}
