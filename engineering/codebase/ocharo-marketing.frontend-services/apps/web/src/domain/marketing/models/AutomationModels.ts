import type { DraftModel } from './DraftModel';
import type { ScheduleModel } from './ScheduleModel';

/** A recurring publishing slot for one channel in Asia/Tashkent time. Days use 1 (Monday) to 7 (Sunday). */
export interface PostingSlotModel {
  id: string;
  channel: string;
  days: number[];
  time: string;
  enabled: boolean;
}

/** Posting rhythm and draft-creation rules; slots create plans, never deliveries. */
export interface AutomationSettingsModel {
  slots: PostingSlotModel[];
  draftNewCatalogueProducts: boolean;
  defaultPresetId: string;
  defaultChannels: string[];
  defaultLocale: string;
  captionTemplates: Record<string, string>;
  /** Unattended catalogue sync interval in hours; 0 = off. */
  autoSyncHours: number;
  /** Unattended slot planning horizon in days; 0 = off. */
  autoFillDays: number;
}

/** Last unattended automation passes. */
export interface AutomationRunModel {
  lastSyncAt: string | null;
  lastSync: CatalogueSyncReportModel | null;
  lastFillAt: string | null;
  lastFillPlanned: number;
  lastError: string | null;
  lastErrorAt: string | null;
}

/** What a catalogue sync did. */
export interface CatalogueSyncReportModel {
  received: number;
  created: number;
  updated: number;
  imagesStored: number;
  newProductIds: string[];
  draftIds: string[];
  warnings: string[];
}

/** Plans created by filling slots, and what could not be placed. */
export interface SlotFillReportModel {
  planned: ScheduleModel[];
  skipped: string[];
}

/** A published Ocharo Blog story. */
export interface BlogStoryModel {
  title: string;
  link: string;
  excerpt: string;
  topic: string | null;
  publishedAt: string | null;
  draftId: string | null;
}

/** Drafts created in one bulk action. */
export type BulkDraftResult = DraftModel[];
