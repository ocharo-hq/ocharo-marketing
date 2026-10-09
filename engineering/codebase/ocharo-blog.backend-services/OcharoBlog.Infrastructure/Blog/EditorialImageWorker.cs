using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Server-to-server delegation; browser never receives worker secrets or chooses remote URLs.</summary>
public sealed class EditorialImageWorker(IConfiguration config, IHttpContextAccessor accessor, IHttpClientFactory clients) : IEditorialImageWorker
{
    private bool SsoEnabled => config.GetValue<bool>("Identity:Sso:Enabled");
    private string? Organization => config[SsoEnabled ? "Identity:Sso:OrganizationId" : "PlatformIntegration:OrganizationId"];

    public async Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct)
    {
        if (!Configured(out var origin) || Ticket() is not { } ticket) return new(false);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            using var request = Request(HttpMethod.Get, origin!, "/api/integrations/media/background-removal/capabilities", ticket);
            using var response = await clients.CreateClient("ocharo-image-processing").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode) return new(false);
            var bytes = await ReadAsync(response.Content, 4096, deadline.Token);
            return JsonSerializer.Deserialize<ImageWorkerStatus>(bytes, BlogJson.Options) ?? new(false);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or BlogException || e is OperationCanceledException && !ct.IsCancellationRequested) { return new(false); }
    }

    public async Task<(byte[] Bytes, string Model)> RemoveAsync(byte[] bytes, string contentType, CancellationToken ct)
    {
        if (!Configured(out var origin) || Ticket() is not { } ticket) throw Unavailable();
        try
        {
            using var request = Request(HttpMethod.Post, origin!, "/api/integrations/media/background-removal", ticket);
            request.Content = new ByteArrayContent(bytes); request.Content.Headers.ContentType = new(contentType);
            using var response = await clients.CreateClient("ocharo-image-processing").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new BlogException("Your connection expired. Reconnect from Platform; your work is unchanged.", 401);
            if (response.StatusCode == HttpStatusCode.Forbidden) throw new BlogException("Editor access is required.", 403);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new BlogException("Image processing is busy. Try again shortly.", 429);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType != "image/png") throw Unavailable();
            var models = response.Headers.TryGetValues("X-Image-Model", out var values) ? values.ToArray() : [];
            if (models.Length != 1 || models[0].Length is 0 or > 100 || !models[0].All(c=>char.IsAsciiLetterOrDigit(c)||c is '-' or '_' or '.')) throw Unavailable();
            return (await ReadAsync(response.Content, MediaEditingService.MaximumBytes, ct), models[0]);
        }
        catch (Exception e) when (e is HttpRequestException || e is OperationCanceledException && !ct.IsCancellationRequested) { throw Unavailable(); }
    }

    public async Task EnsureEditableAsync(MediaEditActor actor, CancellationToken ct)
    {
        if (!SsoEnabled && (config["PlatformIntegration:Mode"] ?? "local") == "local")
        {
            if (actor != new MediaEditActor("local", "local-operator")) throw new BlogException("Untrusted image editor identity.", 401);
            return;
        }
        if (!Configured(out var origin) || Ticket() is not {} ticket
            || !Guid.TryParse(actor.OrganizationId, out var actorOrganization)
            || actorOrganization != Guid.Parse(Organization!))
            throw new BlogException("Reconnect from Platform.", 401);
        try
        {
            using var response = await clients.CreateClient("ocharo-platform-tickets").PostAsJsonAsync(origin!.TrimEnd('/')+"/api/integrations/introspect", new {token=ticket,audience=SsoEnabled ? "ocharo-editorial" : "ocharo-marketing"}, ct);
            if (!response.IsSuccessStatusCode) throw new BlogException("Reconnect from Platform.", 401);
            using var json = JsonDocument.Parse(await ReadAsync(response.Content, 8192, ct)); var data=json.RootElement.GetProperty("data");
            if (!data.GetProperty("active").GetBoolean() || data.GetProperty("organizationId").GetString()!=actor.OrganizationId
                || data.GetProperty("userId").GetString()!=actor.UserId || data.GetProperty("expiresAt").GetDateTimeOffset()<=DateTimeOffset.UtcNow)
                throw new BlogException("Your connection expired. Your original image is unchanged.", 401);
            if (!data.GetProperty("canEdit").GetBoolean()) throw new BlogException("Editor access is required.", 403);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException or FormatException || e is OperationCanceledException && !ct.IsCancellationRequested)
        { throw new BlogException("Could not confirm Platform access. Your original image is unchanged.", 401); }
    }

    private string? Ticket()
    {
        var http=accessor.HttpContext;
        var value = SsoEnabled ? http?.Items["Ocharo.Sso.Ticket"] as string
            : http?.Request.Headers["X-Ocharo-Integration-Ticket"].FirstOrDefault() ?? http?.Request.Cookies["_ocharo_editorial_ticket"];
        return value is {Length:>0 and <=4096} ? value : null;
    }
    private bool Configured(out string? origin)
    {
        origin=config[SsoEnabled ? "Identity:Sso:DeskOrigin" : "PlatformIntegration:BaseUrl"];
        return (SsoEnabled || config["PlatformIntegration:Mode"]=="connected") && Guid.TryParse(Organization,out var organization) && organization != Guid.Empty
            && Uri.TryCreate(origin,UriKind.Absolute,out var uri) && uri.Scheme=="https" && uri.UserInfo.Length==0 && uri.Query.Length==0 && uri.Fragment.Length==0;
    }
    private static HttpRequestMessage Request(HttpMethod method,string origin,string path,string ticket)
    {var request=new HttpRequestMessage(method,origin.TrimEnd('/')+path);request.Headers.Add("X-Ocharo-Integration-Ticket",ticket);return request;}
    private static async Task<byte[]> ReadAsync(HttpContent content,int maximum,CancellationToken ct)
    {
        if(content.Headers.ContentLength>maximum)throw Unavailable();
        await using var stream=await content.ReadAsStreamAsync(ct);using var buffer=new MemoryStream();var chunk=new byte[8192];int count;
        while((count=await stream.ReadAsync(chunk,ct))>0){if(buffer.Length+count>maximum)throw Unavailable();buffer.Write(chunk,0,count);}
        return buffer.ToArray();
    }
    private static BlogException Unavailable()=>new("Background removal is unavailable. Your original image is unchanged.",503);
}
