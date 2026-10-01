using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Backend.Beta.Media.BackgroundRemoval;
using WoW.Two.Sdk.Backend.Beta.Media.Images;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Applies SDK edits to immutable source bytes; preview never persists or changes stories.</summary>
public sealed class MediaEditingService(IMediaStore media, IImageService images, IEditorialImageWorker worker, IClock clock) : IMediaEditingService
{
    private static readonly SemaphoreSlim Operations = new(2, 2);
    public const int MaximumBytes = 20 * 1024 * 1024;
    public const long MaximumPixels = 20_000_000;
    public const int MaximumDimension = 8192;
    public Task<ImageWorkerStatus> BackgroundStatusAsync(CancellationToken ct) => worker.GetStatusAsync(ct);

    public Task<EditedImage> PreviewAsync(string sha, MediaEditRecipe recipe, CancellationToken ct) =>
        WithCapacityAsync(() => RenderAsync(sha, recipe, ct), ct);

    private async Task<EditedImage> RenderAsync(string sha, MediaEditRecipe recipe, CancellationToken ct)
    {
        var opened = await media.OpenAsync(sha, false, ct) ?? throw BlogException.NotFound("Image");
        await using var source = opened.Content;
        if (opened.Record.ByteLength > MaximumBytes) throw new BlogException("Editing supports images up to 20 MiB. Your original is unchanged.", 413);
        using var buffer = new MemoryStream(); var chunk = new byte[8192]; int count;
        while ((count = await source.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + count > MaximumBytes) throw new BlogException("Image exceeds 20 MiB.", 413);
            buffer.Write(chunk, 0, count);
        }
        var bytes = buffer.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != sha) throw new BlogException("Original image checksum changed. No edit was saved.", 409);
        try
        {
            var info = RasterImageGuard.Validate(bytes);
            Validate(recipe, info);
            string? model = null;
            if (recipe.RemoveBackground)
            {
                var removed = await worker.RemoveAsync(bytes, info.ContentType, ct);
                var resultInfo = RasterImageGuard.Validate(removed.Bytes);
                if (resultInfo.ContentType != "image/png" || info.Width != resultInfo.Width || info.Height != resultInfo.Height)
                    throw new BlogException("Background removal returned invalid dimensions.", 503);
                bytes = removed.Bytes; model = removed.Model;
            }
            using var processingSource = new MemoryStream(bytes, false);
            var output = await images.EditAsync(processingSource, new ImageEditSpec {
                Crop = recipe.Crop is { } c ? new() { X=c.X,Y=c.Y,Width=c.Width,Height=c.Height } : null,
                Rotate = recipe.Rotate, FlipHorizontal = recipe.FlipHorizontal, FlipVertical = recipe.FlipVertical,
                Resize = recipe.Resize is { } r ? new() { Width=r.Width,Height=r.Height,Fit=ImageFit.Stretch,Upscale=true } : null,
                Output = new() { Format=recipe.Output.Format switch { "png"=>ImageFormat.Png,"jpeg"=>ImageFormat.Jpeg,_=>ImageFormat.Webp }, Quality=recipe.Output.Quality, Background="#FFFFFF" }
            }, ct);
            RasterImageGuard.Validate(output.Content);
            return new(output.Content, output.ContentType, output.Extension, output.Width, output.Height, model);
        }
        catch (BackgroundRemovalException e) { throw new BlogException(e.Message, e.Code == "invalid_image" ? 400 : 503); }
        catch (ImageRejectedException) { throw new BlogException("The image edit is outside supported limits."); }
    }

    public Task<MediaBlobRecord> SaveAsync(string sha, MediaEditRecipe recipe, MediaEditActor actor, CancellationToken ct) =>
        WithCapacityAsync(() => SaveCoreAsync(sha, recipe, actor, ct), ct);

    private async Task<MediaBlobRecord> SaveCoreAsync(string sha, MediaEditRecipe recipe, MediaEditActor actor, CancellationToken ct)
    {
        var result = await RenderAsync(sha, recipe, ct);
        await worker.EnsureEditableAsync(actor, ct);
        var json = JsonSerializer.Serialize(recipe, BlogJson.Options);
        var outputSha = Convert.ToHexStringLower(SHA256.HashData(result.Bytes));
        var identity = JsonSerializer.Serialize(new { source=sha,output=outputSha,recipe,model=result.Model,organization=actor.OrganizationId,user=actor.UserId }, BlogJson.Options);
        var edit = new MediaEditRecord { Id=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity))),
            SourceSha256=sha,Sha256=outputSha,RecipeJson=json,OrganizationId=actor.OrganizationId,Actor=actor.UserId,Model=result.Model,CreatedAt=clock.UtcNow };
        return await media.StoreEditedAsync(result.Bytes, $"edited-{sha[..12]}.{result.Extension}", edit, ct);
    }

    private static async Task<T> WithCapacityAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        if (!await Operations.WaitAsync(0, ct)) throw new BlogException("Image processing is busy. Try again shortly.", 429);
        try { return await operation(); }
        finally { Operations.Release(); }
    }

    private static void Validate(MediaEditRecipe r, RasterImageInfo source)
    {
        if (r is null || r.Output is null || r.Rotate is not (0 or 90 or 180 or 270)
            || r.Output.Format is not ("png" or "jpeg" or "webp") || r.Output.Quality is < 1 or > 100)
            throw new BlogException("Choose PNG, JPEG or WebP quality 1–100 and a quarter-turn rotation.");
        if (r.Crop is {} c && (c.X<0 || c.Y<0 || c.Width<=0 || c.Height<=0 || (long)c.X+c.Width>source.Width || (long)c.Y+c.Height>source.Height))
            throw new BlogException("Crop must fit inside the upright original image.");
        if (r.Resize is {} size && (size.Width<=0 || size.Height<=0 || size.Width>MaximumDimension || size.Height>MaximumDimension || (long)size.Width*size.Height>MaximumPixels))
            throw new BlogException("Resize must remain within 8192 pixels per side and 20 megapixels.");
    }
}
