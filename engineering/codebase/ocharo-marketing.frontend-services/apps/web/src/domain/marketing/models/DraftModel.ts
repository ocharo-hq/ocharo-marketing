import type { DraftCompositionModel } from './DraftCompositionModel';

/** Represents draft data used by the marketing workspace. */
export interface DraftModel {
  /** The id value. */
  id: string;

  /** The title value. */
  title: string;

  /** The productId value. */
  productId: string;

  /** The presetId value. */
  presetId: string;

  /** The locale value. */
  locale: string;

  /** The caption value. */
  caption: string;

  /** The channels value. */
  channels: string[];

  /** The mediaIds value. */
  mediaIds: string[];

  /** The revision value. */
  revision: number;

  /** The persisted recipe, absent on older drafts. */
  composition?: DraftCompositionModel | null;

  /** The approvedRevision value. */
  approvedRevision: number | null;

  /** The status value. */
  status: string;

  /** The createdAt value. */
  createdAt: string;

  /** The updatedAt value. */
  updatedAt: string;
}
