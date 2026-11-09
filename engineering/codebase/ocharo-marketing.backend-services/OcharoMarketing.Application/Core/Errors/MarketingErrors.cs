using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoMarketing.Application.Core.Errors;

/// <summary>
/// Holds Marketing's error catalogue. Each kind keeps the <c>code</c>, status and title the API answered before the
/// mediator: a refused command reads "Marketing command rejected", a well-formed request Marketing cannot carry out
/// answers 422, an upload outside the accepted size or type 413 or 415, and an upstream failure 502 or 503 under the
/// <c>Unexpected</c> code. The markers, the title and a code the error kind cannot name travel as metadata; the host's
/// status mapper and problem factory read them.
/// </summary>
public static class MarketingErrors
{
    /// <summary>The metadata key that marks a validation failure answered 422 Unprocessable Content.</summary>
    public const string UnprocessableKey = "unprocessable";

    /// <summary>The metadata key that marks an upstream service that did not answer: the host answers 503.</summary>
    public const string UnavailableKey = "upstreamUnavailable";

    /// <summary>The metadata key that marks an upstream service that refused the call: the host answers 502.</summary>
    public const string RefusedKey = "upstreamRefused";

    /// <summary>The metadata key that marks an upload outside the accepted size: the host answers 413.</summary>
    public const string PayloadTooLargeKey = "payloadTooLarge";

    /// <summary>The metadata key that marks an upload of a media type Marketing does not store: the host answers
    /// 415.</summary>
    public const string UnsupportedMediaKey = "unsupportedMedia";

    /// <summary>The metadata key of the problem title an error answers with.</summary>
    public const string TitleKey = "problemTitle";

    /// <summary>The metadata key of a problem <c>code</c> the error's kind cannot name in this be sdk.</summary>
    public const string CodeKey = "problemCode";

    /// <summary>The title of a command Marketing refused.</summary>
    public const string RejectedTitle = "Marketing command rejected";

    /// <summary>The detail every composition the card renderer refuses answers with.</summary>
    public const string CompositionInvalidMessage = "The supplied composition is invalid.";

    /// <summary>The detail a write that lost a lock to another update answers with.</summary>
    public const string WorkspaceBusyMessage = "Another update is finishing. Reload and retry.";

    /// <summary>Creates the error for input the command cannot accept.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Invalid(string message)
    {
        return Of(AppErrorType.Validation, message, RejectedTitle);
    }

    /// <summary>Creates the error for a record the command names that does not exist.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError NotFound(string message)
    {
        return Of(AppErrorType.NotFound, message, RejectedTitle);
    }

    /// <summary>Creates the error for a command the current state refuses: a stale revision, a disabled
    /// provider.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Conflict(string message)
    {
        return Of(AppErrorType.Conflict, message, RejectedTitle);
    }

    /// <summary>Creates the error for an upload outside the accepted size, marked by
    /// <see cref="PayloadTooLargeKey"/>.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError TooLarge(string message)
    {
        return Of(AppErrorType.Validation, message, RejectedTitle, PayloadTooLargeKey, "PayloadTooLarge");
    }

    /// <summary>Creates the error for an upload of a media type Marketing does not store, marked by
    /// <see cref="UnsupportedMediaKey"/>.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError UnsupportedMedia(string message)
    {
        return Of(AppErrorType.Validation, message, RejectedTitle, UnsupportedMediaKey, "UnsupportedMediaType");
    }

    /// <summary>Creates the error for a command that needs the operator's identity and has none.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Unauthenticated(string message)
    {
        return Of(AppErrorType.Unauthorized, message, RejectedTitle);
    }

    /// <summary>Creates the error for a command over its budget.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Throttled(string message)
    {
        return Of(AppErrorType.TooManyRequests, message, RejectedTitle);
    }

    /// <summary>Creates the error for an upstream service that did not answer, marked by
    /// <see cref="UnavailableKey"/>.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Unavailable(string message)
    {
        return Of(AppErrorType.Unexpected, message, RejectedTitle, UnavailableKey);
    }

    /// <summary>Creates the error for an upstream service that refused the call, marked by
    /// <see cref="RefusedKey"/>.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Refused(string message)
    {
        return Of(AppErrorType.Unexpected, message, RejectedTitle, RefusedKey);
    }

    /// <summary>Creates the error for a well-formed request Marketing cannot carry out, marked by
    /// <see cref="UnprocessableKey"/>.</summary>
    /// <param name="message">The safe explanation the client shows.</param>
    public static AppError Unprocessable(string message)
    {
        return Of(AppErrorType.Validation, message, RejectedTitle, UnprocessableKey);
    }

    /// <summary>Creates the error for a card the renderer refused, marked by <see cref="UnprocessableKey"/>.</summary>
    /// <param name="message">The renderer's guidance.</param>
    public static AppError CardNeedsRevision(string message)
    {
        return Of(AppErrorType.Validation, message, "Card needs revision", UnprocessableKey);
    }

    /// <summary>Creates the error for a composition the card renderer cannot draw.</summary>
    public static AppError CompositionInvalid()
    {
        return Of(AppErrorType.Validation, CompositionInvalidMessage, "Invalid composition");
    }

    /// <summary>Creates the error for a write that lost a lock to another update.</summary>
    public static AppError WorkspaceBusy()
    {
        return Of(AppErrorType.Conflict, WorkspaceBusyMessage, "Workspace busy");
    }

    /// <summary>Tells whether an error is an upstream failure: a service that did not answer or refused the
    /// call.</summary>
    /// <param name="error">The error to inspect.</param>
    public static bool IsUpstreamFailure(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return IsMarked(error, UnavailableKey) || IsMarked(error, RefusedKey);
    }

    /// <summary>Tells whether an error carries a marker.</summary>
    /// <param name="error">The error to inspect.</param>
    /// <param name="key">The marker's metadata key.</param>
    public static bool IsMarked(AppError error, string key)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.Metadata?.ContainsKey(key) == true;
    }

    private static AppError Of(
        AppErrorType type,
        string message,
        string title,
        string? marker = null,
        string? code = null)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal) { [TitleKey] = title };
        if (marker is not null)
            metadata[marker] = true;
        if (code is not null)
            metadata[CodeKey] = code;
        return AppError.Of(type, message, metadata);
    }
}
