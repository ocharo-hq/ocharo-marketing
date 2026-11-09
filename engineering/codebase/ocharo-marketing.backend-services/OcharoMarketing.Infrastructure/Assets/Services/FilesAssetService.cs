using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Core.Errors;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Domain.Assets.Entities;
using OcharoMarketing.Infrastructure.Core;
using WoW.Two.Sdk.Beta.Be.Files;
using WoW.Two.Sdk.Beta.Be.Files.References;
using WoW.Two.Sdk.Beta.Be.Files.Store;
using WoW.Two.Sdk.Beta.Be.Files.Uploads;
using WoW.Two.Sdk.Beta.Be.Files.Urls;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace OcharoMarketing.Infrastructure.Assets.Services;

/// <summary>The names Marketing's files carry in WoW2 Files, the same the migration manifest gives the files it
/// moves.</summary>
public static class MarketingFiles
{
    /// <summary>The app every Marketing file belongs to: Marketing's Identity client id, so Files applies one
    /// policy.</summary>
    public const string AppId = "marketing";

    /// <summary>The record type of the reference that keeps an asset's bytes alive; its record id is their SHA-256, the
    /// one
    /// key an asset's local blob (<c>originals/{id}/{sha}</c>) and its Files file share.</summary>
    public const string AssetReference = "marketing.asset";

    /// <summary>The record type of the reference that keeps a draft export ZIP alive; its record id is the ZIP's
    /// SHA-256.</summary>
    public const string ExportReference = "marketing.export";
}

/// <summary>Stores assets in WoW2 Files once per SHA-256, links the asset route to them for
/// <see cref="AssetContent.LinkLifetime"/>, reads their bytes back for server-side work, and keeps each approved
/// revision's
/// export ZIP there as a private file. An asset or export made before Files reads from the local blob folder through
/// <see cref="LocalAssetService"/> until the migration fills its id.</summary>
/// <remarks>A person's request calls Files with their exchanged token, any other call (the generation worker, the seed
/// import, the boot backfill) with Marketing's own; the Files client picks by whether the call has a signed-in
/// session.</remarks>
public sealed class FilesAssetService(
    IMarketingRepository repository,
    IFileUploadsBroker uploads,
    IFileReferencesBroker references,
    IFileUrlsBroker urls,
    IFileStoreBroker store,
    LocalAssetService local,
    IOptions<MarketingOptions> options) : IAssetService
{
    public async Task<PrivateAssetContent> ReadAsync(string id, CancellationToken ct)
    {
        var record = await AssetRules.FindAsync(repository, id, ct);
        if (FileOf(record.FileId) is not { } file) return await local.ReadLocalAsync(record, ct);
        var asset = MarketingJson.Read<AssetModel>(record.Json);
        return new(
            asset,
            await ReadFileAsync(
                file,
                asset.Sha256,
                "Asset integrity failed. Restore the original before exporting.",
                ct));
    }

    public async Task<AssetContent> OpenAsync(string id, CancellationToken ct)
    {
        var record = await AssetRules.FindAsync(repository, id, ct);
        if (FileOf(record.FileId) is not { } file)
        {
            var content = await local.ReadLocalAsync(record, ct);
            return new(content.Asset, null, content.Bytes);
        }

        var link = Unwrap(
            await urls.IssueAsync(
                file,
                new UrlSpec { Lifetime = AssetContent.LinkLifetime, Disposition = FileDisposition.Inline },
                ct));
        return new(MarketingJson.Read<AssetModel>(record.Json), link.Url, null);
    }

    public async Task<AssetModel> SaveAsync(
        byte[] bytes,
        string fileName,
        string provenance,
        string role,
        CancellationToken ct,
        string? id = null)
    {
        var asset = AssetRules.Describe(bytes, fileName, provenance, role, id, options.Value.MaximumAssetBytes);
        var existing = await repository.FindAssetAsync(asset.Id, ct);
        if (existing is not null)
        {
            var stored = AssetRules.Same(existing, asset.Sha256);
            if (existing.FileId is null)
                await repository.SetAssetFileAsync(
                    existing.Id,
                    (
                        await UploadAsync(
                            bytes,
                            ImageName(asset),
                            asset.ContentType,
                            MarketingFiles.AssetReference,
                            asset.Sha256,
                            ct)).ToString(),
                    ct);
            return stored;
        }

        var file = await UploadAsync(
            bytes,
            ImageName(asset),
            asset.ContentType,
            MarketingFiles.AssetReference,
            asset.Sha256,
            ct);
        await repository.SaveAssetAsync(asset, string.Empty, ct, file.ToString());
        return asset;
    }

    public async Task<byte[]> ExportDraftAsync(string id, CancellationToken ct)
    {
        var draft = await DraftExports.ApprovedAsync(repository, id, ct);
        if (await repository.FindExportFileAsync(draft.Id, draft.Revision, ct) is { } recorded)
            return await ReadExportAsync(recorded, ct);
        if (await local.ReadExportAsync(draft, ct) is { } before) return before;

        var bytes = await DraftExports.BuildAsync(draft, assetId => ReadAsync(assetId, ct));
        var sha = AssetRules.Hash(bytes);
        var file = await UploadAsync(
            bytes,
            $"ocharo-draft-{draft.Id}-{draft.Revision}.zip",
            "application/zip",
            MarketingFiles.ExportReference,
            sha,
            ct);
        var first = await repository.SaveExportFileAsync(
            new ExportFileRecord
            {
                DraftId = draft.Id,
                Revision = draft.Revision,
                Sha256 = sha,
                FileId = file.ToString()
            },
            ct);
        return first.Sha256 == sha ? bytes : await ReadExportAsync(first, ct);
    }

    /// <summary>Returns a Files call's value, or refuses the Marketing request with the status its failure maps
    /// to.</summary>
    internal static T Unwrap<T>(Result<T> result) where T : notnull =>
        result.Match(value => value, error => throw Refusal(error));

    private static void Unwrap(Result result) => result.Match(() => true, error => throw Refusal(error));

    private static AppException Refusal(AppError error) => error.Type switch
    {
        AppErrorType.Validation => new AppException(MarketingErrors.Invalid(error.Message)),
        AppErrorType.NotFound => new AppException(MarketingErrors.NotFound("The file is gone from WoW2 Files.")),
        AppErrorType.ExternalUnavailable =>
            new AppException(MarketingErrors.Unavailable("WoW2 Files is unavailable; try again shortly.")),
        _ => new AppException(MarketingErrors.Refused($"WoW2 Files refused the call: {error.Message}")),
    };

    /// <summary>Parses a recorded file id; none for a record made before Files.</summary>
    private static FileId? FileOf(string? value) => value is not null && FileId.TryParse(value, out var id) ? id : null;

    /// <summary>The name an asset's file carries in Files: its hash and the extension Files checks against its
    /// bytes.</summary>
    private static string ImageName(AssetModel asset) =>
        $"{asset.Sha256[..16]}.{AssetRules.Extension(asset.ContentType)}";

    /// <summary>Uploads bytes once per SHA-256 as app <c>marketing</c> and attaches the reference that keeps them
    /// alive.</summary>
    private async Task<FileId> UploadAsync(
        byte[] bytes,
        string name,
        string contentType,
        string reference,
        string sha,
        CancellationToken ct)
    {
        FileModel file;
        await using (var content = new MemoryStream(bytes, false))
        {
            var spec = new UploadSpec
            {
                Name = name,
                ContentType = contentType,
                AppId = MarketingFiles.AppId,
                IdempotencyKey = $"{reference}:{sha}",
            };
            file = Unwrap(await uploads.UploadAsync(content, spec, ct));
        }
        Unwrap(await references.AttachAsync(file.Id, new FileReference(reference, sha), ct));
        return file.Id;
    }

    private async Task<byte[]> ReadExportAsync(ExportFileRecord recorded, CancellationToken ct) =>
        FileOf(recorded.FileId) is { } file
            ? await ReadFileAsync(
                file,
                recorded.Sha256,
                "The stored export failed its integrity check. Approve the draft again to export it anew.",
                ct)
            : throw new AppException(MarketingErrors.Conflict("The stored export is unavailable."));

    /// <summary>Reads a file's bytes from Files, checked against the SHA-256 Marketing recorded.</summary>
    private async Task<byte[]> ReadFileAsync(FileId file, string sha256, string integrityMessage, CancellationToken ct)
    {
        var content = Unwrap(await store.OpenReadAsync(file, null, ct));
        await using var stream = content.Content;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (AssetRules.Hash(bytes) != sha256) throw new AppException(MarketingErrors.Conflict(integrityMessage));
        return bytes;
    }
}

/// <summary>Fills the file id of each asset and export made before Files from the files the migration moved: every
/// Marketing
/// file in Files, matched by SHA-256 (Files t9 #5 step 6). Runs at boot as Marketing itself; a file the migration has
/// not
/// moved yet keeps reading from the local blob folder.</summary>
public sealed class FilesAssetBackfill(
    IMarketingRepository repository,
    IBlobRepository blobs,
    IFileStoreBroker files,
    ILogger<FilesAssetBackfill> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var assets = (await repository.GetAssetsWithoutFileAsync(ct))
            .GroupBy(record => MarketingJson.Read<AssetModel>(record.Json).Sha256, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(record => record.Id).ToArray(),
                StringComparer.Ordinal);
        var exports = await LocalExportsWithoutFileAsync(ct);
        if (assets.Count == 0 && exports.Count == 0) return;

        var filled = 0;
        string? cursor = null;
        do
        {
            var page = await files.ListAsync(
                new FileQuery { AppId = MarketingFiles.AppId, Cursor = cursor, Limit = FilesConstants.MaxPageSize },
                ct);
            if (page is Result<FilePage>.Failure failure)
            {
                logger.LogWarning(
                    "Marketing's file ids stay unfilled: WoW2 Files answered {Type}: {Message}",
                    failure.Error.Type,
                    failure.Error.Message);
                return;
            }
            var (items, next) = page is Result<FilePage>.Success success
                ? (success.Value.Items, success.Value.NextCursor)
                : ([], null);
            foreach (var file in items)
            {
                if (assets.Remove(file.Sha256, out var ids))
                    foreach (var id in ids)
                    {
                        await repository.SetAssetFileAsync(id, file.Id.ToString(), ct);
                        filled++;
                    }
                if (exports.Remove(file.Sha256, out var revisions))
                    foreach (var (draftId, revision) in revisions)
                    {
                        await repository.SaveExportFileAsync(
                            new ExportFileRecord
                            {
                                DraftId = draftId,
                                Revision = revision,
                                Sha256 = file.Sha256,
                                FileId = file.Id.ToString()
                            },
                            ct);
                        filled++;
                    }
            }
            cursor = next;
        }
        while (cursor is not null && (assets.Count > 0 || exports.Count > 0));

        if (filled > 0 || assets.Count > 0 || exports.Count > 0)
            logger.LogInformation(
                "Marketing filled {Filled} file ids from WoW2 Files; {Assets} asset hashes and {Exports} "
                    + "export hashes still read from the local blob folder.",
                filled, assets.Count, exports.Count);
    }

    /// <summary>Hashes the local export ZIPs no export file records yet, by SHA-256.</summary>
    private async Task<Dictionary<string, List<(string DraftId, int Revision)>>> LocalExportsWithoutFileAsync(
        CancellationToken ct)
    {
        var waiting = new Dictionary<string, List<(string DraftId, int Revision)>>(StringComparer.Ordinal);
        await foreach (var blob in blobs.ListAsync(DraftExports.LocalPrefix, ct))
        {
            if (blob is null || !DraftExports.TryParseLocalPath(blob.Path, out var draftId, out var revision)
                || await repository.FindExportFileAsync(draftId, revision, ct) is not null)
                continue;
            await using var stream = await blobs.OpenReadAsync(blob.Path, ct);
            if (stream is null) continue;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            var sha = AssetRules.Hash(buffer.ToArray());
            if (!waiting.TryGetValue(sha, out var revisions)) waiting[sha] = revisions = [];
            revisions.Add((draftId, revision));
        }
        return waiting;
    }
}
