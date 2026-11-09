using OcharoMarketing.Application.Cards.Models;
using SkiaSharp;

namespace OcharoMarketing.Tests.Support;

/// <summary>Builds card compositions over a small generated garment image.</summary>
internal static class CardFixtures
{
    /// <summary>A composition of the bone preset over a visible garment.</summary>
    /// <param name="title">The card's title.</param>
    public static CardCompositionInput Input(string title)
    {
        return new CardCompositionInput("fixture", "t02-bone", title, "", "uz", Image(true));
    }

    /// <summary>A small PNG with a garment drawn, or a transparent one.</summary>
    /// <param name="visible">Whether to draw the garment.</param>
    public static CardImage Image(bool visible)
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
