using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using WoW.Two.Sdk.Backend.Beta.Storage.Core;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>Stores private immutable raster assets and exports an approved draft snapshot.</summary>
public sealed class LocalAssetService(IMarketingRepository repository, IBlobRepository blobs, IOptions<MarketingOptions> options) : IAssetService
{
    public async Task<PrivateAssetContent> ReadAsync(string id, CancellationToken ct)
    {
        var record = await repository.FindAssetAsync(id, ct) ?? throw new MarketingException("Asset not found.", 404);
        var asset = MarketingJson.Read<AssetModel>(record.Json);
        await using var stream = await blobs.OpenReadAsync(record.BlobPath, ct)
            ?? throw new MarketingException("The stored asset bytes are unavailable.", 409);
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, ct);
        var bytes = output.ToArray();
        if (bytes.LongLength != asset.ByteLength || Hash(bytes) != asset.Sha256)
            throw new MarketingException("Asset integrity failed. Restore the original before exporting.", 409);
        return new(asset, bytes);
    }

    public async Task<AssetModel> SaveAsync(byte[] bytes, string fileName, string provenance, string role, CancellationToken ct, string? id = null)
    {
        if (bytes.Length == 0 || bytes.LongLength > options.Value.MaximumAssetBytes)
            throw new MarketingException("Choose a nonempty image smaller than 25 MB.", 413);
        var contentType = DetectType(bytes);
        if (contentType is null) throw new MarketingException("Only original PNG, JPEG and WebP images are accepted.", 415);
        var safeName = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 150) safeName = "image.png";
        if (provenance.Length > 4000 || string.IsNullOrWhiteSpace(provenance))
            throw new MarketingException("Record the asset source before importing it.");
        if (role is not ("front" or "fabric" or "detail" or "reference" or "generated" or "card"))
            throw new MarketingException("Choose a recognized image role.");
        id ??= Guid.NewGuid().ToString("N");
        if (id.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new MarketingException("Invalid asset identifier.");
        var hash = Hash(bytes);
        var existing = await repository.FindAssetAsync(id, ct);
        if (existing is not null)
        {
            var existingAsset = MarketingJson.Read<AssetModel>(existing.Json);
            if (existingAsset.Sha256 != hash) throw new MarketingException("This immutable asset identifier already contains different bytes.", 409);
            return existingAsset;
        }
        var path = $"originals/{id}/{hash}";
        using var input = new MemoryStream(bytes, false);
        await blobs.SaveAsync(path, input, contentType, ct);
        var asset = new AssetModel(id, safeName, contentType, bytes.LongLength, hash, provenance,
            $"/api/marketing/assets/{id}", role);
        await repository.SaveAssetAsync(asset, path, ct);
        return asset;
    }

    public async Task<byte[]> ExportDraftAsync(string id, CancellationToken ct)
    {
        var draft = await repository.FindDraftAsync(id, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (draft.ApprovedRevision != draft.Revision)
            throw new MarketingException("Approve the current draft with selected media before exporting.", 409);
        var exportPath = $"exports/{draft.Id}/{draft.Revision}.zip";
        await using var stored = await blobs.OpenReadAsync(exportPath, ct);
        if (stored is not null)
        {
            using var existing = new MemoryStream();
            await stored.CopyToAsync(existing, ct);
            return existing.ToArray();
        }
        var assets = new List<PrivateAssetContent>();
        foreach (var assetId in draft.MediaIds) assets.Add(await ReadAsync(assetId, ct));
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "caption.txt", Encoding.UTF8.GetBytes(draft.Caption), draft.UpdatedAt);
            AddEntry(archive, "manifest.json", Encoding.UTF8.GetBytes(MarketingJson.Write(new {
                schemaVersion = 1, draft, assets = assets.Select(x => x.Asset).ToArray(),
                publication = "manual-export-only", exportedRevision = draft.Revision })), draft.UpdatedAt);
            for (var index = 0; index < assets.Count; index++)
                AddEntry(archive, $"media/{index + 1:00}-{assets[index].Asset.FileName}", assets[index].Bytes, draft.UpdatedAt);
        }
        var result = buffer.ToArray();
        using var zip = new MemoryStream(result, false);
        await blobs.SaveAsync(exportPath, zip, "application/zip", ct);
        return result;
    }

    private static void AddEntry(ZipArchive archive, string name, byte[] bytes, DateTimeOffset timestamp)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = timestamp.Year >= 1980 ? timestamp : new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static string? DetectType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return "image/jpeg";
        if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return "image/webp";
        return null;
    }
}
