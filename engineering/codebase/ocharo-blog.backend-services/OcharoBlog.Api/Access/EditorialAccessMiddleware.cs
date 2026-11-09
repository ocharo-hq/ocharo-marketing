using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OcharoBlog.Api.Blog;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;
using WoW.Two.Sdk.Beta.Be.Identity.CurrentPrincipal;

namespace OcharoBlog.Api.Access;

/// <summary>
/// Admits a session to the private routes only as a member of the bound organization (<c>Blog:OrganizationId</c>): the
/// session's active organization must be it and carry a role there. Reads need the membership, writes
/// <see cref="EditorialIdentity.EditPermission"/>, approval actions <see cref="EditorialIdentity.ApprovePermission"/> too.
/// Runs after the BFF, which resolves the session and checks its <c>X-Csrf-Token</c>; technical, public and session routes
/// pass untouched, and so does media the public site shows: the settings' images, published covers and the published brand
/// logo. Controllers read the admitted person from <c>Ocharo.CanApprove</c>, <c>Ocharo.OrganizationId</c> and <c>Ocharo.UserId</c>.
/// A live export also admits a service token with <see cref="EditorialAccess.ExportScope"/>, setting no person.
/// </summary>
public sealed class EditorialAccessMiddleware(RequestDelegate next, IOptions<BlogOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = (context.Request.Path.Value ?? "/").ToLowerInvariant();
        if (TechnicalRoutes.IsPublic(context.Request) || EditorialAccess.IsSessionRoute(path) || !await IsPrivateAsync(context, path, options.Value.OrganizationId))
        {
            await next(context);
            return;
        }

        if (EditorialAccess.OffersExportToken(context.Request, path))
        {
            await AdmitExportTokenAsync(context);
            return;
        }

        var user = context.User;
        var subject = user.FindFirst(EditorialIdentity.SubjectClaim)?.Value;
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(subject))
        {
            await ProblemAsync(context, StatusCodes.Status401Unauthorized, "Sign in to continue", "sign_in_required");
            return;
        }

        var organization = options.Value.OrganizationId;
        if (!EditorialIdentity.IsMember(user, organization))
        {
            await ProblemAsync(context, StatusCodes.Status403Forbidden, "This account is not a member of the Ocharo organization", "organization_access_required");
            return;
        }

        var writes = EditorialAccess.IsWrite(context.Request.Method);
        if (writes && !user.HasOrganizationPermission(organization, EditorialIdentity.EditPermission))
        {
            await ProblemAsync(context, StatusCodes.Status403Forbidden, "Editing access is required", "edit_permission_required");
            return;
        }

        var canApprove = user.HasOrganizationPermission(organization, EditorialIdentity.ApprovePermission);
        if (writes && !canApprove && EditorialAccess.IsApproval(path))
        {
            await ProblemAsync(context, StatusCodes.Status403Forbidden, "Publication approval access is required", "approval_permission_required");
            return;
        }

        context.Items["Ocharo.CanApprove"] = canApprove;
        context.Items["Ocharo.OrganizationId"] = organization.ToString("D");
        context.Items["Ocharo.UserId"] = subject;
        await next(context);
    }

    private async Task AdmitExportTokenAsync(HttpContext context)
    {
        var token = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!token.Succeeded)
        {
            await ProblemAsync(context, StatusCodes.Status401Unauthorized, "Sign in to continue", "sign_in_required");
            return;
        }

        // Only a service's own token counts here: a person reaches an export through the session's checks.
        var caller = ClaimsPrincipalNormalizer.Normalize(token.Principal!);
        if (caller.Kind != PrincipalKind.Service || !caller.Scopes.Contains(EditorialAccess.ExportScope))
        {
            await ProblemAsync(context, StatusCodes.Status403Forbidden, "This token may not read exports", "scope_required");
            return;
        }

        await next(context);
    }

    /// <summary>Holds the cache key of the published brand logo's SHA-256.</summary>
    private const string BrandLogoCacheKey = "editorial.access.brand-logo";

    private static async Task<bool> IsPrivateAsync(HttpContext context, string path, Guid organization)
    {
        if (!EditorialAccess.IsMediaRoute(path)) return EditorialAccess.IsPrivateRoute(path);
        var sha = path.Split('/').ElementAtOrDefault(2);
        var site = await context.RequestServices.GetRequiredService<SiteSettingsService>().GetAsync(context.RequestAborted);
        if (new[] { site.Settings.LogoSha256, site.Settings.HeroSha256, site.Settings.BandSha256 }.Contains(sha)) return false;
        var articles = await context.RequestServices.GetRequiredService<IBlogRepository>().GetArticlesAsync(context.RequestAborted);
        if (articles.Where(article => article.Status == "published")
            .Select(article => BlogJson.Read<ArticleDocument>(article.Json))
            .Any(document => document.Cover?.MediaSha256 == sha)) return false;

        // The published brand logo stands in for the settings' logo on every page (SiteService.BrandedAsync), and Brand's
        // own feed discloses it. Asking Brand costs a call, so a member, whom the session checks admit anyway, never waits on
        // it, and every page's logo request shares one answer a minute (ten seconds while Brand gives none).
        if (EditorialIdentity.IsMember(context.User, organization)) return true;
        var logo = await context.RequestServices.GetRequiredService<IMemoryCache>().GetOrCreateAsync(BrandLogoCacheKey, async entry =>
        {
            var kit = await context.RequestServices.GetRequiredService<IBrandKit>().GetAsync(context.RequestAborted);
            entry.AbsoluteExpirationRelativeToNow = kit?.LogoSha256 is null ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1);
            return kit?.LogoSha256;
        });
        return sha is null || logo != sha;
    }

    private static Task ProblemAsync(HttpContext context, int status, string title, string code) =>
        Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code }).ExecuteAsync(context);
}
