using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OcharoMarketing.Application.Inquiries;
namespace OcharoMarketing.Infrastructure.Inquiries;
public sealed class PublishedProductVerifier(HttpClient http,IConfiguration configuration):IPublishedProductVerifier
{
    public async Task<bool> ExistsAsync(string productId,CancellationToken ct)
    {
        var origin=configuration["Marketing:PlatformBaseUrl"]?.TrimEnd('/');if(origin is null)return false;
        var slug=Uri.EscapeDataString(configuration["Marketing:BrandSlug"]??"ocharo");
        try{var feed=await http.GetFromJsonAsync<JsonElement>($"{origin}/api/public/v1/brands/{slug}/catalogue?locale=en",ct);return feed.GetProperty("schemaVersion").GetInt32()==1&&feed.GetProperty("products").EnumerateArray().Any(p=>p.GetProperty("id").GetString()==productId);}
        catch(Exception e)when(e is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException or KeyNotFoundException){return false;}
    }
}
