import type { DraftModel } from './DraftModel';
import type { DeliveryIdentityModel } from './DeliveryIdentityModel';
import type { DeliveryAssetModel } from './DeliveryAssetModel';
/** The immutable content and destination authorized for one delivery. */
export interface PublicationSnapshotModel {
  draft: DraftModel;
  identity: DeliveryIdentityModel;
  media: DeliveryAssetModel[];
}
