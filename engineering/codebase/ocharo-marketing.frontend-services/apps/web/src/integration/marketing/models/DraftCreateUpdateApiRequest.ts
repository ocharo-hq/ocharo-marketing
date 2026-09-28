import type { DraftCompositionModel } from '../../../domain/marketing';

/** Defines the DraftCreateUpdateApiRequest wire payload. */
export interface DraftCreateUpdateApiRequest {
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

  /** The expectedRevision value. */
  expectedRevision?: number;

  /** The reviewed card recipe retained for subsequent edits. */
  composition?: DraftCompositionModel | null;
}
