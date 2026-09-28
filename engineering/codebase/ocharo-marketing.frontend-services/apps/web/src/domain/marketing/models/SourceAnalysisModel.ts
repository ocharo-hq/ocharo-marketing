import type { SourceAnalysisSourceModel } from './SourceAnalysisSourceModel';
/** Stores source-linked wording without changing catalogue facts. */
export interface SourceAnalysisModel {
  id: string;
  revision: number;
  status: 'pending' | 'ready' | 'reviewed' | 'unavailable' | 'failed';
  provider: string;
  model: string | null;
  locale: string;
  prompt: string;
  sources: SourceAnalysisSourceModel[];
  suggestedText: string | null;
  reviewedText: string | null;
  message: string;
  createdAt: string;
  updatedAt: string;
  reviewedAt: string | null;
}
