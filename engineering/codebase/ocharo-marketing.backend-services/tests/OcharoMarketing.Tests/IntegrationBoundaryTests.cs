using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcharoMarketing.Api.Marketing;
using Xunit;
namespace OcharoMarketing.Tests;
public sealed class IntegrationBoundaryTests
{
    private static readonly string Org=Guid.NewGuid().ToString();
    [Theory]
    [InlineData("/api/blog/articles")][InlineData("/API/BLOG/ARTICLES")][InlineData("/Api/Marketing/Workspace")]
    [InlineData("/api/marketing/inquiries")][InlineData("/api/marketing/inquiries/example/activity")]
    [InlineData("/api/marketing/operations")][InlineData("/API/MARKETING/OPERATIONS/CHANNELS")]
    [InlineData("/PREVIEW/123")][InlineData("/preview")][InlineData("/API/BLOG/EXPORT/SITE.ZIP")]
    public async Task Every_private_route_requires_a_current_ticket(string path)
    {
        var context=Context(path);await Boundary().InvokeAsync(context);
        Assert.Equal(401,context.Response.StatusCode);Assert.Equal("no-store",context.Response.Headers.CacheControl.ToString());
    }
    [Theory]
    [InlineData("http://localhost:8264",503)][InlineData("https://localhost:8264",204)]
    public async Task Configured_introspection_requires_https(string origin,int status)
    {var context=Context("/api/marketing/workspace",ticket:true);await Boundary(origin:origin).InvokeAsync(context);Assert.Equal(status,context.Response.StatusCode);}
    [Fact]
    public async Task Organization_mismatch_is_rejected()
    {var c=Context("/api/blog/articles",ticket:true);await Boundary(actualOrg:Guid.NewGuid().ToString()).InvokeAsync(c);Assert.Equal(401,c.Response.StatusCode);}
    [Fact]
    public async Task Missing_organization_binding_fails_closed()
    {var c=Context("/api/blog/articles",ticket:true);await Boundary(configOrg:"").InvokeAsync(c);Assert.Equal(503,c.Response.StatusCode);}
    [Theory]
    [InlineData("https://localhost:8352",204)][InlineData("https://localhost:8344",403)][InlineData("http://localhost:8352",403)][InlineData("https://evil.example",403)][InlineData("null",403)]
    public async Task Browser_writes_require_exact_origin(string origin,int status)
    {var c=Context("/api/blog/articles",ticket:true,method:"POST");c.Request.Headers.Origin=origin;await Boundary().InvokeAsync(c);Assert.Equal(status,c.Response.StatusCode);}
    [Theory]
    [InlineData("/API/BLOG/ARTICLES/abc/PUBLISH")][InlineData("/api/blog/articles/abc/archive")][InlineData("/api/marketing/drafts/abc/approve")]
    public async Task Mixed_case_approval_actions_require_approver(string path)
    {var c=Context(path,ticket:true,method:"POST");await Boundary(canApprove:false).InvokeAsync(c);Assert.Equal(403,c.Response.StatusCode);}
    [Theory]
    [InlineData("/api/marketing/automation", "PUT")]
    [InlineData("/API/MARKETING/AUTOMATION/", "PUT")]
    [InlineData("/api/marketing/automation/fill", "POST")]
    [InlineData("/Api/Marketing/Automation/Fill/", "POST")]
    [InlineData("/api/marketing/settings", "PUT")]
    [InlineData("/API/MARKETING/SETTINGS/", "PUT")]
    public async Task Scheduling_settings_and_fill_require_approver(string path,string method)
    {
        var editor=Context(path,ticket:true,method:method);
        await Boundary(canApprove:false).InvokeAsync(editor);Assert.Equal(403,editor.Response.StatusCode);
        var approver=Context(path,ticket:true,method:method);
        await Boundary().InvokeAsync(approver);Assert.Equal(204,approver.Response.StatusCode);
    }
    [Theory]
    [InlineData("/api/marketing/automation/drafts", "POST")]
    [InlineData("/API/MARKETING/AUTOMATION/CAPTION", "POST")]
    [InlineData("/api/marketing/automation", "GET")]
    [InlineData("/api/marketing/catalogue/sync", "POST")]
    public async Task Draft_only_automation_and_reads_remain_available_to_editors(string path,string method)
    {
        var editor=Context(path,ticket:true,method:method);
        await Boundary(canApprove:false).InvokeAsync(editor);Assert.Equal(204,editor.Response.StatusCode);
    }
    [Theory]
    [InlineData("/api/marketing/inquiries/example", "PUT")]
    [InlineData("/api/marketing/operations/channels/instagram", "PUT")]
    [InlineData("/API/MARKETING/OPERATIONS/OBSERVATIONS", "POST")]
    public async Task Operations_writes_require_editor_but_do_not_grant_publishing(string path,string method)
    {
        var reader=Context(path,ticket:true,method:method);await Boundary(canEdit:false).InvokeAsync(reader);Assert.Equal(403,reader.Response.StatusCode);
        var editor=Context(path,ticket:true,method:method);await Boundary(canApprove:false).InvokeAsync(editor);Assert.Equal(204,editor.Response.StatusCode);
    }
    [Fact]
    public async Task Read_only_role_cannot_mutate_but_can_read()
    {var read=Context("/api/blog/articles",ticket:true);await Boundary(canEdit:false).InvokeAsync(read);Assert.Equal(204,read.Response.StatusCode);var write=Context("/api/blog/articles",ticket:true,method:"POST");await Boundary(canEdit:false).InvokeAsync(write);Assert.Equal(403,write.Response.StatusCode);}
    [Fact]
    public async Task Connected_private_requests_require_https()
    {var c=Context("/api/blog/articles",ticket:true);c.Request.Scheme="http";await Boundary().InvokeAsync(c);Assert.Equal(403,c.Response.StatusCode);}
    [Fact]
    public async Task Expired_and_revoked_tickets_are_rejected()
    {foreach(var expired in new[]{true,false}){var c=Context("/api/blog/articles",ticket:true);await Boundary(expired:expired,active:expired).InvokeAsync(c);Assert.Equal(401,c.Response.StatusCode);}}
    [Fact]
    public async Task Public_routes_remain_available_without_private_session()
    {var c=Context("/api/public/catalogue");await Boundary().InvokeAsync(c);Assert.Equal(204,c.Response.StatusCode);}
    private static DefaultHttpContext Context(string path,bool ticket=false,string method="GET")
    {
        var c=new DefaultHttpContext{RequestServices=new ServiceCollection().AddLogging().BuildServiceProvider()};
        c.Request.Path=path;c.Request.Method=method;c.Request.Scheme="https";c.Request.Host=new("localhost:8352");c.Response.Body=new MemoryStream();
        if(ticket)c.Request.Headers["X-Ocharo-Integration-Ticket"]="test-ticket";return c;
    }
    private static IntegrationBoundaryMiddleware Boundary(string origin="https://localhost:8264",string? actualOrg=null,string? configOrg=null,bool canEdit=true,bool canApprove=true,bool active=true,bool expired=false)
    {
        var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["PlatformIntegration:Mode"]="connected",["PlatformIntegration:BaseUrl"]=origin,["PlatformIntegration:OrganizationId"]=configOrg??Org}).Build();
        return new(c=>{c.Response.StatusCode=204;return Task.CompletedTask;},configuration,new Factory(new Handler(new { data=new { active,userId="test-member",organizationId=actualOrg??Org,canEdit,canApprove,expiresAt=DateTimeOffset.UtcNow.AddMinutes(expired?-1:5) } })));
    }
    private sealed class Factory(HttpMessageHandler handler):IHttpClientFactory{public HttpClient CreateClient(string name)=>new(handler);}
    private sealed class Handler(object value):HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(value)});}
}
