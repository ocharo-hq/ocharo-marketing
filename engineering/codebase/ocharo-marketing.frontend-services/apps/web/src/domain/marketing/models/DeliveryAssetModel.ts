/** Records server evidence used by the operator workflow. */
export interface DeliveryAssetModel {
  assetId: string;
  sha256: string;
  contentType: string;
  fileName: string;
}
