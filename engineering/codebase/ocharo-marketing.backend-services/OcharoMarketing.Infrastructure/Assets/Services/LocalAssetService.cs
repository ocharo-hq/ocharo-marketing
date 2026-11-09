using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Core.Errors;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Domain.Assets.Entities;
using OcharoMarketing.Infrastructure.Core;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Storage.Core;

namespace OcharoMarketing.Infrastructure.Assets.Services;

/// <summary>Stores private immutable raster assets and exports an approved draft snapshot in the local blob folder. It
/// serves
/// a run with no WoW2 Files (<c>Files:BaseUrl</c> unset), and <see cref="FilesAssetService"/> reads through it the
/// assets and
/// exports made before Files until the migration fills their ids.</summary>
public sealed class LocalAssetService(
    IMarketingRepository repository,
    IBlobRepository blobs,
    IOptions<MarketingOptions> options) : IAssetService
{
    public async Task<PrivateAssetContent> ReadAsync(string id, CancellationToken ct) =>
        await ReadLocalAsync(await AssetRules.FindAsync(repository, id, ct), ct);

    public async Task<AssetContent> OpenAsync(string id, CancellationToken ct)
    {
        var content = await ReadAsync(id, ct);
        return new(content.Asset, null, content.Bytes);
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
        if (existing is not null) return AssetRules.Same(existing, asset.Sha256);
        var path = $"originals/{asset.Id}/{asset.Sha256}";
        using var input = new MemoryStream(bytes, false);
        await blobs.SaveAsync(path, input, asset.ContentType, ct);
        await repository.SaveAssetAsync(asset, path, ct);
        return asset;
    }

    public async Task<byte[]> ExportDraftAsync(string id, CancellationToken ct)
    {
        var draft = await DraftExports.ApprovedAsync(repository, id, ct);
        if (await ReadExportAsync(draft, ct) is { } stored) return stored;
        var result = await DraftExports.BuildAsync(draft, assetId => ReadAsync(assetId, ct));
        using var zip = new MemoryStream(result, false);
        await blobs.SaveAsync(DraftExports.LocalPath(draft.Id, draft.Revision), zip, "application/zip", ct);
        return result;
    }

    /// <summary>Reads an asset's bytes from the local blob folder, checked against its length and SHA-256.</summary>
    internal async Task<PrivateAssetContent> ReadLocalAsync(AssetRecord record, CancellationToken ct)
    {
        var asset = MarketingJson.Read<AssetModel>(record.Json);
        if (string.IsNullOrEmpty(record.BlobPath))
            throw new AppException(
                MarketingErrors.Conflict(
                    "The stored asset bytes are unavailable: they live in WoW2 Files, which this run does not use."));
        await using var stream = await blobs.OpenReadAsync(record.BlobPath, ct)
            ?? throw new AppException(MarketingErrors.Conflict("The stored asset bytes are unavailable."));
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, ct);
        var bytes = output.ToArray();
        if (bytes.LongLength != asset.ByteLength || AssetRules.Hash(bytes) != asset.Sha256)
            throw new AppException(
                MarketingErrors.Conflict("Asset integrity failed. Restore the original before exporting."));
        return new(asset, bytes);
    }

    /// <summary>Reads a revision's export from the local blob folder; none when it was never exported there.</summary>
    internal async Task<byte[]?> ReadExportAsync(DraftModel draft, CancellationToken ct)
    {
        await using var stored = await blobs.OpenReadAsync(DraftExports.LocalPath(draft.Id, draft.Revision), ct);
        if (stored is null) return null;
        using var existing = new MemoryStream();
        await stored.CopyToAsync(existing, ct);
        return existing.ToArray();
    }
}
