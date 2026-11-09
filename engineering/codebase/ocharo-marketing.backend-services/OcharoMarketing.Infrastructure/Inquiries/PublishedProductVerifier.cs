using System.Net.Http.Json;
using System.Text.Json;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Infrastructure.Core;
namespace OcharoMarketing.Infrastructure.Inquiries;
public sealed class PublishedProductVerifier(HttpClient http, MarketingOptions marketing) : IPublishedProductVerifier
{
    public async Task<bool> ExistsAsync(string productId,CancellationToken ct)
    {
        var origin = marketing.PlatformBaseUrl?.TrimEnd('/');
        if (origin is null) return false;
        var slug = Uri.EscapeDataString(marketing.BrandSlug);
        try
        {
            var feed=await http.GetFromJsonAsync<JsonElement>(
                $"{origin}/api/public/v1/brands/{slug}/catalogue?locale=en",
                ct);
            return feed.GetProperty("schemaVersion").GetInt32()==1
                &&feed.GetProperty("products").EnumerateArray().Any(p=>p.GetProperty("id").GetString()==productId);
        }
        catch(
            Exception e)when(
                e is HttpRequestException
                    or JsonException
                    or TaskCanceledException
                    or InvalidOperationException
                    or KeyNotFoundException)
        {
            return false;
        }
    }
}
