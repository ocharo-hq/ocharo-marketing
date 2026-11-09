using Microsoft.EntityFrameworkCore;
using Npgsql;
using OcharoBlog.Application.Blog.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoBlog.Persistence.Blog.Errors;

/// <summary>Maps a save that lost a race with another save to the same record to
/// <see cref="BlogErrors.SaveRaced"/>: a concurrency token that moved, or a PostgreSQL unique key taken meanwhile, a
/// serialization failure, a deadlock or a lock not granted.</summary>
public sealed class BlogSaveRaceExceptionRule : IExceptionMappingRule
{
    private static readonly HashSet<string> RaceStates = new(StringComparer.Ordinal)
    {
        PostgresErrorCodes.UniqueViolation,
        PostgresErrorCodes.SerializationFailure,
        PostgresErrorCodes.DeadlockDetected,
        PostgresErrorCodes.LockNotAvailable,
    };

    /// <inheritdoc />
    public AppError? TryMap(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return IsLostRace(exception) ? BlogErrors.SaveRaced() : null;
    }

    /// <summary>Tells whether a save failed only because another save to the same record finished first.</summary>
    /// <param name="exception">The exception the save raised.</param>
    public static bool IsLostRace(Exception exception)
    {
        return exception switch
        {
            DbUpdateConcurrencyException => true,
            DbUpdateException { InnerException: PostgresException inner } => RaceStates.Contains(inner.SqlState),
            PostgresException postgres => RaceStates.Contains(postgres.SqlState),
            _ => false,
        };
    }
}
