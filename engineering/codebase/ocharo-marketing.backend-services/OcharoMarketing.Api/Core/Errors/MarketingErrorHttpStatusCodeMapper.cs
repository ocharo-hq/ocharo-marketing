using OcharoMarketing.Application.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Web.ErrorMapping;

namespace OcharoMarketing.Api.Core.Errors;

/// <summary>
/// Maps an application error to its HTTP status: an error marked <see cref="MarketingErrors.UnprocessableKey"/> to 422,
/// <see cref="MarketingErrors.UnavailableKey"/> to 503 and <see cref="MarketingErrors.RefusedKey"/> to 502, every other
/// error by the be sdk's defaults.
/// </summary>
public sealed class MarketingErrorHttpStatusCodeMapper : IErrorHttpStatusCodeMapper
{
    private readonly ErrorHttpStatusCodeMapper defaults = new();

    /// <summary>Maps <paramref name="error"/> to the HTTP status the host answers it with.</summary>
    /// <param name="error">The error to map.</param>
    /// <returns>The HTTP status code.</returns>
    public int ToStatusCode(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (MarketingErrors.IsMarked(error, MarketingErrors.UnprocessableKey))
            return StatusCodes.Status422UnprocessableEntity;
        if (MarketingErrors.IsMarked(error, MarketingErrors.UnavailableKey))
            return StatusCodes.Status503ServiceUnavailable;
        if (MarketingErrors.IsMarked(error, MarketingErrors.RefusedKey))
            return StatusCodes.Status502BadGateway;
        return defaults.ToStatusCode(error);
    }
}
