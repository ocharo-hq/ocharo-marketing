using Microsoft.AspNetCore.Mvc;
using OcharoBlog.Application.Blog.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Web.ExceptionHandling.Extensions;

namespace OcharoBlog.Api.Blog.Extensions;

/// <summary>Extends the public site's controllers with the page a public path no page answers gets.</summary>
public static class PublicNotFoundExtensions
{
    /// <summary>Answers 404 with the public site's not-found page for <paramref name="path"/>.</summary>
    /// <param name="controller">The controller handling the request.</param>
    /// <param name="sender">The mediator the page is asked for through.</param>
    /// <param name="path">The path no page answered.</param>
    /// <param name="ct">The request's cancellation.</param>
    public static async Task<IActionResult> NotFoundPageAsync(
        this ControllerBase controller,
        ISender sender,
        string path,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(sender);

        var result = await sender.SendAsync(new SiteNotFoundPageGetQuery { Path = path }, ct);

        return result.Match<IActionResult>(
            ok => new ContentResult
            {
                StatusCode = StatusCodes.Status404NotFound,
                Content = ok.Data.Content,
                ContentType = ok.Data.ContentType,
            },
            fail => controller.ToProblem(fail.Error));
    }
}
