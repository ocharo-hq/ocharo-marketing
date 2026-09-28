using System.Net;
using System.Reflection;
using System.Text;
using OcharoMarketing.Application.Cards.Brokers;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Cards.Services;
using SkiaSharp;

namespace OcharoMarketing.Infrastructure.Cards.Services;

/// <summary>Builds the same bounded HTML composition for preview, cards, and atlas exports.</summary>
public sealed class HtmlCardRenderingService(IDocumentRenderingBroker renderer) : ICardRenderingService
{
    private static readonly Lazy<string> FontCss = new(LoadFonts);
    private static readonly IReadOnlyList<CardPreset> Presets = Array.AsReadOnly(new[]
    {
        new CardPreset("t02-bone", "Editorial · bone", "T02", "bone", "Front silhouette on warm ivory.", false, 1080, 1350),
        new CardPreset("t02-charcoal", "Editorial · charcoal", "T02", "charcoal", "The same composition on charcoal.", false, 1080, 1350),
        new CardPreset("t01-fabric-right", "Half fabric · right", "T01", "bone", "Matching fabric occupies the right half.", true, 1080, 1350),
    });

    public IReadOnlyList<CardPreset> GetPresets()
    {
        return Presets;
    }

    public CardPreview Compose(CardCompositionInput input)
    {
        CardPreset preset = Presets.SingleOrDefault(item => item.Id == input.PresetId)
            ?? throw new ArgumentException("Choose an available card preset.");
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160)
            throw new ArgumentException("A card title between 1 and 160 characters is required.");
        if (input.Subtitle.Length > 200 || input.BrandName.Length > 60)
            throw new ArgumentException("Card copy exceeds the supported length.");
        if (!new[] { "uz", "ru", "en" }.Contains(input.Locale))
            throw new ArgumentException("Choose Uzbek, Russian, or English.");
        if (!double.IsFinite(input.Scale) || input.Scale < 0.75 || input.Scale > 1.05
            || !double.IsFinite(input.OffsetX) || Math.Abs(input.OffsetX) > 0.12
            || !double.IsFinite(input.OffsetY) || Math.Abs(input.OffsetY) > 0.06)
            throw new ArgumentException("Framing is outside the permitted range.");
        if (preset.RequiresFabric && input.FabricImage is null)
            throw new ArgumentException("This preset needs a matching fabric image. Upload one or select Editorial.");
        string hero = ImageData(input.HeroImage);
        string placement = PlaceHero(input.HeroImage, input.Scale, input.OffsetX, input.OffsetY);
        string fabric = input.FabricImage is null ? "" : ImageData(input.FabricImage);
        bool dark = preset.Surface == "charcoal";
        string background = dark ? "#242322" : "#f2eddf";
        string ink = dark ? "#f7f1e3" : "#27241f";
        string texture = preset.RequiresFabric
            ? $"<div class='fabric'><img src='{fabric}' alt=''></div><div class='veil'></div>"
            : "";
        string html =
            $$"""
            <!doctype html><html lang="{{Encode(input.Locale)}}"><head><meta charset="utf-8">
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; font-src data:; style-src 'unsafe-inline'">
            <style>{{FontCss.Value}}
            *{box-sizing:border-box}html,body{margin:0;width:1080px;height:1350px;overflow:hidden}
            #card{position:relative;width:1080px;height:1350px;overflow:hidden;background:{{background}};color:{{ink}}}
            .fabric{position:absolute;inset:0 0 0 50%;overflow:hidden}.fabric img{height:100%;width:100%;object-fit:cover}
            .veil{position:absolute;inset:0;background:linear-gradient(90deg,#f2eddf 39%,#f2eddfcc 53%,#f2eddf10 90%)}
            .brand{position:absolute;top:54px;left:66px;font:600 40px CardSans;letter-spacing:9px}
            .rule{position:absolute;top:126px;left:66px;right:66px;border-top:1px solid currentColor;opacity:.22}
            .title{position:absolute;top:162px;left:64px;width:952px;height:166px;font:600 82px/.96 CardSerif;overflow:hidden;overflow-wrap:break-word}
            .subtitle{position:absolute;top:338px;left:68px;width:940px;height:106px;font:400 42px/1.2 CardSans;overflow:hidden;overflow-wrap:break-word;opacity:.78}
            .hero{position:absolute;{{placement}}}.hero img{width:100%;height:100%;display:block}
            .shadow{position:absolute;left:300px;top:1235px;width:480px;height:36px;background:#0003;border-radius:50%;filter:blur(22px)}
            .foot{position:absolute;bottom:44px;left:68px;width:944px;height:32px;border-top:1px solid currentColor;opacity:.18}
            </style></head><body><main id="card">{{texture}}<div class="brand">{{Encode(input.BrandName)}}</div>
            <div class="rule"></div><div class="title" data-fit>{{Encode(input.Title)}}</div>
            <div class="subtitle" data-fit>{{Encode(input.Subtitle)}}</div><div class="shadow"></div>
            <div class="hero"><img src="{{hero}}" alt="{{Encode(input.Title)}}"></div><div class="foot"></div>
            </main></body></html>
            """;
        var issues = new List<string>();
        if (input.Title.Length > 45) issues.Add("Long title: inspect the phone-size proof; overflowing copy blocks export.");
        if (input.Subtitle.Length > 70) issues.Add("Long supporting copy: move extra information into the caption.");
        return new CardPreview(html, issues);
    }

    public Task<byte[]> RenderPngAsync(string html, CancellationToken cancellationToken)
    {
        return renderer.RenderPngAsync(html, cancellationToken);
    }

    public Task<byte[]> RenderAtlasPdfAsync(IReadOnlyList<CardAtlasItem> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0 || items.Count > 24)
            throw new ArgumentException("Select between one and twenty-four card outputs for the atlas.");
        var spreads = new StringBuilder();
        for (int start = 0; start < items.Count; start += 2)
        {
            spreads.Append("<section class='spread'><header><b>OCHARO</b><span>Card studies · ")
                .Append(start / 2 + 1).Append("</span></header><div class='pair'>");
            foreach (CardAtlasItem item in items.Skip(start).Take(2))
            {
                string image = ImageData(new CardImage(item.PngBytes, "image/png"));
                spreads.Append("<figure><img src='").Append(image).Append("'><figcaption><b>")
                    .Append(Encode(item.Title)).Append("</b><span>").Append(Encode(item.PresetName))
                    .Append("</span><p>").Append(Encode(item.Notes)).Append("</p></figcaption></figure>");
            }
            spreads.Append("</div><footer><div><b>COMPOSITION</b><span>Garment scale and balance</span></div><div><b>LEGIBILITY</b><span>Read at phone size</span></div><div><b>SELECTION</b><span>Keep · refine · compare again</span></div></footer></section>");
        }
        string html =
            $$"""
            <!doctype html><html><head><meta charset="utf-8"><style>{{FontCss.Value}}
            @page{size:A4 landscape;margin:12mm}*{box-sizing:border-box}body{margin:0;color:#29251f;background:white;font:16px CardSans}
            .spread{break-after:page;height:685px}.spread:last-child{break-after:auto}header{display:flex;justify-content:space-between;align-items:baseline;border-bottom:1px solid #ccc;padding-bottom:12px;margin-bottom:20px}
            header b{font:600 36px CardSerif;letter-spacing:5px}header span{color:#736759}.pair{display:flex;gap:32px;justify-content:center}
            figure{margin:0;width:488px;display:grid;grid-template-columns:340px 1fr;gap:16px;align-items:start}figure img{width:340px;height:425px;object-fit:contain}
            figcaption{padding-top:8px;overflow-wrap:anywhere}figcaption b{display:block;font:600 25px/1.1 CardSerif}figcaption span{display:block;margin-top:14px;font-size:13px;color:#655b51}figcaption p{font-size:13px;line-height:1.5}footer{border-top:1px solid #ddd;margin-top:32px;padding-top:18px;color:#776d61;font-size:12px;display:grid;grid-template-columns:repeat(3,1fr);gap:24px}footer b{display:block;font-size:11px;letter-spacing:1px;color:#423b33;margin-bottom:8px}footer span{display:block;border-bottom:1px solid #ddd;padding-bottom:28px}
            </style></head><body>{{spreads}}</body></html>
            """;
        return renderer.RenderPdfAsync(html, cancellationToken);
    }

    private static string PlaceHero(CardImage image, double scale, double offsetX, double offsetY)
    {
        using SKBitmap bitmap = SKBitmap.Decode(image.Bytes)
            ?? throw new ArgumentException("The selected hero is not a decodable image.");
        if (bitmap.Width > 8192 || bitmap.Height > 8192 || (long)bitmap.Width * bitmap.Height > 20000000)
            throw new ArgumentException("The hero image exceeds the supported dimensions.");
        int left = bitmap.Width;
        int top = bitmap.Height;
        int right = -1;
        int bottom = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha <= 4) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }
        if (right < left) throw new ArgumentException("The hero image contains no visible garment.");
        double visibleWidth = right - left + 1;
        double visibleHeight = bottom - top + 1;
        double ratio = Math.Min(860 / visibleWidth, 780 / visibleHeight) * scale;
        double xPosition = 540 - (left + visibleWidth / 2) * ratio + offsetX * 1080;
        double yPosition = 845 - (top + visibleHeight / 2) * ratio + offsetY * 1350;
        if (xPosition + left * ratio < 46 || xPosition + (right + 1) * ratio > 1034
            || yPosition + top * ratio < 440 || yPosition + (bottom + 1) * ratio > 1250)
            throw new ArgumentException("Framing would crop the garment or cover the title. Reduce scale or offsets.");
        return FormattableString.Invariant($"left:{xPosition:F3}px;top:{yPosition:F3}px;width:{bitmap.Width * ratio:F3}px;height:{bitmap.Height * ratio:F3}px;");
    }

    private static string ImageData(CardImage image)
    {
        if (image.ContentType is not ("image/png" or "image/jpeg") || image.Bytes.Length == 0
            || image.Bytes.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Card images must be PNG/JPEG files up to 5 MiB.");
        // Inspect compressed image headers before allocating the full raster.
        using SKData data = SKData.CreateCopy(image.Bytes);
        using SKCodec codec = SKCodec.Create(data)
            ?? throw new ArgumentException("The selected image cannot be decoded.");
        SKImageInfo info = codec.Info;
        if (info.Width is < 1 or > 8192 || info.Height is < 1 or > 8192
            || (long)info.Width * info.Height > 20000000)
            throw new ArgumentException("The image exceeds the supported dimensions.");
        if ((image.ContentType == "image/png" && codec.EncodedFormat != SKEncodedImageFormat.Png)
            || (image.ContentType == "image/jpeg" && codec.EncodedFormat != SKEncodedImageFormat.Jpeg))
            throw new ArgumentException("The image content does not match its declared format.");
        return $"data:{image.ContentType};base64,{Convert.ToBase64String(image.Bytes)}";
    }

    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string LoadFonts()
    {
        Assembly assembly = typeof(HtmlCardRenderingService).Assembly;
        var css = new StringBuilder();
        foreach ((string file, string family) in new[] { ("CormorantGaramond.ttf", "CardSerif"), ("Manrope.ttf", "CardSans") })
        {
            string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("." + file, StringComparison.Ordinal));
            using Stream stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("The card font resource is missing.");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            css.Append("@font-face{font-family:").Append(family).Append(";font-style:normal;font-weight:200 800;src:url(data:font/ttf;base64,")
                .Append(Convert.ToBase64String(buffer.ToArray())).Append(") format('truetype');}");
        }
        return css.ToString();
    }
}
