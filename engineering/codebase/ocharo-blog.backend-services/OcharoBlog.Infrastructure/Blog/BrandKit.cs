using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Published Platform identity wins; legacy kit is supported only when canonical Platform is unconfigured.</summary>
public sealed class BrandKit(HttpClient http,IOptions<BlogOptions> options,ILogger<BrandKit> logger):IBrandKit
{
    public async Task<BrandSnapshot?> GetAsync(CancellationToken ct)
    {
        var canonical=!string.IsNullOrWhiteSpace(options.Value.PlatformBaseUrl);var origin=(canonical?options.Value.PlatformBaseUrl:options.Value.BrandBaseUrl)?.TrimEnd('/');if(string.IsNullOrWhiteSpace(origin))return null;
        try
        {
            var path=canonical?$"/api/public/v1/brands/{Uri.EscapeDataString(options.Value.BrandSlug)}":"/api/public/brand?app=blog";
            var kit=await http.GetFromJsonAsync<JsonElement>(origin+path,ct);var identity=canonical?kit.GetProperty("identity"):kit;
            if(canonical&&(kit.GetProperty("schemaVersion").GetInt32()!=1||!Guid.TryParse(kit.GetProperty("release").GetProperty("id").GetString(),out _)))return null;
            var logos=identity.GetProperty("logos");var logo=logos.TryGetProperty("on-light",out var light)?light:logos.TryGetProperty("master",out var master)?master:default;
            if(canonical&&logo.ValueKind==JsonValueKind.Object&&(!logo.TryGetProperty("provenance",out var p)||!p.TryGetProperty("approvedForPublication",out var approved)||approved.ValueKind!=JsonValueKind.True))logo=default;
            var links=identity.GetProperty("links").EnumerateArray().Where(l=>!canonical||l.TryGetProperty("verified",out var v)&&v.ValueKind==JsonValueKind.True)
                .Select(l=>new SiteLink(l.GetProperty("label").GetString()??"",l.GetProperty("url").GetString()??""))
                .Where(l=>l.Label.Length>0&&Uri.TryCreate(l.Url,UriKind.Absolute,out var u)&&u.Scheme=="https").ToList();
            var release=canonical?kit.GetProperty("release").GetProperty("revision").GetInt32():kit.GetProperty("release").GetInt32();
            var sha=logo.ValueKind==JsonValueKind.Object?logo.GetProperty("sha256").GetString():null;
            var location=logo.ValueKind==JsonValueKind.Object?logo.GetProperty(canonical?"url":"fileUrl").GetString():null;
            var url=location is not null&&Uri.TryCreate(new Uri(origin+"/"),location,out var absolute)&&absolute.Authority==new Uri(origin).Authority?absolute.ToString():null;
            return new(release,sha,url,links,canonical?null:Event(kit));
        }
        catch(Exception e)when(e is HttpRequestException or JsonException or TaskCanceledException or KeyNotFoundException or InvalidOperationException or FormatException){logger.LogWarning("Published brand unavailable at {Origin}: {Message}",origin,e.Message);return null;}
    }
    public async Task<byte[]?> DownloadAsync(string url,CancellationToken ct)
    {
        var origin=options.Value.PlatformBaseUrl??options.Value.BrandBaseUrl;if(origin is null||!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Authority!=new Uri(origin).Authority||u.Scheme is not("https" or "http"))return null;
        try{return await http.GetByteArrayAsync(url,ct);}catch(Exception e)when(e is HttpRequestException or TaskCanceledException){return null;}
    }
    private static BrandEvent? Event(JsonElement kit)
    {
        if(!kit.TryGetProperty("appearance",out var a)||a.ValueKind!=JsonValueKind.Object)return null;
        var label=a.TryGetProperty("linkLabel",out var l)?l.GetString():null;var url=a.TryGetProperty("linkUrl",out var u)?u.GetString():null;
        if(string.IsNullOrWhiteSpace(label)||url is null||!url.StartsWith("https://",StringComparison.Ordinal))return null;
        return new(a.GetProperty("name").GetString()??label,label,url,a.GetProperty("endsAt").GetDateTimeOffset());
    }
}
