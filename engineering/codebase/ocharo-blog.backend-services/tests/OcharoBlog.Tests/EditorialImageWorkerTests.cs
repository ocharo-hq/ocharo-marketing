using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;
using Xunit;

namespace OcharoBlog.Tests;

/// <summary>Background removal waits on Desk's service credential, so the worker makes no HTTP call: it reports itself
/// unavailable and checks a saved edit against the current session's claims.</summary>
public sealed class EditorialImageWorkerTests
{
    private static readonly Guid Bound = Guid.NewGuid();
    private static readonly MediaEditActor Actor = new(Bound.ToString("D"), "operator");

    [Fact]
    public async Task Status_is_unavailable_and_removal_refuses_with_the_original_unchanged()
    {
        var worker = Build(Session(Bound, "editor", "editorial:edit"));

        Assert.Equal(new ImageWorkerStatus(false), await worker.GetStatusAsync(default));
        var error = await Assert.ThrowsAsync<BlogException>(() => worker.RemoveAsync([1, 2, 3], "image/png", default));
        Assert.Equal(503, error.StatusCode);
        Assert.Equal("Background removal is unavailable. Your original image is unchanged.", error.Message);
    }

    [Fact]
    public async Task Same_person_of_the_bound_organization_with_edit_may_save()
    {
        await Build(Session(Bound, "editor", "editorial:edit")).EnsureEditableAsync(Actor, default);
    }

    [Fact]
    public async Task Member_without_edit_is_forbidden()
    {
        var error = await Assert.ThrowsAsync<BlogException>(() => Build(Session(Bound, "viewer")).EnsureEditableAsync(Actor, default));

        Assert.Equal(403, error.StatusCode);
        Assert.Equal("Editor access is required.", error.Message);
    }

    [Fact]
    public async Task Actor_of_another_organization_or_person_is_refused()
    {
        var worker = Build(Session(Bound, "editor", "editorial:edit"));

        foreach (var actor in new[] { new MediaEditActor(Guid.NewGuid().ToString("D"), "operator"), new MediaEditActor("local", "local-operator"), new MediaEditActor(Bound.ToString("D"), "someone-else") })
            Assert.Equal(401, (await Assert.ThrowsAsync<BlogException>(() => worker.EnsureEditableAsync(actor, default))).StatusCode);
    }

    [Fact]
    public async Task Session_outside_the_bound_organization_or_no_session_is_refused()
    {
        foreach (var worker in new[] { Build(Session(Guid.NewGuid(), "owner", "editorial:edit")), Build(new ClaimsPrincipal(new ClaimsIdentity())), Build(null) })
        {
            var error = await Assert.ThrowsAsync<BlogException>(() => worker.EnsureEditableAsync(Actor, default));
            Assert.Equal(401, error.StatusCode);
            Assert.Equal("Your connection expired. Your original image is unchanged.", error.Message);
        }
    }

    private static ClaimsPrincipal Session(Guid organization, string role, params string[] permissions)
    {
        var id = organization.ToString("D");
        var claims = new List<Claim> { new("sub", "operator"), new(OrganizationClaimTypeConstants.Id, id), new(OrganizationClaimTypeConstants.Role, $"{id}:{role}") };
        claims.AddRange(permissions.Select(permission => new Claim(OrganizationClaimTypeConstants.Permission, $"{id}:{permission}")));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static EditorialImageWorker Build(ClaimsPrincipal? user)
    {
        var accessor = new HttpContextAccessor();
        if (user is not null) accessor.HttpContext = new DefaultHttpContext { User = user };
        return new EditorialImageWorker(Options.Create(new BlogOptions { OrganizationId = Bound }), accessor);
    }
}
