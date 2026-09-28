/** Represents schedule data used by the marketing workspace. */
export interface ScheduleModel {
  /** The id value. */
  id: string;

  /** The draftId value. */
  draftId: string;

  /** The draftRevision value. */
  draftRevision: number;

  /** The title captured from the scheduled revision, absent on older plans. */
  draftTitle?: string | null;

  /** The channel value. */
  channel: string;

  /** The scheduledAt value. */
  scheduledAt: string;

  /** The status value. */
  status: string;

  /** The message value. */
  message: string | null;

  /** The externalId value. */
  externalId: string | null;
}
