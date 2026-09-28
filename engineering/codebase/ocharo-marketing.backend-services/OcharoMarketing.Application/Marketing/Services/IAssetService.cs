using OcharoMarketing.Application.Marketing.Models;

namespace OcharoMarketing.Application.Marketing.Services;

/// <summary>Defines private bounded immutable marketing asset storage.</summary>
public interface IAssetService
{
    Task<PrivateAssetContent> ReadAsync(string id, CancellationToken cancellationToken);
    Task<AssetModel> SaveAsync(byte[] bytes, string fileName, string provenance, string role, CancellationToken cancellationToken, string? id = null);
    Task<byte[]> ExportDraftAsync(string id, CancellationToken cancellationToken);
}
