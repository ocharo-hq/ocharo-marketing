using Ocharo.Sso;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace OcharoBlog.Api.Blog;

/// <summary>One configured organization's operator instance, authenticated by revocable Platform audience tickets.</summary>
public sealed class IntegrationBoundaryMiddleware(RequestDelegate next,IConfiguration configuration,IHttpClientFactory clients)
{
    private const string Cookie = "_ocharo_editorial_ticket";
    private const string Audience = "ocharo-marketing";
    private bool SsoEnabled => configuration.GetValue<bool>("Identity:Sso:Enabled");
    private string Mode => configuration["PlatformIntegration:Mode"] ?? "local";

    public async Task InvokeAsync(HttpContext context)
    {
        var path=(context.Request.Path.Value??"/").ToLowerInvariant();
        if(path.StartsWith("/api/",StringComparison.Ordinal)||path.StartsWith("/preview",StringComparison.Ordinal)||path.StartsWith("/media/",StringComparison.Ordinal))NoStore(context);
        var writes=!HttpMethods.IsGet(context.Request.Method)&&!HttpMethods.IsHead(context.Request.Method)&&!HttpMethods.IsOptions(context.Request.Method);
        if(Mode!="local" && writes && !SameOrigin(context.Request))
        {await Reject(context,403,"Cross-origin writes are disabled.");return;}
        // Technical status is public in every integration mode; business APIs remain fail-closed.
        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && path.TrimEnd('/') is "/health" or "/api/system/status")
        {
            await next(context);
            return;
        }
        if(path is "/api/integrations/session" or "/api/integrations/connect")
        {
            if (SsoEnabled)
            {
                if (path != "/api/integrations/session" || !HttpMethods.IsGet(context.Request.Method))
                { await Reject(context, 409, "Use Ocharo sign-in and sign-out for this application."); return; }
                var current = VerifiedSsoSession(context);
                await context.Response.WriteAsJsonAsync(new { mode = "sso", active = current is not null,
                    configured = true, canEdit = current?.CanEdit ?? false, canApprove = current?.CanApprove ?? false,
                    expiresAt = current?.ExpiresAt }, context.RequestAborted);
                return;
            }
            NoStore(context);
            if(path.EndsWith("connect",StringComparison.Ordinal)&&HttpMethods.IsDelete(context.Request.Method))
            {context.Response.Cookies.Delete(Cookie);context.Response.StatusCode=204;return;}
            string? token=null;
            if(path.EndsWith("connect",StringComparison.Ordinal)&&HttpMethods.IsPost(context.Request.Method))
            {
                try{var body=await context.Request.ReadFromJsonAsync<TicketInput>(context.RequestAborted);token=body?.Ticket;}
                catch(JsonException){await Reject(context,400,"Provide the Platform ticket.");return;}
            }
            if(Mode=="local")
            {await context.Response.WriteAsJsonAsync(new{mode="local",active=true,canEdit=true,canApprove=true,configured=true},context.RequestAborted);return;}
            if(!context.Request.IsHttps){await Reject(context,403,"Connected access requires HTTPS.");return;}
            var session=await ValidateAsync(context,token);
            if(session is null)
            {
                if(HttpMethods.IsGet(context.Request.Method)){await context.Response.WriteAsJsonAsync(new{mode=Mode,active=false,configured=Configured},context.RequestAborted);return;}
                await Reject(context,Configured?401:503,Configured?"The ticket is expired, revoked or belongs to another organization.":"This Marketing instance needs its Platform URL and organization binding.");return;
            }
            if(token is not null)
                context.Response.Cookies.Append(Cookie,token,new CookieOptions{HttpOnly=true,Secure=true,SameSite=SameSiteMode.Strict,Path="/",Expires=session.ExpiresAt,IsEssential=true});
            await context.Response.WriteAsJsonAsync(new{mode=Mode,active=true,configured=true,canEdit=session.CanEdit,canApprove=session.CanApprove,expiresAt=session.ExpiresAt},context.RequestAborted);return;
        }
        var privateRoute=(path=="/api"||path.StartsWith("/api/",StringComparison.Ordinal))&&!path.StartsWith("/api/public/",StringComparison.Ordinal)||path=="/preview"||path.StartsWith("/preview/",StringComparison.Ordinal);
        if (path.StartsWith("/media/",StringComparison.Ordinal))
        {
            var sha=path.Split('/').ElementAtOrDefault(2);
            var repo=context.RequestServices.GetRequiredService<OcharoBlog.Application.Blog.Repositories.IBlogRepository>();
            var settings=await context.RequestServices.GetRequiredService<OcharoBlog.Application.Blog.Services.SiteSettingsService>().GetAsync(context.RequestAborted);
            var publicMedia=new[]{settings.Settings.LogoSha256,settings.Settings.HeroSha256,settings.Settings.BandSha256}.Contains(sha)
                ||(await repo.GetArticlesAsync(context.RequestAborted)).Where(r=>r.Status=="published")
                    .Select(r=>OcharoBlog.Application.Blog.BlogJson.Read<OcharoBlog.Domain.Blog.ArticleDocument>(r.Json)).Any(d=>d.Cover?.MediaSha256==sha);
            privateRoute=!publicMedia;
        }
        if(!privateRoute){await next(context);return;}
        NoStore(context);
        Session? principal;
        if (SsoEnabled)
        {
            principal = VerifiedSsoSession(context);
        }
        else
        {
            if(Mode=="local"){context.Items["Ocharo.CanApprove"]=true;await next(context);return;}
            if(!context.Request.IsHttps){await Reject(context,403,"Connected access requires HTTPS.");return;}
            if(!Configured){await Reject(context,503,"This Marketing instance needs its Platform URL and organization binding.");return;}
            principal = await ValidateAsync(context);
        }
        if(principal is null){await Reject(context,401,"Connect a current Ocharo Platform ticket for this organization.");return;}
        var approval=path.Split('/').Any(p=>p is "publish" or "unpublish" or "archive" or "schedule" or "approve" or "restore" or "queue" or "retry" or "dispatch" or "settings" or "cancel" or "reconcile");
        if(writes&&(!principal.CanEdit||approval&&!principal.CanApprove)){await Reject(context,403,"Your Platform role does not permit this action.");return;}
        context.Items["Ocharo.CanApprove"]=principal.CanApprove;
        context.Items["Ocharo.OrganizationId"]=principal.OrganizationId;
        context.Items["Ocharo.UserId"]=principal.UserId;
        await next(context);
    }
    private Session? VerifiedSsoSession(HttpContext context)
    {
        if (context.User.Identity?.AuthenticationType != SsoContext.AuthenticationType
            || context.Items[SsoContext.ActorKey] is not SsoActor { Active: true } actor
            || actor.ExpiresAt <= DateTimeOffset.UtcNow
            || !Guid.TryParse(configuration["Identity:Sso:OrganizationId"], out var organization)
            || organization == Guid.Empty || actor.OrganizationId != organization) return null;
        return new Session(actor.OrganizationId.ToString("D"), actor.UserId, actor.CanEdit, actor.CanApprove, actor.ExpiresAt);
    }
    private bool Configured=>Mode=="connected"&&Guid.TryParse(configuration["PlatformIntegration:OrganizationId"],out _)
        &&Uri.TryCreate(configuration["PlatformIntegration:BaseUrl"],UriKind.Absolute,out var u)&&u.Scheme == "https" && u.UserInfo.Length == 0 && u.Query.Length == 0 && u.Fragment.Length == 0;
    private async Task<Session?> ValidateAsync(HttpContext context,string? supplied=null)
    {
        if(!Configured)return null;
        var token=supplied??context.Request.Headers["X-Ocharo-Integration-Ticket"].FirstOrDefault()??context.Request.Cookies[Cookie];
        if(string.IsNullOrWhiteSpace(token)||token.Length>4096)return null;
        try
        {
            var url=configuration["PlatformIntegration:BaseUrl"]!.TrimEnd('/')+"/api/integrations/introspect";
            using var response=await clients.CreateClient("ocharo-platform-tickets").PostAsJsonAsync(url,new{token,audience=Audience},context.RequestAborted);
            if(!response.IsSuccessStatusCode)return null;
            var json=await response.Content.ReadFromJsonAsync<JsonElement>(context.RequestAborted);var data=json.GetProperty("data");
            if(!data.GetProperty("active").GetBoolean())return null;
            var org=data.GetProperty("organizationId").GetString();
            if(!Guid.TryParse(org,out var actual)||actual!=Guid.Parse(configuration["PlatformIntegration:OrganizationId"]!))return null;
            var expires=data.GetProperty("expiresAt").GetDateTimeOffset();if(expires<=DateTimeOffset.UtcNow)return null;
            var userId=data.GetProperty("userId").GetString();if(string.IsNullOrWhiteSpace(userId))return null;
            return new(org!,userId,data.GetProperty("canEdit").GetBoolean(),data.GetProperty("canApprove").GetBoolean(),expires);
        }
        catch(Exception e)when(e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException){return null;}
    }
    private static void NoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl="no-store";
        context.Response.OnStarting(()=>{context.Response.Headers.CacheControl="no-store";return Task.CompletedTask;});
    }
    private static bool SameOrigin(HttpRequest request)
    {
        var origin=request.Headers.Origin.ToString();
        if(origin.Length==0)return !request.Headers.ContainsKey("Sec-Fetch-Site") || request.Headers["Sec-Fetch-Site"]=="same-origin";
        return Uri.TryCreate(origin,UriKind.Absolute,out var uri) && uri.Scheme=="https" && uri.UserInfo.Length==0
            && uri.AbsolutePath=="/" && uri.Query.Length==0 && uri.Fragment.Length==0
            && string.Equals(uri.GetLeftPart(UriPartial.Authority),$"{request.Scheme}://{request.Host.Value}",StringComparison.OrdinalIgnoreCase);
    }
    private static Task Reject(HttpContext context,int status,string detail)=>Results.Problem(statusCode:status,title:"Ocharo Platform access",detail:detail).ExecuteAsync(context);
    private sealed record TicketInput(string Ticket);
    private sealed record Session(string OrganizationId,string UserId,bool CanEdit,bool CanApprove,DateTimeOffset ExpiresAt);
}
