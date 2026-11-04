using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Services;

public interface IMediaEditingService
{
    Task<ImageWorkerStatus> BackgroundStatusAsync(CancellationToken ct);
    Task<EditedImage> PreviewAsync(string sha, MediaEditRecipe recipe, CancellationToken ct);
    Task<MediaBlobRecord> SaveAsync(string sha, MediaEditRecipe recipe, MediaEditActor actor, CancellationToken ct);
}
/// <summary>Optional segmentation and the final write check of a saved edit.</summary>
public interface IEditorialImageWorker
{
    Task<ImageWorkerStatus> GetStatusAsync(CancellationToken ct);
    Task<(byte[] Bytes, string Model)> RemoveAsync(byte[] bytes, string contentType, CancellationToken ct);
    Task EnsureEditableAsync(MediaEditActor actor, CancellationToken ct);
}
