using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoBlog.Api.Blog.Errors;

/// <summary>Extends Editorial's controllers with the problem a refused request answers.</summary>
public static class BlogProblemExtensions
{
    /// <summary>The code an image too large has always carried; the published be sdk has no kind that names it
    /// yet.</summary>
    private const string PayloadTooLarge = "PayloadTooLarge";

    /// <summary>Creates the problem for <paramref name="error"/> through the be sdk's problem factory. An image too
    /// large keeps its <c>PayloadTooLarge</c> code and type until the SDK release that carries the kind.</summary>
    /// <param name="controller">The controller handling the request.</param>
    /// <param name="error">The error to answer.</param>
    public static ObjectResult ToBlogProblem(this ControllerBase controller, AppError error)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(error);

        var result = controller.ToProblem(error);
        if (BlogErrors.IsMarked(error, BlogErrors.TooLargeKey) && result.Value is ProblemDetails problem)
        {
            problem.Type = $"urn:wow-two:error:{PayloadTooLarge}";
            problem.Extensions["code"] = PayloadTooLarge;
        }
        return result;
    }
}
