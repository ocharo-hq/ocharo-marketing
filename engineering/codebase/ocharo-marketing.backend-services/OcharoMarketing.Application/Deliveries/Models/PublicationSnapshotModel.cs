using OcharoMarketing.Application.Posts.Models;

namespace OcharoMarketing.Application.Deliveries.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record PublicationSnapshotModel(
    DraftModel Draft,
    DeliveryIdentityModel Identity,
    DeliveryAssetModel[] Media);
