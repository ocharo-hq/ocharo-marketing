using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoBlog.Application.Blog.Errors;

/// <summary>Holds the error catalogue of Editorial's blog use cases; the host maps each to the status the API has
/// always
/// answered it with.</summary>
public static class BlogErrors
{
    /// <summary>The metadata key that marks a dependency Editorial needs as unavailable: the host answers it 503 under
    /// the
    /// <c>Unexpected</c> code the API has always given it.</summary>
    public const string UnavailableKey = "unavailable";

    /// <summary>The metadata key that marks a call WoW2 Files refused for a reason Editorial cannot name: the host
    /// answers
    /// it 502 under the <c>Unexpected</c> code the API has always given it.</summary>
    public const string FilesRefusedKey = "filesRefused";

    /// <summary>The metadata key that marks an image beyond the size Editorial accepts: the host answers it 413 under
    /// the
    /// <c>PayloadTooLarge</c> code the API has always given it. The SDK release after 10.0.108 carries that kind as
    /// <c>AppErrorType.PayloadTooLarge</c>, which replaces the mark.</summary>
    public const string TooLargeKey = "tooLarge";

    /// <summary>Creates the error for input a use case refuses.</summary>
    /// <param name="message">What to change, as the editor reads it.</param>
    public static AppError Invalid(string message) => AppErrorFactory.Validation(message);

    /// <summary>Creates the error for a record that does not exist.</summary>
    /// <param name="what">The record, as the editor names it: <c>Story</c>, <c>Revision</c>, <c>Image</c>.</param>
    public static AppError NotFound(string what) => AppErrorFactory.NotFound($"{what} was not found.");

    /// <summary>Creates the error for a stored file whose bytes WoW2 Files no longer holds.</summary>
    public static AppError FileGone() => AppErrorFactory.NotFound("The file is gone from WoW2 Files.");

    /// <summary>Creates the error for a change that no longer matches the stored state.</summary>
    /// <param name="message">What changed, as the editor reads it.</param>
    public static AppError Conflict(string message) => AppErrorFactory.Conflict(message);

    /// <summary>Creates the error for a save that lost a race with another save to the same record.</summary>
    public static AppError SaveRaced() => AppErrorFactory.Conflict("Another save is finishing. Reload and retry.");

    /// <summary>Creates the error for a request whose session no longer holds.</summary>
    /// <param name="message">What happened, as the editor reads it.</param>
    public static AppError Unauthorized(string message) => AppErrorFactory.Unauthorized(message);

    /// <summary>Creates the error for a signed-in person who may not do what they asked.</summary>
    /// <param name="message">What access is missing, as the editor reads it.</param>
    public static AppError Forbidden(string message) => AppErrorFactory.Forbidden(message);

    /// <summary>Creates the error for an image beyond the size Editorial accepts.</summary>
    /// <param name="message">The limit, as the editor reads it.</param>
    public static AppError TooLarge(string message) => Marked(AppErrorType.Validation, message, TooLargeKey);

    /// <summary>Creates the error for work refused while image processing runs at capacity.</summary>
    /// <param name="message">When to retry, as the editor reads it.</param>
    public static AppError Busy(string message) => AppErrorFactory.TooManyRequests(message);

    /// <summary>Creates the error for a dependency Editorial needs that cannot serve the request now.</summary>
    /// <param name="message">What is unavailable, as the editor reads it.</param>
    public static AppError Unavailable(string message) => Marked(AppErrorType.Unexpected, message, UnavailableKey);

    /// <summary>Creates the error for a call WoW2 Files refused for a reason Editorial cannot name.</summary>
    /// <param name="message">The refusal, as the editor reads it.</param>
    public static AppError FilesRefused(string message) => Marked(AppErrorType.Unexpected, message, FilesRefusedKey);

    /// <summary>Tells whether <paramref name="error"/> carries the mark <paramref name="key"/>.</summary>
    /// <param name="error">The error to read.</param>
    /// <param name="key">One of the mark keys this catalogue declares.</param>
    public static bool IsMarked(AppError error, string key)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Metadata?.ContainsKey(key) == true;
    }

    private static AppError Marked(AppErrorType type, string message, string key)
    {
        var mark = new Dictionary<string, object?>(StringComparer.Ordinal) { [key] = true };
        return AppError.Of(type, message, mark);
    }
}
