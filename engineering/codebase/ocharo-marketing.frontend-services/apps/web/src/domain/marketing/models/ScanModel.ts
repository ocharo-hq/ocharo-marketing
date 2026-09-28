/** Represents scan data used by the marketing workspace. */
export interface ScanModel {
  /** The summary value. */
  summary: string;

  /** The suggestions value. */
  suggestions: string[];

  /** The issues value. */
  issues: string[];
}
