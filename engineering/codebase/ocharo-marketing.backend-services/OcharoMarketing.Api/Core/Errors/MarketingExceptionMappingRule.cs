using Npgsql;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoMarketing.Api.Core.Errors;

/// <summary>
/// Maps the exceptions Marketing answers as client errors, inside the mediator and at the HTTP boundary alike: a card
/// the
/// renderer refused (422), a composition it cannot draw (400), and a PostgreSQL write that lost a lock to another
/// update
/// (409 "Workspace busy").
/// </summary>
public sealed class MarketingExceptionMappingRule : IExceptionMappingRule
{
    /// <inheritdoc />
    public AppError? TryMap(Exception exception)
    {
        return exception switch
        {
            CardRenderingException refused => MarketingErrors.CardNeedsRevision(refused.Message),
            ArgumentException => MarketingErrors.CompositionInvalid(),
            PostgresException contention when IsContention(contention) => MarketingErrors.WorkspaceBusy(),
            _ => null,
        };
    }

    /// <summary>Whether PostgreSQL gave up on a lock another update held: a serialization failure, a deadlock or a lock
    /// timeout.</summary>
    private static bool IsContention(PostgresException exception)
    {
        return exception.SqlState is PostgresErrorCodes.SerializationFailure
            or PostgresErrorCodes.DeadlockDetected
            or PostgresErrorCodes.LockNotAvailable;
    }
}
