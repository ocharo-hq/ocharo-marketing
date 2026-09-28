namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record DeliveryAssetModel(string AssetId, string Sha256, string ContentType, string FileName);
