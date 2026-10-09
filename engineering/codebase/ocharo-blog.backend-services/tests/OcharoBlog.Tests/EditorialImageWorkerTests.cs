using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Infrastructure.Blog;
using Xunit;

namespace OcharoBlog.Tests;

public sealed class EditorialImageWorkerTests
{
    private static readonly string Org=Guid.NewGuid().ToString();
    [Fact]
    public async Task Capabilities_without_connected_ticket_never_calls_platform()
    {
        var service=Build((_,_)=>throw new InvalidOperationException(),ticket:false);
        Assert.False((await service.GetStatusAsync(default)).Available);
        var local=Build((_,_)=>throw new InvalidOperationException(),mode:"local");
        Assert.False((await local.GetStatusAsync(default)).Available);
    }
    [Fact]
    public async Task Capabilities_use_fixed_private_endpoint_and_delegated_ticket_only()
    {
        var service=Build((request,_)=>{
            Assert.Equal("https://platform.test/api/integrations/media/background-removal/capabilities",request.RequestUri!.ToString());
            Assert.Equal("unit-test-ticket",Assert.Single(request.Headers.GetValues("X-Ocharo-Integration-Ticket")));
            Assert.False(request.Headers.Contains("X-Image-Worker-Key"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new{available=true,model="u2net"})});
        });
        Assert.Equal(new ImageWorkerStatus(true,"u2net"),await service.GetStatusAsync(default));
    }
    [Fact]
    public async Task Foreign_actor_organization_is_rejected_before_network()
    {
        var service=Build((_,_)=>throw new InvalidOperationException());
        var error=await Assert.ThrowsAsync<BlogException>(()=>service.EnsureEditableAsync(new(Guid.NewGuid().ToString(),"operator"),default));
        Assert.Equal(401,error.StatusCode);
    }
    [Theory]
    [InlineData(false,true,401)]
    [InlineData(true,false,403)]
    public async Task Final_authorization_rechecks_session_and_edit_role(bool active,bool canEdit,int status)
    {
        var service=Build((request,_)=>{
            Assert.Equal("/api/integrations/introspect",request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=JsonContent.Create(new{data=new{active,canEdit,userId="operator",organizationId=Org,expiresAt=DateTimeOffset.UtcNow.AddMinutes(2)}})});
        });
        var error=await Assert.ThrowsAsync<BlogException>(()=>service.EnsureEditableAsync(new(Org,"operator"),default));
        Assert.Equal(status,error.StatusCode);
    }
    [Fact]
    public async Task Redirect_and_invalid_capability_responses_fail_closed()
    {
        var redirect=Build((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)));
        Assert.False((await redirect.GetStatusAsync(default)).Available);
        var invalid=Build((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new string('x',4097))}));
        Assert.False((await invalid.GetStatusAsync(default)).Available);
    }
    [Fact]
    public async Task Sso_uses_server_ticket_and_editorial_audience_even_with_legacy_local_mode()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["PlatformIntegration:Mode"] = "local", ["Identity:Sso:Enabled"] = "true",
            ["Identity:Sso:DeskOrigin"] = "https://desk.test", ["Identity:Sso:OrganizationId"] = Org }).Build();
        var context = new DefaultHttpContext();
        context.Items["Ocharo.Sso.Ticket"] = "server-only-ticket";
        context.Request.Headers["X-Ocharo-Integration-Ticket"] = "untrusted-browser-ticket";
        var service = new EditorialImageWorker(config, new HttpContextAccessor { HttpContext=context }, new Factory(async (request, ct) => {
            Assert.Equal("https://desk.test/api/integrations/introspect", request.RequestUri!.ToString());
            var input = await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken:ct);
            Assert.Equal("server-only-ticket", input.GetProperty("token").GetString());
            Assert.Equal("ocharo-editorial", input.GetProperty("audience").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content=JsonContent.Create(new { data=new { active=true, canEdit=true, userId="operator", organizationId=Org, expiresAt=DateTimeOffset.UtcNow.AddMinutes(2) } }) };
        }));
        await service.EnsureEditableAsync(new(Org, "operator"), default);
        var denied = await Assert.ThrowsAsync<BlogException>(() => service.EnsureEditableAsync(new("local", "local-operator"), default));
        Assert.Equal(401, denied.StatusCode);
    }

    private static EditorialImageWorker Build(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send,bool ticket=true,string mode="connected")
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["PlatformIntegration:Mode"]=mode,["PlatformIntegration:BaseUrl"]="https://platform.test",["PlatformIntegration:OrganizationId"]=Org}).Build();
        var context=new DefaultHttpContext();if(ticket)context.Request.Headers["X-Ocharo-Integration-Ticket"]="unit-test-ticket";
        return new(config,new HttpContextAccessor{HttpContext=context},new Factory(send));
    }
    private sealed class Factory(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send):IHttpClientFactory
    {public HttpClient CreateClient(string name)=>new(new Handler(send));}
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> send):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>send(request,ct);}
}
