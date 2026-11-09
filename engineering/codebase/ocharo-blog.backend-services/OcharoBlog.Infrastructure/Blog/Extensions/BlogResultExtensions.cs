using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.Extensions;

/// <summary>Extends a service's result with its form at the mediator's handler boundary.</summary>
internal static class BlogResultExtensions
{
    /// <summary>Carries <paramref name="result"/> across the handler boundary: its value on success, its error on
    /// failure.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The service's result.</param>
    public static AppResult<T> ToAppResult<T>(this Result<T> result) where T : notnull
    {
        return result.Match(value => AppResult<T>.Ok(value), error => AppResult<T>.Fail(error));
    }
}
