using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;

namespace OcharoMarketing.Api.Access;

/// <summary>
/// Admits a session to the private routes only as a member of the bound organization (<c>Marketing:OrganizationId</c>): the
/// session's active organization must be it and carry a role there. Reads need the membership, writes
/// <see cref="MarketingAccess.EditPermission"/>, approval writes <see cref="MarketingAccess.ApprovePermission"/> too. Runs
/// after <c>UseIdentityBff</c>, which checks the session's <c>X-Csrf-Token</c>, and publishes who is calling to the
/// controllers through <c>HttpContext.Items</c>; public requests and the session routes pass untouched.
/// </summary>
public sealed class MarketingAccessMiddleware(RequestDelegate next, IOptions<MarketingAccessOptions> options)
{
    public Task InvokeAsync(HttpContext http)
    {
        if (!MarketingAccess.IsPrivate(http.Request)) return next(http);

        var user = http.User;
        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (user.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(subject))
            return ProblemAsync(http, StatusCodes.Status401Unauthorized, "Sign in to continue", "sign_in_required");

        var organization = options.Value.OrganizationId;
        if (!Guid.TryParse(user.FindFirstValue(OrganizationClaimTypeConstants.Id), out var active) || active != organization
            || user.GetOrganizationRole(organization) is null)
            return ProblemAsync(http, StatusCodes.Status403Forbidden, "This account is not a member of the Ocharo organization", "organization_access_required");

        var canApprove = user.HasOrganizationPermission(organization, MarketingAccess.ApprovePermission);
        if (MarketingAccess.IsWrite(http.Request.Method))
        {
            if (!user.HasOrganizationPermission(organization, MarketingAccess.EditPermission))
                return ProblemAsync(http, StatusCodes.Status403Forbidden, "Editing access is required", "edit_permission_required");
            if (!canApprove && MarketingAccess.NeedsApproval(http.Request))
                return ProblemAsync(http, StatusCodes.Status403Forbidden, "Approval access is required", "approval_permission_required");
        }

        http.Items["Ocharo.CanApprove"] = canApprove;
        http.Items["Ocharo.OrganizationId"] = organization.ToString("D", CultureInfo.InvariantCulture);
        http.Items["Ocharo.UserId"] = subject;
        return next(http);
    }

    private static Task ProblemAsync(HttpContext http, int status, string title, string code) =>
        Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code }).ExecuteAsync(http);
}
