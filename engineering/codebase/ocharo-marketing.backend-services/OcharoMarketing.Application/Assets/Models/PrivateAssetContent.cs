namespace OcharoMarketing.Application.Assets.Models;

/// <summary>Represents PrivateAssetContent data for the local marketing workflow.</summary>
public sealed record PrivateAssetContent(AssetModel Asset, byte[] Bytes);
