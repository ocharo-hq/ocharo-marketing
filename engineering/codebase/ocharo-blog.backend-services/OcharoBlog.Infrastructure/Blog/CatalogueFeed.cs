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
        locale = CatalogueLanguages.Normalize(locale);
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
            var edition = canonical ? ParseEdition(feed, origin, options.Value.BrandSlug, locale) : null;
            var available = Strings(feed, "availableLocales").Where(CatalogueLanguages.Supported).Distinct(StringComparer.Ordinal).ToList();
            // Older Platform releases can prove only the edition included in this exact feed.
            if (edition is not null && !available.Contains(edition.Locale)) available.Add(edition.Locale);
            var catalogues = canonical ? ParseCatalogues(feed, origin, options.Value.BrandSlug, locale, products) : [];
            return new(products, edition, available, catalogues);
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            logger.LogWarning("Published catalogue unavailable at {Origin}: {Message}", origin, e.Message);
            return new([]);
        }
    }
    private static CatalogueEdition? ParseEdition(JsonElement feed, string origin, string brandSlug, string locale, JsonElement? selected = null)
    {
        var edition = selected ?? (feed.TryGetProperty("edition", out var value) ? value : default);
        if (edition.ValueKind != JsonValueKind.Object) return null;
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
    private static IReadOnlyList<CatalogueCollection> ParseCatalogues(JsonElement feed, string origin,
        string brandSlug, string locale, IReadOnlyList<CatalogueProduct> products)
    {
        if (!feed.TryGetProperty("catalogues", out var entries) || entries.ValueKind != JsonValueKind.Array) return [];
        var result = new List<CatalogueCollection>();
        var knownProducts = products.Select(product => product.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in entries.EnumerateArray().Take(250))
        {
            try
            {
                var id = Text(entry, "id"); var slug = Text(entry, "slug"); var title = Text(entry, "title");
                if (!Guid.TryParse(id, out _) || !SafeToken(slug) || string.IsNullOrWhiteSpace(title)
                    || Text(entry, "locale") != locale) continue;
                CatalogueWeb? web = null;
                if (entry.TryGetProperty("web", out var sourceWeb) && sourceWeb.ValueKind == JsonValueKind.Object
                    && Text(sourceWeb, "theme") is "editorial" or "grid"
                    && sourceWeb.TryGetProperty("pages", out var sourcePages) && sourcePages.ValueKind == JsonValueKind.Array)
                {
                    var pages = new List<CatalogueWebPage>();
                    foreach (var page in sourcePages.EnumerateArray().Take(250))
                    {
                        var pageId = Text(page, "id"); var layout = Text(page, "layout");
                        if (!SafeToken(pageId) || layout is not ("spread" or "grid" or "collage")) continue;
                        var productIds = Strings(page, "productIds").Where(knownProducts.Contains).Distinct(StringComparer.Ordinal).ToList();
                        if (productIds.Count == 0 || pages.Any(existing => existing.Id == pageId)) continue;
                        pages.Add(new(pageId!, Text(page, "title") ?? title, layout!, productIds));
                    }
                    if (pages.Count > 0)
                    {
                        var mediaGroup = Text(sourceWeb, "mediaGroup") ?? "all";
                        if (mediaGroup is not ("all" or "model" or "product" or "detail" or "fabric")) continue;
                        CataloguePriceDisplay? display = null;
                        if (sourceWeb.TryGetProperty("priceDisplay", out var priceDisplay) && priceDisplay.ValueKind == JsonValueKind.Object)
                            display = new(priceDisplay.GetProperty("show").GetBoolean(), priceDisplay.GetProperty("showCurrency").GetBoolean(),
                                priceDisplay.GetProperty("showQuantity").GetBoolean(), priceDisplay.GetProperty("showRetail").GetBoolean(), priceDisplay.GetProperty("showWholesale").GetBoolean());
                        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
                        if (sourceWeb.TryGetProperty("copy", out var sourceCopy) && sourceCopy.ValueKind == JsonValueKind.Object)
                            foreach (var key in new[] { "collection", "coverNote", "header", "footer", "sizes", "pieces", "priceOnRequest", "prices", "materials", "decoration" })
                                if (Text(sourceCopy, key) is { } text) copy[key] = text;
                        Dictionary<string, IReadOnlyList<string>>? selected = null;
                        if (sourceWeb.TryGetProperty("productMedia", out var selection) && selection.ValueKind == JsonValueKind.Object)
                        {
                            selected = new(StringComparer.Ordinal);
                            foreach (var product in products)
                            {
                                var approved = (product.Images ?? []).Where(image => image.Sha256 is not null).Select(image => image.Sha256!).ToHashSet(StringComparer.Ordinal);
                                selected[product.Id] = Strings(selection, product.Id).Where(approved.Contains).Distinct(StringComparer.Ordinal).ToList();
                            }
                        }
                        web = new(Text(sourceWeb, "theme")!, pages, mediaGroup, display, copy, selected);
                    }
                }
                var pdf = entry.TryGetProperty("pdf", out var sourcePdf)
                    ? ParseEdition(feed, origin, brandSlug, locale, sourcePdf) : null;
                if (web is null && pdf is null) continue;
                var locales = Strings(entry, "availableLocales").Where(CatalogueLanguages.Supported).Distinct(StringComparer.Ordinal).ToList();
                if (!locales.Contains(locale)) locales.Add(locale);
                result.Add(new(id!, slug!, title!, Text(entry, "description"), locale,
                    entry.GetProperty("updatedAt").GetDateTimeOffset(), locales, web, pdf));
            }
            catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            { /* One malformed catalogue never exposes a draft or hides unrelated valid catalogues. */ }
        }
        return result.Where(item => result.Count(other => other.Slug == item.Slug) == 1).ToList();
    }
    private static bool SafeToken(string? value) => value is { Length: > 0 and <= 100 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

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
            images.Add(new CatalogueImage(url,Text(image,"alt")??name,evidence.ValueKind==JsonValueKind.Object?Text(evidence,"kind"):null,evidence.ValueKind==JsonValueKind.Object?Text(evidence,"credit"):null,evidence.ValueKind==JsonValueKind.Object?Text(evidence,"rights"):null,Text(image,"sha256")));
        }
        long? oldPrice=null;decimal? price=null;string? currency=null;
        if(canonical&&p.TryGetProperty("price",out var modern)&&modern.ValueKind==JsonValueKind.Object&&modern.TryGetProperty("amount",out var amount)&&amount.TryGetDecimal(out var a)){price=a;currency=Text(modern,"currency");}
        if(!canonical&&p.TryGetProperty("pricing",out var pricing)&&pricing.ValueKind==JsonValueKind.Object&&pricing.TryGetProperty("retail",out var retail)&&retail.TryGetInt64(out var r))oldPrice=r;
        var first=images.FirstOrDefault();
        return new(id,name,Text(p,"garmentName")??Text(p,"category")??"Ocharo piece",Text(p,"fabric"),oldPrice,first?.Url,first?.Url,
            Text(p,"slug")??id,Text(p,"description"),Text(p,"category"),Text(p,"availability"),Strings(p,"sizes"),Strings(p,"collectionIds"),price,currency,first?.Alt,first?.Kind,first?.Credit,first?.Rights,images,canonical ? ParsePricing(p) : null);
    }
    private static CataloguePricing? ParsePricing(JsonElement product)
    {
        if (!product.TryGetProperty("pricing", out var pricing) || pricing.ValueKind != JsonValueKind.Object
            || Text(pricing, "currency") is not { Length: 3 } currency || currency.Any(character => !char.IsAsciiLetter(character))
            || !pricing.TryGetProperty("tiers", out var source) || source.ValueKind != JsonValueKind.Array) return null;
        var tiers = new List<CataloguePriceTier>();
        foreach (var tier in source.EnumerateArray().Take(100))
        {
            if (!tier.TryGetProperty("minimumQuantity", out var minimum) || !minimum.TryGetInt32(out var min) || min < 1
                || !tier.TryGetProperty("unitPrice", out var amount) || !amount.TryGetDecimal(out var price) || price < 0) return null;
            int? max = tier.TryGetProperty("maximumQuantity", out var maximum) && maximum.ValueKind == JsonValueKind.Number && maximum.TryGetInt32(out var value) ? value : null;
            if (max < min) return null;
            tiers.Add(new(min, max, price));
        }
        return tiers.Count > 0 ? new(currency, tiers) : null;
    }

    public async Task<byte[]?> DownloadAsync(string url,CancellationToken ct)
    {
        var origin=(options.Value.PlatformBaseUrl??options.Value.CatalogueBaseUrl)?.TrimEnd('/');
        if(origin is null||Url(origin,url)is null)return null;
        try{return await http.GetByteArrayAsync(url,ct);}catch(Exception e)when(e is HttpRequestException or TaskCanceledException){return null;}
    }
}
