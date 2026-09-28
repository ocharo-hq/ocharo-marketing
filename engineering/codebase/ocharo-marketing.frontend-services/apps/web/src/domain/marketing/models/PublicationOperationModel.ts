import type { PublicationSnapshotModel } from './PublicationSnapshotModel';
import type { ProviderReceiptModel } from './ProviderReceiptModel';
/** Records the frozen delivery intent and its actual outcome. */
export interface PublicationOperationModel {
  id: string;
  scheduleId: string;
  provider: string;
  connectionId: string;
  connectionRevision: number;
  status: string;
  phase: string;
  dueAt: string;
  authorizedAt: string;
  dispatchStartedAt: string | null;
  updatedAt: string;
  message: string | null;
  receipt: ProviderReceiptModel | null;
  snapshot: PublicationSnapshotModel;
  canCancel: boolean;
  canReconcile: boolean;
  reconciliationMessage: string;
}
