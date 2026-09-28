/** Records server evidence used by the operator workflow. */
export interface MetricAttemptModel {
  attemptedAt: string;
  status: string;
  message: string;
}
