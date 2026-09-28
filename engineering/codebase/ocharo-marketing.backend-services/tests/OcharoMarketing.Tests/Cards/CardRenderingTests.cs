using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Cards.Brokers;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Infrastructure.Cards.Brokers;
using OcharoMarketing.Infrastructure.Cards.Services;
using OcharoMarketing.Infrastructure.Cards.Settings;
using SkiaSharp;
using Xunit;

namespace OcharoMarketing.Tests.Cards;

/// <summary>Verifies safe composition, visible garment placement, and real browser artwork exports.</summary>
public sealed class CardRenderingTests
{
    [Fact]
    public void CopyIsEncodedAndCardHasNoExternalResources()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardPreview preview = service.Compose(Input("<script>alert(1)</script>"));
        Assert.Contains("&lt;script&gt;", preview.Html);
        Assert.DoesNotContain("<script>", preview.Html);
        Assert.Contains("font-src data:", preview.Html);
        Assert.DoesNotContain("https://", preview.Html);
        Assert.DoesNotContain("Draft", preview.Html);
    }

    [Fact]
    public void SurfaceComparisonKeepsGeometryAndCopyConstant()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardCompositionInput input = Input("Nimcha");
        string bone = service.Compose(input).Html;
        string dark = service.Compose(input with { PresetId = "t02-charcoal" }).Html;
        Assert.Equal(bone, dark.Replace("#242322", "#f2eddf").Replace("#f7f1e3", "#27241f"));
    }

    [Fact]
    public void FabricPresetRequiresItsOwnMatchingSource()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
            service.Compose(Input("Nimcha") with { PresetId = "t01-fabric-right" }));
        Assert.Contains("matching fabric", failure.Message);
    }

    [Fact]
    public void FullyTransparentHeroCannotBecomeProductArtwork()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardCompositionInput input = Input("Nimcha") with { HeroImage = Image(false) };
        Assert.Throws<ArgumentException>(() => service.Compose(input));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(double.NaN)]
    public void InvalidScaleIsRejected(double scale)
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        Assert.Throws<ArgumentException>(() => service.Compose(Input("Nimcha") with { Scale = scale }));
    }

    [Fact]
    public void MismatchedImageFormatIsRejected()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardImage png = Image(true);
        Assert.Throws<ArgumentException>(() => service.Compose(Input("Nimcha") with
        { HeroImage = png with { ContentType = "image/jpeg" } }));
    }

    [Fact]
    public void OversizedImageHeaderIsRejectedBeforePlacement()
    {
        using var bitmap = new SKBitmap(8193, 1);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        ArgumentException error = Assert.Throws<ArgumentException>(() => service.Compose(Input("Nimcha") with
        { HeroImage = new CardImage(data.ToArray(), "image/png") }));
        Assert.Contains("supported dimensions", error.Message);
    }

    [Fact]
    public async Task AtlasRejectsEmptySelection()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.RenderAtlasPdfAsync(Array.Empty<CardAtlasItem>(), CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task BrowserExportsExactCardDimensionsAndAtlasPdf()
    {
        var broker = new PlaywrightDocumentRenderingBroker(new CardRendererSettings());
        var service = new HtmlCardRenderingService(broker);
        CardPreview preview = service.Compose(Input("Nimcha"));
        byte[] png = await service.RenderPngAsync(preview.Html, CancellationToken.None);
        using SKBitmap rendered = SKBitmap.Decode(png);
        Assert.Equal(1080, rendered.Width);
        Assert.Equal(1350, rendered.Height);
        byte[] pdf = await service.RenderAtlasPdfAsync(
            new[] { new CardAtlasItem("Nimcha", "T02 · bone", png, "Exact selected card output") },
            CancellationToken.None);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task BrowserRejectsClippedCopyInsteadOfShrinkingText()
    {
        var broker = new PlaywrightDocumentRenderingBroker(new CardRendererSettings());
        var service = new HtmlCardRenderingService(broker);
        CardPreview preview = service.Compose(Input(string.Join(" ", Enumerable.Repeat("Long garment", 10))));
        await Assert.ThrowsAsync<CardRenderingException>(() =>
            service.RenderPngAsync(preview.Html, CancellationToken.None));
    }

    private static CardCompositionInput Input(string title)
    {
        return new CardCompositionInput("fixture", "t02-bone", title, "", "uz", Image(true));
    }

    private static CardImage Image(bool visible)
    {
        using var bitmap = new SKBitmap(120, 200);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        if (visible)
        {
            using var paint = new SKPaint { Color = new SKColor(95, 70, 73) };
            canvas.DrawRect(35, 12, 50, 180, paint);
        }
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new CardImage(data.ToArray(), "image/png");
    }
}
