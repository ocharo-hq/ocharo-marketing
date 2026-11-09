using AwesomeAssertions;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Infrastructure.Cards.Brokers;
using OcharoMarketing.Infrastructure.Cards.Services;
using OcharoMarketing.Infrastructure.Cards.Settings;
using OcharoMarketing.Tests.Support;
using SkiaSharp;
using Xunit;

namespace OcharoMarketing.Tests.Integration.Cards;

/// <summary>Verifies real browser artwork exports: exact card dimensions, the atlas PDF, and clipped copy refused
/// rather than shrunk. Needs Playwright's Chromium.</summary>
public sealed class CardBrowserRenderingTests
{
    [Fact]
    [Trait("Category", "Browser")]
    public async Task RenderPng_ShouldExportExactCardDimensionsAndAnAtlasPdf()
    {
        var broker = new PlaywrightDocumentRenderingBroker(new CardRendererSettings());
        var service = new HtmlCardRenderingService(broker);
        CardPreview preview = service.Compose(CardFixtures.Input("Nimcha"));
        byte[] png = await service.RenderPngAsync(preview.Html, CancellationToken.None);
        using SKBitmap rendered = SKBitmap.Decode(png);
        rendered.Width.Should().Be(1080);
        rendered.Height.Should().Be(1350);
        byte[] pdf = await service.RenderAtlasPdfAsync(
            new[] { new CardAtlasItem("Nimcha", "T02 · bone", png, "Exact selected card output") },
            CancellationToken.None);
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task RenderPng_ShouldRejectTheCardInsteadOfShrinkingText_WhenCopyWouldClip()
    {
        var broker = new PlaywrightDocumentRenderingBroker(new CardRendererSettings());
        var service = new HtmlCardRenderingService(broker);
        CardPreview preview = service.Compose(
            CardFixtures.Input(string.Join(" ", Enumerable.Repeat("Long garment", 10))));
        await FluentActions.Awaiting(() =>
            service
                .RenderPngAsync(preview.Html, CancellationToken.None))
                .Should()
                .ThrowExactlyAsync<CardRenderingException>();
    }
}
