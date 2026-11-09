using System.Security.Claims;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>What Editorial reads from the claims Ocharo's Identity issues its client (<c>editorial</c>).</summary>
public static class EditorialIdentity
{
    /// <summary>The permission unsafe requests of the private routes need.</summary>
    public const string EditPermission = "editorial:edit";

    /// <summary>The permission the approval actions need, beside <see cref="EditPermission"/>.</summary>
    public const string ApprovePermission = "editorial:approve";

    /// <summary>The claim naming the person: their Identity subject.</summary>
    public const string SubjectClaim = "sub";

    /// <summary>Whether <paramref name="user"/> is signed in with <paramref name="organization"/> as the active
    /// organization and holds a role there.</summary>
    public static bool IsMember(ClaimsPrincipal user, Guid organization) =>
        user.Identity?.IsAuthenticated == true
        && Guid.TryParse(user.FindFirstValue(OrganizationClaimTypeConstants.Id), out var active)
            && active == organization
        && user.GetOrganizationRole(organization) is not null;
}
