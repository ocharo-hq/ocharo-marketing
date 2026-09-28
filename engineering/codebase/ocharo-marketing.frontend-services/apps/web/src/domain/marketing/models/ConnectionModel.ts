/** Represents connection data used by the marketing workspace. */
export interface ConnectionModel {
  /** The id value. */
  id: string;

  /** The name value. */
  name: string;

  /** The kind value. */
  kind: 'social' | 'image' | 'text';

  /** The status value. */
  status: string;

  /** The capabilities value. */
  capabilities: string[];

  /** The setupHint value. */
  setupHint: string;

  /** The documentationUrl value. */
  documentationUrl: string;
  /** Additive readiness evidence; an absent value is safely unavailable. */
  connectionRevision?: number;
  verificationStatus?: string;
  executionEnabled?: boolean;
  metricReadsEnabled?: boolean;
  accountId?: string | null;
  destinationId?: string | null;
  displayName?: string | null;
  verifiedAt?: string | null;
  safeMessage?: string | null;
  canVerify?: boolean;
}
