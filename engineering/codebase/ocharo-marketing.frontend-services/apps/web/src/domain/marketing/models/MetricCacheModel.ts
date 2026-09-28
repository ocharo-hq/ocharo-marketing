import type { MetricObservationModel } from './MetricObservationModel';
import type { MetricAttemptModel } from './MetricAttemptModel';
/** Holds saved observations separately from the last provider-read attempt. */
export interface MetricCacheModel {
  scheduleId: string;
  observations: MetricObservationModel[];
  /** Actual persisted snapshots; absent on an older server. */
  history?: MetricObservationModel[];
  /** Indicates that the server returned its bounded most recent history. */
  historyTruncated?: boolean;
  lastAttempt: MetricAttemptModel | null;
}
