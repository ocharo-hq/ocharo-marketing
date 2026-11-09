using OcharoBlog.Application.Blog.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Web.ErrorMapping;

namespace OcharoBlog.Api.Blog.Errors;

/// <summary>Maps an application error to its HTTP status: a call WoW2 Files refused to 502 and a dependency Editorial
/// needs as unavailable to 503, both under the <c>Unexpected</c> code the API has always given them; every other error
/// by the be sdk's defaults.</summary>
public sealed class BlogErrorHttpStatusCodeMapper : IErrorHttpStatusCodeMapper
{
    private readonly ErrorHttpStatusCodeMapper defaults = new();

    /// <summary>Maps <paramref name="error"/> to the HTTP status the host answers it with.</summary>
    /// <param name="error">The error to map.</param>
    /// <returns>The HTTP status code.</returns>
    public int ToStatusCode(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (BlogErrors.IsMarked(error, BlogErrors.FilesRefusedKey)) return StatusCodes.Status502BadGateway;
        if (BlogErrors.IsMarked(error, BlogErrors.UnavailableKey)) return StatusCodes.Status503ServiceUnavailable;
        return defaults.ToStatusCode(error);
    }
}
