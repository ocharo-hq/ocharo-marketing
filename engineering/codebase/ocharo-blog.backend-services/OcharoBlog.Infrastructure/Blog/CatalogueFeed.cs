using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Reads a published Platform release; failures never re-expose a withdrawn cached release.</summary>
public sealed class CatalogueFeed(HttpClient http, IOptions<BlogOptions> options, ILogger<CatalogueFeed> logger) : ICatalogueFeed
{
    public async Task<IReadOnlyList<CatalogueProduct>> GetProductsAsync(CancellationToken ct) => (await GetCatalogueAsync(ct)).Products;

    public async Task<CatalogueSnapshot> GetCatalogueAsync(CancellationToken ct, string locale = "en")
    {
        locale = locale is "uz" or "ru" ? locale : "en";
        var canonical = !string.IsNullOrWhiteSpace(options.Value.PlatformBaseUrl);
        var origin = (canonical ? options.Value.PlatformBaseUrl : options.Value.CatalogueBaseUrl)?.TrimEnd('/');
        if (string.IsNullOrEmpty(origin)) return new([]);
        try
        {
            var path = canonical ? $"/api/public/v1/brands/{Uri.EscapeDataString(options.Value.BrandSlug)}/catalogue?locale={locale}" : $"/api/public/catalogue?locale={locale}";
            var feed = await http.GetFromJsonAsync<JsonElement>(origin + path, ct);
            if (canonical && (!feed.TryGetProperty("schemaVersion", out var version) || version.GetInt32() != 1
                || !feed.TryGetProperty("release", out var release) || release.ValueKind != JsonValueKind.Object
                || !release.TryGetProperty("id", out var releaseId) || !Guid.TryParse(releaseId.GetString(), out _))) return new([]);
            var products = feed.GetProperty("products").EnumerateArray().Select(p => Parse(p, origin, canonical)).Where(p=>p is not null).Cast<CatalogueProduct>().ToList();
            return new(products, canonical ? ParseEdition(feed, origin, options.Value.BrandSlug, locale) : null);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            logger.LogWarning("Published catalogue unavailable at {Origin}: {Message}", origin, e.Message);
            return new([]);
        }
    }
    private static CatalogueEdition? ParseEdition(JsonElement feed, string origin, string brandSlug, string locale)
    {
        if (!feed.TryGetProperty("edition", out var edition) || edition.ValueKind != JsonValueKind.Object) return null;
        try
        {
            if (Text(edition, "locale") != locale) return null;
            var id = Text(edition, "id");
            if (!Guid.TryParse(id, out _) || Text(edition, "title") is not { Length: > 0 } title
                || !edition.TryGetProperty("pages", out var source) || source.ValueKind != JsonValueKind.Array) return null;
            var prefix = $"/api/public/v1/brands/{Uri.EscapeDataString(brandSlug)}/catalogue-editions/{id}/";
            var releaseId = Text(feed.GetProperty("release"), "id");
            if (!Guid.TryParse(releaseId, out _)) return null;
            string? Asset(string? value, string file)
            {
                var url = Url(origin, value);
                return url is not null && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && uri.UserInfo.Length == 0 && uri.Query == "?release=" + releaseId && uri.Fragment.Length == 0
                    && uri.AbsolutePath == prefix + file ? url : null;
            }
            var pdf = Asset(Text(edition, "pdfUrl"), "catalogue.pdf");
            if (pdf is null) return null;
            var pages = new List<CatalogueEditionPage>();
            foreach (var page in source.EnumerateArray())
            {
                var pageId = Text(page, "id");
                if (pageId is null || pageId.Length is < 1 or > 100 || pageId.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_'))) return null;
                var image = Asset(Text(page, "imageUrl"), pageId + ".png");
                var png = Asset(Text(page, "pngUrl"), pageId + ".png");
                var pagePdf = Asset(Text(page, "pdfUrl"), pageId + ".pdf");
                var number = page.GetProperty("number").GetInt32();
                var width = page.GetProperty("widthMm").GetDecimal();
                var height = page.GetProperty("heightMm").GetDecimal();
                if (image is null || png is null || pagePdf is null || number != pages.Count + 1 || width <= 0 || height <= 0) return null;
                pages.Add(new(pageId!, number, Text(page, "title") ?? $"Page {number}", width, height,
                    Strings(page, "productIds"), image, pagePdf, png));
            }
            if (pages.Count == 0 || pages.Count > 250 || edition.GetProperty("pageCount").GetInt32() != pages.Count
                || pages.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != pages.Count) return null;
            return new(id!, Text(edition, "brandId") ?? "", Text(edition, "sourceDesignId") ?? "",
                edition.GetProperty("sourceRevision").GetInt32(), title, Text(edition, "locale") ?? "en",
                Text(edition, "templateId") ?? "", pages.Count, edition.GetProperty("releasedAt").GetDateTimeOffset(), pdf, pages);
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            return null; // A malformed presentation never grants asset access or suppresses valid product facts.
        }
    }
    private static string? Text(JsonElement e,string name)=>e.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
    private static IReadOnlyList<string> Strings(JsonElement e,string name)=>e.TryGetProperty(name,out var a)&&a.ValueKind==JsonValueKind.Array?a.EnumerateArray().Where(v=>v.ValueKind==JsonValueKind.String).Select(v=>v.GetString()!).ToList():[];
    private static string? Url(string origin,string? value)=>value is not null&&Uri.TryCreate(new Uri(origin+"/"),value,out var uri)&&uri.Scheme is "https" or "http"&&uri.Authority==new Uri(origin).Authority?uri.ToString():null;
    private static CatalogueProduct? Parse(JsonElement p,string origin,bool canonical)
    {
        var id=Text(p,"id");var name=Text(p,canonical?"title":"name");if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(name))return null;
        var images=new List<CatalogueImage>();
        foreach(var image in p.GetProperty("images").EnumerateArray())
        {
            var evidence=image.TryGetProperty("provenance",out var provenance)&&provenance.ValueKind==JsonValueKind.Object?provenance:default;
            if(canonical&&(evidence.ValueKind!=JsonValueKind.Object||!evidence.TryGetProperty("approvedForPublication",out var approved)||approved.ValueKind!=JsonValueKind.True))continue;
            var url=Url(origin,Text(image,"url"));if(url is null)continue;
            images.Add(new CatalogueImage(url,Text(image,"alt")??name,evidence.ValueKind==JsonValueKind.Object?Text(evidence,"kind"):null,evidence.ValueKind==JsonValueKind.Object?Text(evidence,"credit"):null,evidence.ValueKind==JsonValueKind.Object?Text(evidence,"rights"):null));
        }
        long? oldPrice=null;decimal? price=null;string? currency=null;
        if(canonical&&p.TryGetProperty("price",out var modern)&&modern.ValueKind==JsonValueKind.Object&&modern.TryGetProperty("amount",out var amount)&&amount.TryGetDecimal(out var a)){price=a;currency=Text(modern,"currency");}
        if(!canonical&&p.TryGetProperty("pricing",out var pricing)&&pricing.ValueKind==JsonValueKind.Object&&pricing.TryGetProperty("retail",out var retail)&&retail.TryGetInt64(out var r))oldPrice=r;
        var first=images.FirstOrDefault();
        return new(id,name,Text(p,"garmentName")??Text(p,"category")??"Ocharo piece",Text(p,"fabric"),oldPrice,first?.Url,first?.Url,
            Text(p,"slug")??id,Text(p,"description"),Text(p,"category"),Text(p,"availability"),Strings(p,"sizes"),Strings(p,"collectionIds"),price,currency,first?.Alt,first?.Kind,first?.Credit,first?.Rights,images);
    }
    public async Task<byte[]?> DownloadAsync(string url,CancellationToken ct)
    {
        var origin=(options.Value.PlatformBaseUrl??options.Value.CatalogueBaseUrl)?.TrimEnd('/');
        if(origin is null||Url(origin,url)is null)return null;
        try{return await http.GetByteArrayAsync(url,ct);}catch(Exception e)when(e is HttpRequestException or TaskCanceledException){return null;}
    }
}
