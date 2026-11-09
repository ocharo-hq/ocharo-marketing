using OcharoMarketing.Api.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;

namespace OcharoMarketing.Tests.Support;

/// <summary>Reads the HTTP status the host answers a thrown catalogue error with, through the host's own
/// mapper.</summary>
internal static class ErrorStatus
{
    private static readonly MarketingErrorHttpStatusCodeMapper Mapper = new();

    /// <summary>The HTTP status of <paramref name="exception"/>'s error.</summary>
    /// <param name="exception">The thrown catalogue error.</param>
    public static int Of(AppException exception)
    {
        return Mapper.ToStatusCode(exception.Error);
    }
}
