/** Records server evidence used by the operator workflow. */
export interface ProviderReceiptModel {
  provider: string;
  stage: string;
  externalId: string;
  relatedIds: Record<string, string> | null;
}
