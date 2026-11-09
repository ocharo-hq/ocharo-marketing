using OcharoMarketing.Application.Assets.Models;

namespace OcharoMarketing.Application.Assets.Services;

/// <summary>Defines private bounded immutable marketing asset storage: WoW2 Files when the host names one, the local
/// blob
/// folder otherwise and for assets stored before Files.</summary>
public interface IAssetService
{
    /// <summary>Reads an asset's bytes for server-side work (renders, exports, delivery, AI pipelines), checked against
    /// its
    /// SHA-256.</summary>
    Task<PrivateAssetContent> ReadAsync(string id, CancellationToken cancellationToken);

    /// <summary>Opens an asset for the asset route: a Files link to redirect to, or checked local bytes.</summary>
    Task<AssetContent> OpenAsync(string id, CancellationToken cancellationToken);

    Task<AssetModel> SaveAsync(
        byte[] bytes,
        string fileName,
        string provenance,
        string role,
        CancellationToken cancellationToken,
        string? id = null);

    /// <summary>Exports an approved draft revision as a ZIP, the same bytes on every call for that revision.</summary>
    Task<byte[]> ExportDraftAsync(string id, CancellationToken cancellationToken);
}
