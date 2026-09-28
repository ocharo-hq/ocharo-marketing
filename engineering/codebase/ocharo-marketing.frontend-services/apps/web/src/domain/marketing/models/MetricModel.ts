/** Represents metric data used by the marketing workspace. */
export interface MetricModel {
  /** The channel value. */
  channel: string;

  /** The name value. */
  name: string;

  /** The value value. */
  value: number | null;

  /** The status value. */
  status: string;

  /** The observedAt value. */
  observedAt: string | null;

  /** The explanation value. */
  explanation: string;
}
