using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace OcharoBlog.Application.Blog.Services;

public interface IMediaEditingService
{
    Task<ImageWorkerStatus> BackgroundStatusAsync(CancellationToken ct);
    Task<Result<EditedImage>> PreviewAsync(string sha, MediaEditRecipe recipe, CancellationToken ct);
    Task<Result<MediaBlobRecord>> SaveAsync(
        string sha,
        MediaEditRecipe recipe,
        MediaEditActor actor,
        CancellationToken ct);
}
/// <summary>Optional segmentation and the final write check of a saved edit.</summary>
public interface IEditorialImageWorker
{
    Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct);
    Task<Result<(byte[] Bytes, string Model)>> RemoveAsync(byte[] bytes, string contentType, CancellationToken ct);
    Task<Result> EnsureEditableAsync(MediaEditActor actor, CancellationToken ct);
}
