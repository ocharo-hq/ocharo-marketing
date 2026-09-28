namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record PublicationSnapshotModel(DraftModel Draft, DeliveryIdentityModel Identity, DeliveryAssetModel[] Media);
