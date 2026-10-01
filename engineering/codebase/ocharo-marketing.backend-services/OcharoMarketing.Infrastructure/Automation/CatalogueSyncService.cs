using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Infrastructure.Marketing;

namespace OcharoMarketing.Infrastructure.Automation;

/// <summary>
/// Pulls active products from the Ocharo Catalogue public feed: copy in three languages, sizes, prices and the
/// front cutout (hash-verified, stored once as an immutable asset). Catalogue stays the source of truth.
/// </summary>
public sealed class CatalogueSyncService(IMarketingRepository repository, IAssetService assets, HttpClient http, IOptions<MarketingOptions> options)
{
    public async Task<CatalogueSyncReport> SyncAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(options.Value.PlatformBaseUrl)) return await SyncPlatformAsync(ct);
        var origin = options.Value.CatalogueBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(origin)) throw new MarketingException("Katalog manzili sozlanmagan (Marketing:CatalogueBaseUrl).", 409);
        JsonElement feed;
        try { feed = await http.GetFromJsonAsync<JsonElement>($"{origin}/api/public/catalogue?locale=uz", ct); }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new MarketingException($"Ocharo Katalogga ulanib bo‘lmadi ({origin}). Katalog xizmati ishlayotganini tekshiring.", 502);
        }

        var warnings = new List<string>();
        var created = new List<string>();
        int received = 0, updated = 0, images = 0;
        foreach (var item in feed.GetProperty("products").EnumerateArray())
        {
            received++;
            var id = item.GetProperty("id").GetString()!;
            var imageList = item.GetProperty("images").EnumerateArray().ToList();
            var hero = imageList.FirstOrDefault(i => i.GetProperty("role").GetString() == "cutout");
            if (hero.ValueKind != JsonValueKind.Object) hero = imageList.FirstOrDefault();
            if (hero.ValueKind != JsonValueKind.Object) { warnings.Add($"{id}: rasmsiz, o‘tkazib yuborildi"); continue; }

            var heroAsset = await StoreAsync(origin, id, "front", hero, ct);
            if (heroAsset is null) { warnings.Add($"{id}: asosiy rasm yuklanmadi yoki xeshi mos kelmadi"); continue; }
            images += heroAsset.Value.Stored ? 1 : 0;
            var assetIds = new List<string> { heroAsset.Value.Id };
            var model = imageList.FirstOrDefault(i => i.GetProperty("role").GetString() == "model");
            if (model.ValueKind == JsonValueKind.Object && await StoreAsync(origin, id, "reference", model, ct) is { } modelAsset)
            {
                assetIds.Add(modelAsset.Id);
                images += modelAsset.Stored ? 1 : 0;
            }

            var pricing = item.GetProperty("pricing");
            decimal? retail = pricing.ValueKind == JsonValueKind.Object && pricing.GetProperty("retail").ValueKind == JsonValueKind.Number ? pricing.GetProperty("retail").GetDecimal() : null;
            decimal? wholesale = pricing.ValueKind == JsonValueKind.Object && pricing.GetProperty("wholesale").ValueKind == JsonValueKind.Number ? pricing.GetProperty("wholesale").GetDecimal() : null;
            var copy = new Dictionary<string, ProductCopyModel>();
            foreach (var locale in item.GetProperty("locales").EnumerateObject())
                copy[locale.Name] = new ProductCopyModel(Text(locale.Value, "status") ?? "draft", Text(locale.Value, "name"), Text(locale.Value, "fabric"),
                    Text(locale.Value, "decoration"), Text(locale.Value, "description"));
            var approved = copy.Values.Count(c => c.Status == "approved");
            var product = new ProductModel(id, Text(item, "name") ?? id, Text(item, "garmentType") ?? "", Text(item, "fabric") ?? "", Text(item, "description") ?? "",
                heroAsset.Value.Id, [.. assetIds], item.GetProperty("sizes").EnumerateArray().Select(s => s.GetString()!).ToArray(), retail, "UZS",
                approved == 3 ? "approved-copy" : "needs-review", $"catalogue:{id}:r{item.GetProperty("revision").GetInt32()}",
                approved == 3 ? [] : [$"Matn tasdig‘i: {approved}/3 til (katalogda)."], copy, wholesale,
                Text(item, "availability"),
                item.TryGetProperty("availableSizes", out var sizes) && sizes.ValueKind == JsonValueKind.Array ? sizes.EnumerateArray().Select(s => s.GetString()!).ToArray() : null);
            if (await repository.UpsertProductAsync(product, ct)) created.Add(id);
            else updated++;
        }

        return new CatalogueSyncReport(received, created.Count, updated, images, [.. created], [], [.. warnings]);
    }

    private async Task<CatalogueSyncReport> SyncPlatformAsync(CancellationToken ct)
    {
        var origin=options.Value.PlatformBaseUrl!.TrimEnd('/');
        if(!Uri.TryCreate(origin,UriKind.Absolute,out var baseUri)||baseUri.Scheme!="https")
            throw new MarketingException("The canonical Platform URL must use HTTPS.",409);
        var slug=Uri.EscapeDataString(options.Value.BrandSlug);
        JsonElement feed;
        try{feed=await http.GetFromJsonAsync<JsonElement>($"{origin}/api/public/v1/brands/{slug}/catalogue?locale=uz",ct);}
        catch(Exception e)when(e is HttpRequestException or TaskCanceledException or JsonException)
        {throw new MarketingException("The published Platform catalogue is unavailable. Existing drafts are preserved.",502);}
        if(!feed.TryGetProperty("schemaVersion",out var schema)||schema.GetInt32()!=1||!feed.TryGetProperty("release",out var release)
            ||!release.TryGetProperty("id",out var releaseId)||!Guid.TryParse(releaseId.GetString(),out var revision))
            throw new MarketingException("Platform returned no supported published release.",502);
        var created=new List<string>();var warnings=new List<string>();int received=0,updated=0,images=0;
        foreach(var item in feed.GetProperty("products").EnumerateArray())
        {
            received++;var id=Text(item,"id")!;
            var publicImages=item.GetProperty("images").EnumerateArray().Where(i=>i.TryGetProperty("provenance",out var p)&&p.TryGetProperty("approvedForPublication",out var a)&&a.ValueKind==JsonValueKind.True).ToList();
            var hero=publicImages.FirstOrDefault();if(hero.ValueKind!=JsonValueKind.Object){warnings.Add($"{id}: no approved image");continue;}
            var image=await StoreAsync(origin,id,"front",hero,ct);if(image is null){warnings.Add($"{id}: image integrity unavailable");continue;}
            if(image.Value.Stored)images++;
            decimal? amount=null;string currency="UZS";
            if(item.TryGetProperty("price",out var price)&&price.ValueKind==JsonValueKind.Object){if(price.TryGetProperty("amount",out var number)&&number.TryGetDecimal(out var value))amount=value;currency=Text(price,"currency")??currency;}
            var name=Text(item,"title")??id;var fabric=Text(item,"fabric")??"";var description=Text(item,"description")??"";
            var copy=new Dictionary<string,ProductCopyModel>{{"uz",new("approved",name,fabric,Text(item,"decoration"),description)}};
            var product=new ProductModel(id,name,Text(item,"category")??"",fabric,description,image.Value.Id,[image.Value.Id],
                item.GetProperty("sizes").EnumerateArray().Select(x=>x.GetString()!).ToArray(),amount,currency,"approved-copy",
                $"platform:{revision}:product:{id}",[],copy,null,Text(item,"availability"));
            if(await repository.UpsertProductAsync(product,ct))created.Add(id);else updated++;
        }
        return new CatalogueSyncReport(received,created.Count,updated,images,[..created],[],[..warnings]);
    }

    private async Task<(string Id, bool Stored)?> StoreAsync(string origin, string productId, string role, JsonElement image, CancellationToken ct)
    {
        var url = image.GetProperty("url").GetString();
        var sha = image.GetProperty("sha256").GetString();
        if (url is null || sha is null) return null;
        byte[] bytes;
        if(!Uri.TryCreate(new Uri(origin+"/"),url,out var assetUri)||assetUri.GetLeftPart(UriPartial.Authority)!=new Uri(origin).GetLeftPart(UriPartial.Authority))return null;
        try { bytes = await http.GetByteArrayAsync(assetUri, ct); }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException) { return null; }
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != sha) return null;
        var baseId = $"{productId}-{(role == "front" ? "front" : "model")}";
        var fileName = Path.GetFileName(assetUri.AbsolutePath) + (bytes.Length > 4 && bytes[0] == 0x89 ? ".png" : ".jpg");
        var provenance = $"Ocharo Catalogue {productId} · sha256 {sha}";
        var existing = await repository.FindAssetAsync(baseId, ct);
        foreach (var candidate in new[] { baseId, $"{baseId}-{sha[..8]}" })
        {
            try
            {
                var before = await repository.FindAssetAsync(candidate, ct);
                var asset = await assets.SaveAsync(bytes, fileName, provenance, role, ct, candidate);
                return (asset.Id, before is null);
            }
            catch (MarketingException exception) when (exception.StatusCode == 409 && existing is not null)
            {
                // The stable id already holds older bytes; the hash-suffixed id keeps both immutable.
            }
        }

        return null;
    }

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString() : null;
}
