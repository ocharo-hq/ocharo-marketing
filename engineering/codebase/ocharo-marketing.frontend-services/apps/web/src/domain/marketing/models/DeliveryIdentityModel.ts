/** Records server evidence used by the operator workflow. */
export interface DeliveryIdentityModel {
  connectionId: string;
  provider: string;
  revision: number;
  accountId: string;
  destinationId: string;
  displayName: string;
  capabilities: string[];
  verifiedAt: string;
}
