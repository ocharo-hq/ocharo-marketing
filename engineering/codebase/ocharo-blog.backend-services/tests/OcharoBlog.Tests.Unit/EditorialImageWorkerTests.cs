using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Identity.Core.Organizations;
using Xunit;

namespace OcharoBlog.Tests.Unit;

/// <summary>With no Desk configured the worker makes no HTTP call and reports itself unavailable; it checks a saved
/// edit against
/// the current session's claims. Its calls to Desk have their own tests in
/// <see cref="EditorialServiceTokenTests"/>.</summary>
public sealed class EditorialImageWorkerTests
{
    private static readonly Guid Bound = Guid.NewGuid();
    private static readonly MediaEditActor Actor = new(Bound.ToString("D"), "operator");

    [Fact]
    public async Task RemoveAsync_ShouldRefuseAsUnavailable_WhenNoDeskIsConfigured()
    {
        var worker = Build(Session(Bound, "editor", "editorial:edit"));

        (await worker.GetStatusAsync(default)).Should().Be(new ImageWorkerStatus(false));
        var error = Refusals.Of(await worker.RemoveAsync([1, 2, 3], "image/png", default));
        Refusals.Status(error).Should().Be(503);
        error.Message.Should().Be("Background removal is unavailable. Your original image is unchanged.");
    }

    [Fact]
    public async Task EnsureEditableAsync_ShouldSucceed_WhenTheSamePersonMayEdit()
    {
        (await Build(Session(Bound, "editor", "editorial:edit")).EnsureEditableAsync(Actor, default))
            .Should()
            .BeOfType<Result.Success>();
    }

    [Fact]
    public async Task EnsureEditableAsync_ShouldRefuseAsForbidden_WhenTheMemberCannotEdit()
    {
        var error = Refusals.Of(await Build(Session(Bound, "viewer")).EnsureEditableAsync(Actor, default));

        Refusals.Status(error).Should().Be(403);
        error.Message.Should().Be("Editor access is required.");
    }

    [Fact]
    public async Task EnsureEditableAsync_ShouldRefuseAsExpired_WhenTheActorIsSomeoneElse()
    {
        var worker = Build(Session(Bound, "editor", "editorial:edit"));

        foreach (var actor in new[]
            {
                new MediaEditActor(Guid.NewGuid().ToString("D"), "operator"),
                new MediaEditActor("local", "local-operator"),
                new MediaEditActor(Bound.ToString("D"), "someone-else")
            })
            (Refusals.Status(Refusals.Of(await worker.EnsureEditableAsync(actor, default)))).Should().Be(401);
    }

    [Fact]
    public async Task EnsureEditableAsync_ShouldRefuseAsExpired_WhenTheSessionIsOutsideOrMissing()
    {
        foreach (var worker in new[]
            {
                Build(Session(Guid.NewGuid(), "owner", "editorial:edit")),
                Build(new ClaimsPrincipal(new ClaimsIdentity())),
                Build(null)
            })
        {
            var error = Refusals.Of(await worker.EnsureEditableAsync(Actor, default));
            Refusals.Status(error).Should().Be(401);
            error.Message.Should().Be("Your connection expired. Your original image is unchanged.");
        }
    }

    private static ClaimsPrincipal Session(Guid organization, string role, params string[] permissions)
    {
        var id = organization.ToString("D");
        var claims = new List<Claim>
        {
            new("sub", "operator"),
            new(OrganizationClaimTypeConstants.Id, id),
            new(OrganizationClaimTypeConstants.Role, $"{id}:{role}")
        };
        claims.AddRange(permissions.Select(permission =>
            new Claim(OrganizationClaimTypeConstants.Permission, $"{id}:{permission}")));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static EditorialImageWorker Build(ClaimsPrincipal? user)
    {
        var accessor = new HttpContextAccessor();
        if (user is not null) accessor.HttpContext = new DefaultHttpContext { User = user };
        return new EditorialImageWorker(
            Options.Create(new BlogOptions { OrganizationId = Bound }),
            accessor,
            new NoClients(),
            NullLogger<EditorialImageWorker>.Instance);
    }

    private sealed class NoClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("No HTTP call is expected.");
    }
}
