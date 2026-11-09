using AwesomeAssertions;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Infrastructure.Cards.Services;
using OcharoMarketing.Tests.Support;
using SkiaSharp;
using Xunit;

namespace OcharoMarketing.Tests.Unit.Cards;

/// <summary>Verifies safe composition, visible garment placement, and real browser artwork exports.</summary>
public sealed class CardRenderingTests
{
    [Fact]
    public void Compose_ShouldEncodeCopyAndLoadNoExternalResources()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardPreview preview = service.Compose(CardFixtures.Input("<script>alert(1)</script>"));
        preview.Html.Should().Contain("&lt;script&gt;");
        preview.Html.Should().NotContain("<script>");
        preview.Html.Should().Contain("font-src data:");
        preview.Html.Should().NotContain("https://");
        preview.Html.Should().NotContain("Draft");
    }

    [Fact]
    public void Compose_ShouldKeepGeometryAndCopy_WhenOnlyTheSurfaceChanges()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardCompositionInput input = CardFixtures.Input("Nimcha");
        string bone = service.Compose(input).Html;
        string dark = service.Compose(input with { PresetId = "t02-charcoal" }).Html;
        (dark.Replace("#242322", "#f2eddf").Replace("#f7f1e3", "#27241f")).Should().Be(bone);
    }

    [Fact]
    public void Compose_ShouldRefuseAFabricPreset_WhenItsOwnFabricSourceIsMissing()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        ArgumentException failure = FluentActions.Invoking(() =>
            service
                .Compose(CardFixtures.Input("Nimcha") with { PresetId = "t01-fabric-right" }))
                .Should()
                .ThrowExactly<ArgumentException>().Which;
        failure.Message.Should().Contain("matching fabric");
    }

    [Fact]
    public void Compose_ShouldRefuseTheHero_WhenItIsFullyTransparent()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardCompositionInput input = CardFixtures.Input("Nimcha") with { HeroImage = CardFixtures.Image(false) };
        FluentActions.Invoking(() => service.Compose(input)).Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(double.NaN)]
    public void Compose_ShouldRefuse_WhenTheScaleIsInvalid(double scale)
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        FluentActions
            .Invoking(() => service.Compose(CardFixtures.Input("Nimcha") with { Scale = scale }))
            .Should()
            .ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Compose_ShouldRefuse_WhenTheImageFormatMismatches()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        CardImage png = CardFixtures.Image(true);
        FluentActions.Invoking(() => service.Compose(CardFixtures.Input("Nimcha") with
        { HeroImage = png with { ContentType = "image/jpeg" } })).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void Compose_ShouldRefuseBeforePlacement_WhenTheImageHeaderIsOversized()
    {
        using var bitmap = new SKBitmap(8193, 1);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        ArgumentException error = FluentActions.Invoking(() => service.Compose(CardFixtures.Input("Nimcha") with
        { HeroImage = new CardImage(data.ToArray(), "image/png") })).Should().ThrowExactly<ArgumentException>().Which;
        error.Message.Should().Contain("supported dimensions");
    }

    [Fact]
    public async Task RenderAtlasPdf_ShouldRefuse_WhenTheSelectionIsEmpty()
    {
        var service = new HtmlCardRenderingService(new UnusedRenderingBroker());
        await FluentActions.Awaiting(async () =>
            await service
                .RenderAtlasPdfAsync(Array.Empty<CardAtlasItem>(), CancellationToken.None))
                .Should()
                .ThrowExactlyAsync<ArgumentException>();
    }
}
