/** Requests one idempotent, explicit source-image analysis. */
export interface SourceAnalysisCreateApiRequest {
  operationId: string;
  assetIds: string[];
  provider: 'openai';
  locale: 'uz' | 'ru' | 'en';
  prompt?: string;
}
