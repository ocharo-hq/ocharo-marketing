using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OcharoMarketing.Api.Access;
using Xunit;
using static OcharoMarketing.Tests.MarketingTestIdentity;

namespace OcharoMarketing.Tests;

/// <summary>Which writes need <c>marketing:edit</c> and which also need <c>marketing:approve</c>, in any letter case, and what
/// the controllers learn about the caller. The BFF stays out: these run the access middleware on a resolved session.</summary>
public sealed class MarketingAccessMiddlewareTests
{
    [Theory]
    [InlineData("/API/BLOG/ARTICLES/abc/PUBLISH")]
    [InlineData("/api/blog/articles/abc/unpublish")]
    [InlineData("/api/blog/articles/abc/archive")]
    [InlineData("/api/marketing/drafts/abc/schedule")]
    [InlineData("/api/marketing/drafts/abc/approve")]
    [InlineData("/api/blog/articles/abc/restore")]
    [InlineData("/api/marketing/schedules/abc/queue")]
    [InlineData("/api/marketing/jobs/abc/retry")]
    [InlineData("/api/marketing/jobs/abc/dispatch")]
    [InlineData("/api/marketing/settings")]
    [InlineData("/API/MARKETING/SETTINGS/")]
    [InlineData("/api/marketing/schedules/abc/cancel")]
    [InlineData("/api/marketing/schedules/abc/reconcile")]
    [InlineData("/api/marketing/automation")]
    [InlineData("/API/MARKETING/AUTOMATION/")]
    [InlineData("/api/marketing/automation/fill")]
    [InlineData("/Api/Marketing/Automation/Fill/")]
    public async Task ApprovalWrites_ShouldNeedAnApprover(string path)
    {
        var editor = await InvokeAsync(Editor, path, "POST");
        var owner = await InvokeAsync(Owner, path, "POST");

        Assert.Equal(StatusCodes.Status403Forbidden, editor.Response.StatusCode);
        Assert.Equal(StatusCodes.Status204NoContent, owner.Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/marketing/automation/drafts", "POST")]
    [InlineData("/API/MARKETING/AUTOMATION/CAPTION", "POST")]
    [InlineData("/api/marketing/catalogue/sync", "POST")]
    [InlineData("/api/marketing/automation", "GET")]
    [InlineData("/api/marketing/settings", "GET")]
    [InlineData("/api/marketing/drafts/abc/approve", "HEAD")]
    [InlineData("/api/marketing/drafts/abc/approve", "OPTIONS")]
    public async Task DraftWritesAndReads_ShouldStayOpenToEditors(string path, string method)
    {
        Assert.Equal(StatusCodes.Status204NoContent, (await InvokeAsync(Editor, path, method)).Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/marketing/inquiries/example", "PUT")]
    [InlineData("/api/marketing/operations/channels/instagram", "PUT")]
    [InlineData("/API/MARKETING/OPERATIONS/OBSERVATIONS", "POST")]
    [InlineData("/api/marketing/assets/example", "DELETE")]
    [InlineData("/api/marketing/assets/example", "PATCH")]
    public async Task OperationsWrites_ShouldNeedAnEditor_AndGrantNoApproval(string path, string method)
    {
        Assert.Equal(StatusCodes.Status403Forbidden, (await InvokeAsync(Viewer, path, method)).Response.StatusCode);
        Assert.Equal(StatusCodes.Status204NoContent, (await InvokeAsync(Editor, path, method)).Response.StatusCode);
    }

    [Theory]
    [InlineData("/api/public/inquiries", "POST")]
    [InlineData("/API/PUBLIC/INQUIRIES", "POST")]
    [InlineData("/api/runtime-config", "GET")]
    [InlineData("/api/system/status", "GET")]
    [InlineData("/health", "GET")]
    [InlineData("/health", "PATCH")]
    [InlineData("/api/identity/session", "GET")]
    [InlineData("/API/IDENTITY/SIGN-OUT", "POST")]
    [InlineData("/", "GET")]
    [InlineData("/assets/app.js", "GET")]
    [InlineData("/media/example", "GET")]
    public async Task PublicRoutes_ShouldPassWithoutAPerson_AndCarryNoCaller(string path, string method)
    {
        var context = await InvokeAsync(null, path, method);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey("Ocharo.UserId"));
    }

    [Theory]
    [InlineData(nameof(Owner), true)]
    [InlineData(nameof(Editor), false)]
    [InlineData(nameof(Viewer), false)]
    public async Task Members_ShouldReachControllersWithTheirClaims(string name, bool canApprove)
    {
        var person = name switch { nameof(Owner) => Owner, nameof(Editor) => Editor, _ => Viewer };

        var context = await InvokeAsync(person, "/api/marketing/workspace", "GET");

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(canApprove, context.Items["Ocharo.CanApprove"]);
        Assert.Equal(Organization.ToString("D"), context.Items["Ocharo.OrganizationId"]);
        Assert.Equal(person.Subject, context.Items["Ocharo.UserId"]);
    }

    private static async Task<DefaultHttpContext> InvokeAsync(Person? person, string path, string method)
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
        context.Request.Path = path;
        context.Request.Method = method;
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost:8276");
        context.Response.Body = new MemoryStream();
        if (person is not null) context.User = Principal(person);
        var middleware = new MarketingAccessMiddleware(next =>
        {
            next.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        }, Options.Create(new MarketingAccessOptions { OrganizationId = Organization }));
        await middleware.InvokeAsync(context);
        return context;
    }
}
