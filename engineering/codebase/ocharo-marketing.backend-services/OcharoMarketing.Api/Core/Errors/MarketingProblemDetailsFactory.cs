using OcharoMarketing.Application.Core.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Web.ErrorMapping;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Factories;

namespace OcharoMarketing.Api.Core.Errors;

/// <summary>
/// Builds every application failure through the be sdk's <see cref="AppErrorProblemDetailsFactory"/>, then titles it
/// with the error's <see cref="MarketingErrors.TitleKey"/> and codes it with its <see cref="MarketingErrors.CodeKey"/>,
/// so a refused command keeps the title and code the desk always read.
/// </summary>
public sealed class MarketingProblemDetailsFactory(
    IErrorHttpStatusCodeMapper statusMapper,
    IErrorMessageMapper messageMapper,
    IFieldErrorMessageMapper fieldMessageMapper) : IAppErrorProblemDetailsFactory
{
    private readonly AppErrorProblemDetailsFactory factory = new(statusMapper, messageMapper, fieldMessageMapper);

    /// <inheritdoc />
    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(AppError error, HttpContext httpContext)
    {
        var problem = factory.Create(error, httpContext);
        if (error.Metadata?.TryGetValue(MarketingErrors.TitleKey, out var title) == true && title is string text)
            problem.Title = text;
        if (error.Metadata?.TryGetValue(MarketingErrors.CodeKey, out var code) == true && code is string name)
            problem.Extensions["code"] = name;
        return problem;
    }
}
