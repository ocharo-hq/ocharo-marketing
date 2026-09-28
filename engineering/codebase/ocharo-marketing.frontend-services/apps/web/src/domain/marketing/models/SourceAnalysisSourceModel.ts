/** Records server evidence used by the operator workflow. */
export interface SourceAnalysisSourceModel {
  assetId: string;
  sha256: string;
  fileName: string;
  provenance: string;
}
