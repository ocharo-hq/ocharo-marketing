/** Records server evidence used by the operator workflow. */
export interface MetricObservationModel {
  id: string;
  scheduleId: string;
  connectionId: string;
  provider: string;
  receiptId: string;
  name: string;
  value: number | null;
  availability: string;
  observedAt: string | null;
  reason: string | null;
  scope: string;
  aggregation: string;
}
