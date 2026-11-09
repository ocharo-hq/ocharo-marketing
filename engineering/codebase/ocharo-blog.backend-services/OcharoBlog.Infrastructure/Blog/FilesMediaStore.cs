using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Media.Images;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>The names Editorial's files carry in WoW2 Files, the same the migration manifest gives the files it moves.</summary>
public static class BlogFiles
{
    /// <summary>The app every Editorial file belongs to: Editorial's Identity client id, so Files applies one policy.</summary>
    public const string AppId = "editorial";

    /// <summary>The record type of the reference that keeps a stored file alive; its record id is the SHA-256.</summary>
    public const string MediaReference = "editorial.media";
}

/// <summary>Stores originals and edited derivatives in WoW2 Files once per SHA-256, and links to them for
/// <see cref="MediaContent.LinkLifetime"/>; Files makes the 1400 px WebP preview as a variant, so none is written here. A
/// file stored before Files reads from the local blob folder through <see cref="MediaStore"/> until the migration fills its
/// id. Server-side reads (editing, static exports) take the original from Files and render a preview from it. A file
/// another Ocharo app keeps in Files (Brand's logo) is referenced, never copied.</summary>
/// <remarks>A person's request calls Files with their exchanged token, any other call (the seeder, an anonymous public
/// render) with Editorial's own; the Files client picks by whether the request has a signed-in session.</remarks>
public sealed class FilesMediaStore(
    IBlogRepository repository,
    IFileUploadsBroker uploads,
    IFileReferencesBroker references,
    IFileUrlsBroker urls,
    IFileStoreBroker store,
    MediaStore local,
    IOptions<BlogOptions> options,
    TimeProvider time,
    IImageService images) : IMediaStore
{
    public Task<MediaBlobRecord> StoreAsync(byte[] bytes, string fileName, CancellationToken ct) => StoreCoreAsync(bytes, fileName, null, ct);

    public Task<MediaBlobRecord> StoreEditedAsync(byte[] bytes, string fileName, MediaEditRecord edit, CancellationToken ct) =>
        StoreCoreAsync(bytes, fileName, edit, ct);

    private async Task<MediaBlobRecord> StoreCoreAsync(byte[] bytes, string fileName, MediaEditRecord? edit, CancellationToken ct)
    {
        var (info, sha) = MediaInput.Check(bytes, edit, options.Value.MaximumImageBytes);
        var existing = await repository.FindMediaAsync(sha, ct);
        if (existing is { FileId: not null })
            return await WithReceiptAsync(existing, edit, ct);

        FileModel file;
        await using (var content = new MemoryStream(bytes, false))
        {
            var spec = new UploadSpec
            {
                Name = $"{sha[..16]}.{MediaInput.Extension(info.ContentType)}",
                ContentType = info.ContentType,
                AppId = BlogFiles.AppId,
                IdempotencyKey = $"{BlogFiles.MediaReference}:{sha}",
            };
            file = Unwrap(await uploads.UploadAsync(content, spec, ct));
        }
        Unwrap(await references.AttachAsync(file.Id, new FileReference(BlogFiles.MediaReference, sha), ct));

        if (existing is not null)
        {
            await repository.SetMediaFileAsync(sha, file.Id.ToString(), ct);
            existing.FileId = file.Id.ToString();
            return await WithReceiptAsync(existing, edit, ct);
        }

        var record = new MediaBlobRecord
        {
            Sha256 = sha,
            FileName = MediaInput.SafeName(fileName),
            ContentType = info.ContentType,
            ByteLength = bytes.LongLength,
            FileId = file.Id.ToString(),
            Width = info.Width,
            Height = info.Height,
            StoredAt = time.GetUtcNow(),
        };
        if (edit is null) await repository.SaveMediaAsync(record, ct);
        else await repository.SaveMediaWithEditAsync(record, edit, ct);
        return record;
    }

    public async Task<MediaContent?> OpenAsync(string sha256, bool preview, CancellationToken ct)
    {
        if (!MediaInput.IsSha256(sha256)) return null;
        var record = await repository.FindMediaAsync(sha256, ct);
        if (record is null) return null;
        if (record.FileId is null || !FileId.TryParse(record.FileId, out var id))
            return await local.OpenLocalAsync(record, preview, ct);

        var spec = new UrlSpec { Lifetime = MediaContent.LinkLifetime, Width = preview ? MediaInput.PreviewSize(options.Value) : null };
        var link = Unwrap(await urls.IssueAsync(id, spec, ct));
        return new MediaContent(record, preview ? "image/webp" : record.ContentType, link.Url, null);
    }

    public async Task<(MediaBlobRecord Record, Stream Content)?> ReadAsync(string sha256, bool preview, CancellationToken ct)
    {
        if (!MediaInput.IsSha256(sha256)) return null;
        var record = await repository.FindMediaAsync(sha256, ct);
        if (record is null) return null;
        if (record.FileId is null || !FileId.TryParse(record.FileId, out var id))
            return await local.ReadLocalAsync(record, preview, ct);

        var opened = await store.OpenReadAsync(id, null, ct);
        if (opened is Result<FileContent>.Failure { Error.Type: AppErrorType.NotFound }) return null;
        var content = Unwrap(opened).Content;
        if (!preview) return (record, content);

        // Files keeps the preview as a link-only variant; a server-side read renders the same WebP from the original.
        byte[] original;
        await using (content)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            original = buffer.ToArray();
        }
        return (record, new MemoryStream(await MediaInput.PreviewAsync(images, original, MediaInput.PreviewSize(options.Value), ct), false));
    }

    public async Task<MediaBlobRecord?> ReferenceAsync(string sha256, string fileId, string fileName, CancellationToken ct)
    {
        if (!MediaInput.IsSha256(sha256) || !FileId.TryParse(fileId, out var id)) return null;
        var existing = await repository.FindMediaAsync(sha256, ct);
        if (existing is { FileId: not null }) return existing;

        // Files checks references and links per tenant only, so Editorial may hold one on another Ocharo app's file.
        var found = await store.GetAsync(id, ct);
        if (found is Result<FileModel>.Failure { Error.Type: AppErrorType.NotFound }) return null;
        var file = Unwrap(found);
        if (file.Sha256 != sha256 || file.State != FileState.Ready || file.DeletedAtUtc is not null) return null;
        Unwrap(await references.AttachAsync(file.Id, new FileReference(BlogFiles.MediaReference, sha256), ct));

        if (existing is not null)
        {
            await repository.SetMediaFileAsync(sha256, file.Id.ToString(), ct);
            existing.FileId = file.Id.ToString();
            return existing;
        }

        var record = new MediaBlobRecord
        {
            Sha256 = sha256,
            FileName = MediaInput.SafeName(fileName),
            ContentType = file.ContentType,
            ByteLength = file.Size,
            FileId = file.Id.ToString(),
            StoredAt = time.GetUtcNow(),
        };
        await repository.SaveMediaAsync(record, ct);
        return record;
    }

    /// <summary>Keeps an edited derivative's receipt beside the file it describes.</summary>
    private async Task<MediaBlobRecord> WithReceiptAsync(MediaBlobRecord record, MediaEditRecord? edit, CancellationToken ct)
    {
        if (edit is not null) await repository.SaveMediaWithEditAsync(record, edit, ct);
        return record;
    }

    /// <summary>Returns a Files call's value, or refuses the Editorial request with the status its failure maps to.</summary>
    internal static T Unwrap<T>(Result<T> result) where T : notnull => result.Match(value => value, error => throw Refusal(error));

    private static void Unwrap(Result result) => result.Match(() => true, error => throw Refusal(error));

    private static BlogException Refusal(AppError error) => error.Type switch
    {
        AppErrorType.Validation => new BlogException(error.Message, 400),
        AppErrorType.NotFound => new BlogException("The file is gone from WoW2 Files.", 404),
        AppErrorType.ExternalUnavailable => new BlogException("WoW2 Files is unavailable; try again shortly.", 503),
        _ => new BlogException($"WoW2 Files refused the call: {error.Message}", 502),
    };
}

/// <summary>Fills the file id of each file stored before Files from the files the migration moved: every Editorial file in
/// Files, matched by SHA-256 (Files t9 #5 step 6). Runs at boot as Editorial itself; a file the migration has not moved yet
/// keeps reading from the local blob folder.</summary>
public sealed class FilesMediaBackfill(IBlogRepository repository, IFileStoreBroker files, ILogger<FilesMediaBackfill> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var waiting = (await repository.GetMediaWithoutFileAsync(ct)).Select(record => record.Sha256).ToHashSet(StringComparer.Ordinal);
        if (waiting.Count == 0) return;

        var filled = 0;
        string? cursor = null;
        do
        {
            var page = await files.ListAsync(new FileQuery { AppId = BlogFiles.AppId, Cursor = cursor, Limit = FilesConstants.MaxPageSize }, ct);
            if (page is Result<FilePage>.Failure failure)
            {
                logger.LogWarning("Editorial's file ids stay unfilled: WoW2 Files answered {Type}: {Message}", failure.Error.Type, failure.Error.Message);
                return;
            }
            var (items, next) = page is Result<FilePage>.Success success ? (success.Value.Items, success.Value.NextCursor) : ([], null);
            foreach (var file in items.Where(file => waiting.Remove(file.Sha256)))
            {
                await repository.SetMediaFileAsync(file.Sha256, file.Id.ToString(), ct);
                filled++;
            }
            cursor = next;
        }
        while (cursor is not null && waiting.Count > 0);

        if (filled > 0 || waiting.Count > 0)
            logger.LogInformation("Editorial filled {Filled} file ids from WoW2 Files; {Waiting} still read from the local blob folder.", filled, waiting.Count);
    }
}
