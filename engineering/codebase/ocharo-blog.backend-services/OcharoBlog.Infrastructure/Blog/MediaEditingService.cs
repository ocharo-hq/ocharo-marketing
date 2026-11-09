using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Media.BackgroundRemoval;
using WoW.Two.Sdk.Beta.Be.Media.Images;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Applies SDK edits to immutable source bytes; preview never persists or changes stories.</summary>
public sealed class MediaEditingService(
    IMediaStore media, IImageService images, IEditorialImageWorker worker, TimeProvider time) : IMediaEditingService
{
    private static readonly SemaphoreSlim Operations = new(2, 2);
    public const int MaximumBytes = 20 * 1024 * 1024;
    public const long MaximumPixels = 20_000_000;
    public const int MaximumDimension = 8192;

    public Task<ImageWorkerStatus> BackgroundStatusAsync(CancellationToken ct)
    {
        return worker.GetStatusAsync(ct);
    }

    public Task<Result<EditedImage>> PreviewAsync(string sha, MediaEditRecipe recipe, CancellationToken ct)
    {
        return WithCapacityAsync(() => RenderAsync(sha, recipe, ct), ct);
    }

    private async Task<Result<EditedImage>> RenderAsync(string sha, MediaEditRecipe recipe, CancellationToken ct)
    {
        if (await media.ReadAsync(sha, false, ct) is not { } opened)
            return Fail<EditedImage>(BlogErrors.NotFound("Image"));
        await using var source = opened.Content;
        if (opened.Record.ByteLength > MaximumBytes)
            return Fail<EditedImage>(BlogErrors.TooLarge(
                "Editing supports images up to 20 MiB. Your original is unchanged."));
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + count > MaximumBytes)
                return Fail<EditedImage>(BlogErrors.TooLarge("Image exceeds 20 MiB."));
            buffer.Write(chunk, 0, count);
        }
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != sha)
            return Fail<EditedImage>(BlogErrors.Conflict("Original image checksum changed. No edit was saved."));
        try
        {
            return await EditAsync(bytes, recipe, ct);
        }
        catch (BackgroundRemovalException e)
        {
            return Fail<EditedImage>(e.Code == "invalid_image"
                ? BlogErrors.Invalid(e.Message)
                : BlogErrors.Unavailable(e.Message));
        }
        catch (ImageRejectedException)
        {
            return Fail<EditedImage>(BlogErrors.Invalid("The image edit is outside supported limits."));
        }
    }

    /// <summary>Checks the recipe against the original, removes its background when asked, then applies the
    /// edit.</summary>
    private async Task<Result<EditedImage>> EditAsync(byte[] bytes, MediaEditRecipe recipe, CancellationToken ct)
    {
        var info = RasterImageGuard.Validate(bytes);
        if (Validate(recipe, info) is { } refused)
            return Fail<EditedImage>(refused);
        string? model = null;
        if (recipe.RemoveBackground)
        {
            if ((await worker.RemoveAsync(bytes, info.ContentType, ct)).IsFailure(out var error, out var removed))
                return Fail<EditedImage>(error);
            var resultInfo = RasterImageGuard.Validate(removed.Bytes);
            if (resultInfo.ContentType != "image/png"
                || info.Width != resultInfo.Width
                || info.Height != resultInfo.Height)
                return Fail<EditedImage>(BlogErrors.Unavailable("Background removal returned invalid dimensions."));
            bytes = removed.Bytes;
            model = removed.Model;
        }
        using var processingSource = new MemoryStream(bytes, false);
        var output = await images.EditAsync(processingSource, Spec(recipe), ct);
        RasterImageGuard.Validate(output.Content);
        return Result<EditedImage>.Ok(
            new(output.Content, output.ContentType, output.Extension, output.Width, output.Height, model));
    }

    public Task<Result<MediaBlobRecord>> SaveAsync(
        string sha,
        MediaEditRecipe recipe,
        MediaEditActor actor,
        CancellationToken ct)
    {
        return WithCapacityAsync(() => SaveCoreAsync(sha, recipe, actor, ct), ct);
    }

    private async Task<Result<MediaBlobRecord>> SaveCoreAsync(
        string sha,
        MediaEditRecipe recipe,
        MediaEditActor actor,
        CancellationToken ct)
    {
        if ((await RenderAsync(sha, recipe, ct)).IsFailure(out var error, out var result))
            return Fail<MediaBlobRecord>(error);
        if (await worker.EnsureEditableAsync(actor, ct) is Result.Failure refused)
            return Fail<MediaBlobRecord>(refused.Error);
        var json = JsonSerializer.Serialize(recipe, BlogJson.Options);
        var outputSha = Convert.ToHexStringLower(SHA256.HashData(result.Bytes));
        var identity = JsonSerializer.Serialize(new
        {
            source = sha,
            output = outputSha,
            recipe,
            model = result.Model,
            organization = actor.OrganizationId,
            user = actor.UserId,
        }, BlogJson.Options);
        var edit = new MediaEditRecord
        {
            Id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity))),
            SourceSha256 = sha,
            Sha256 = outputSha,
            RecipeJson = json,
            OrganizationId = actor.OrganizationId,
            Actor = actor.UserId,
            Model = result.Model,
            CreatedAt = time.GetUtcNow(),
        };
        return await media.StoreEditedAsync(result.Bytes, $"edited-{sha[..12]}.{result.Extension}", edit, ct);
    }

    private static async Task<Result<T>> WithCapacityAsync<T>(Func<Task<Result<T>>> operation, CancellationToken ct)
        where T : notnull
    {
        if (!await Operations.WaitAsync(0, ct))
            return Fail<T>(BlogErrors.Busy("Image processing is busy. Try again shortly."));
        try
        {
            return await operation();
        }
        finally
        {
            Operations.Release();
        }
    }

    private static ImageEditSpec Spec(MediaEditRecipe recipe)
    {
        return new ImageEditSpec
        {
            Crop = recipe.Crop is { } c ? new() { X = c.X, Y = c.Y, Width = c.Width, Height = c.Height } : null,
            Rotate = recipe.Rotate,
            FlipHorizontal = recipe.FlipHorizontal,
            FlipVertical = recipe.FlipVertical,
            Resize = recipe.Resize is { } r
                ? new() { Width = r.Width, Height = r.Height, Fit = ImageFit.Stretch, Upscale = true }
                : null,
            Output = new()
            {
                Format = recipe.Output.Format switch
                {
                    "png" => ImageFormat.Png,
                    "jpeg" => ImageFormat.Jpeg,
                    _ => ImageFormat.Webp,
                },
                Quality = recipe.Output.Quality,
                Background = "#FFFFFF",
            },
        };
    }

    /// <summary>Tells why a recipe cannot apply to the source image; none when it can.</summary>
    private static AppError? Validate(MediaEditRecipe r, RasterImageInfo source)
    {
        if (r is null || r.Output is null || r.Rotate is not (0 or 90 or 180 or 270)
            || r.Output.Format is not ("png" or "jpeg" or "webp") || r.Output.Quality is < 1 or > 100)
            return BlogErrors.Invalid("Choose PNG, JPEG or WebP quality 1–100 and a quarter-turn rotation.");
        if (r.Crop is { } c && (c.X < 0 || c.Y < 0 || c.Width <= 0 || c.Height <= 0
            || (long)c.X + c.Width > source.Width || (long)c.Y + c.Height > source.Height))
            return BlogErrors.Invalid("Crop must fit inside the upright original image.");
        if (r.Resize is { } size && (size.Width <= 0 || size.Height <= 0
            || size.Width > MaximumDimension || size.Height > MaximumDimension
            || (long)size.Width * size.Height > MaximumPixels))
            return BlogErrors.Invalid("Resize must remain within 8192 pixels per side and 20 megapixels.");
        return null;
    }

    private static Result<T> Fail<T>(AppError error) where T : notnull
    {
        return Result<T>.Fail(error);
    }
}
