using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Domain.Marketing.Entities;

namespace OcharoMarketing.Infrastructure.Marketing.Services;

/// <summary>The checks every stored asset passes, whichever store keeps its bytes.</summary>
internal static class AssetRules
{
    /// <summary>Checks an upload and returns the asset it becomes; refuses an empty, oversized or non-image upload, a missing
    /// source, an unknown role and a malformed id.</summary>
    public static AssetModel Describe(byte[] bytes, string fileName, string provenance, string role, string? id, long maximumBytes)
    {
        if (bytes.Length == 0 || bytes.LongLength > maximumBytes)
            throw new MarketingException("Choose a nonempty image smaller than 25 MB.", 413);
        var contentType = DetectType(bytes) ?? throw new MarketingException("Only original PNG, JPEG and WebP images are accepted.", 415);
        var safeName = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 150) safeName = "image.png";
        if (provenance.Length > 4000 || string.IsNullOrWhiteSpace(provenance))
            throw new MarketingException("Record the asset source before importing it.");
        if (role is not ("front" or "fabric" or "detail" or "reference" or "generated" or "card"))
            throw new MarketingException("Choose a recognized image role.");
        id ??= Guid.NewGuid().ToString("N");
        if (id.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new MarketingException("Invalid asset identifier.");
        return new AssetModel(id, safeName, contentType, bytes.LongLength, Hash(bytes), provenance, $"/api/marketing/assets/{id}", role);
    }

    /// <summary>Returns a stored asset, or refuses other bytes under its immutable id.</summary>
    public static AssetModel Same(AssetRecord existing, string sha256)
    {
        var asset = MarketingJson.Read<AssetModel>(existing.Json);
        if (asset.Sha256 != sha256) throw new MarketingException("This immutable asset identifier already contains different bytes.", 409);
        return asset;
    }

    /// <summary>Finds an asset, or refuses an unknown id.</summary>
    public static async Task<AssetRecord> FindAsync(IMarketingRepository repository, string id, CancellationToken ct) =>
        await repository.FindAssetAsync(id, ct) ?? throw new MarketingException("Asset not found.", 404);

    /// <summary>Returns the lowercase hex SHA-256 of the bytes.</summary>
    public static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>The file extension WoW2 Files checks an image's type against.</summary>
    public static string Extension(string contentType) => contentType switch
    {
        "image/png" => "png",
        "image/jpeg" => "jpg",
        _ => "webp",
    };

    private static string? DetectType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) return "image/jpeg";
        if (bytes.Length >= 12 && Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return "image/webp";
        return null;
    }
}

/// <summary>Builds an approved draft revision's export ZIP, and names the local blob of one made before WoW2 Files.</summary>
internal static class DraftExports
{
    private const string LocalFolder = "exports";

    /// <summary>Finds a draft whose current revision is approved, or refuses the export.</summary>
    public static async Task<DraftModel> ApprovedAsync(IMarketingRepository repository, string id, CancellationToken ct)
    {
        var draft = await repository.FindDraftAsync(id, ct) ?? throw new MarketingException("Draft not found.", 404);
        if (draft.ApprovedRevision != draft.Revision)
            throw new MarketingException("Approve the current draft with selected media before exporting.", 409);
        return draft;
    }

    /// <summary>The local blob of a revision's export: <c>exports/{draft}/{revision}.zip</c>.</summary>
    public static string LocalPath(string draftId, int revision) =>
        $"{LocalFolder}/{draftId}/{revision.ToString(CultureInfo.InvariantCulture)}.zip";

    /// <summary>The prefix every local export blob starts with.</summary>
    public static string LocalPrefix => $"{LocalFolder}/";

    /// <summary>Reads the draft and revision a local export blob belongs to; false for any other blob.</summary>
    public static bool TryParseLocalPath(string path, out string draftId, out int revision)
    {
        var parts = path.Split('/');
        draftId = parts.Length == 3 ? parts[1] : string.Empty;
        revision = 0;
        return parts.Length == 3 && parts[0] == LocalFolder && draftId.Length > 0 && parts[2].EndsWith(".zip", StringComparison.Ordinal)
            && int.TryParse(parts[2][..^4], NumberStyles.None, CultureInfo.InvariantCulture, out revision);
    }

    /// <summary>Builds the ZIP: the caption, a manifest of the draft and its assets, and each selected asset's checked bytes.</summary>
    public static async Task<byte[]> BuildAsync(DraftModel draft, Func<string, Task<PrivateAssetContent>> read)
    {
        var assets = new List<PrivateAssetContent>();
        foreach (var assetId in draft.MediaIds) assets.Add(await read(assetId));
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
        return buffer.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, byte[] bytes, DateTimeOffset timestamp)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = timestamp.Year >= 1980 ? timestamp : new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var stream = entry.Open();
        stream.Write(bytes);
    }
}
