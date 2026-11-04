using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Reports background removal as unavailable and checks a saved edit against the current session.</summary>
/// <remarks>Background removal waits on Desk's service credential (<c>identity-cutover.md</c> #47); the previous HTTP call is in commit <c>67c4193</c>.</remarks>
public sealed class EditorialImageWorker(IOptions<BlogOptions> options, IHttpContextAccessor accessor) : IEditorialImageWorker
{
    public Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct) => Task.FromResult(new ImageWorkerStatus(false));

    public Task<(byte[] Bytes, string Model)> RemoveAsync(byte[] bytes, string contentType, CancellationToken ct) =>
        Task.FromException<(byte[] Bytes, string Model)>(new BlogException("Background removal is unavailable. Your original image is unchanged.", 503));

    public Task EnsureEditableAsync(MediaEditActor actor, CancellationToken ct)
    {
        var organization = options.Value.OrganizationId;
        if (accessor.HttpContext?.User is not { } user
            || !EditorialIdentity.IsMember(user, organization)
            || !Guid.TryParse(actor.OrganizationId, out var actorOrganization) || actorOrganization != organization
            || user.FindFirst(EditorialIdentity.SubjectClaim)?.Value != actor.UserId)
            throw new BlogException("Your connection expired. Your original image is unchanged.", 401);
        if (!user.HasOrganizationPermission(organization, EditorialIdentity.EditPermission))
            throw new BlogException("Editor access is required.", 403);
        return Task.CompletedTask;
    }
}
