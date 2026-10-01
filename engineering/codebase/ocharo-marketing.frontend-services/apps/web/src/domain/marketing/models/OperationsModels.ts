import type { AutomationRunModel } from './AutomationModels';
import type { DeliverySettingsModel } from './DeliverySettingsModel';
import type { ScheduleModel } from './ScheduleModel';
import type { PublicationOperationModel } from './PublicationOperationModel';
import type { MetricObservationModel } from './MetricObservationModel';

/** An operator-owned plan, independently versioned for each channel. */
export interface ChannelStrategyModel {
  channel: string;
  revision: number;
  active: boolean;
  audience: string;
  purpose: string;
  formats: string;
  pillars: string;
  owner: string;
  profileUrl: string;
  weeklyTarget: number;
  reviewEveryDays: number;
  updatedAt: string | null;
}
/** A manual cumulative snapshot; blank measurements remain unknown. */
export interface EngagementInput {
  id: string;
  channel: string;
  postUrl: string;
  title: string;
  scheduleId: string | null;
  publishedAt: string;
  observedAt: string;
  views: number | null;
  reach: number | null;
  likes: number | null;
  comments: number | null;
  shares: number | null;
  saves: number | null;
  clicks: number | null;
  enquiries: number | null;
  orders: number | null;
  evidenceNote: string;
}
export interface EngagementObservationModel {
  observation: EngagementInput;
  recordedAt: string;
  recordedBy: string;
  source: 'manual';
  engagementRate: number | null;
}
export interface WorkerHealthModel {
  worker: string;
  status: string;
  startedAt: string | null;
  checkedAt: string | null;
  lastSuccessAt: string | null;
  lastError: string | null;
  pollSeconds: number;
}
export interface ChannelSummaryModel {
  strategy: ChannelStrategyModel;
  weeklySlots: number;
  upcomingPlans: number;
  confirmedReceiptsThisWeek: number;
  manualPostsThisWeek: number;
  duePlans: number;
  lastObservationAt: string | null;
  warnings: string[];
}
export interface OperationsOverviewModel {
  observedAt: string;
  weekStartsAt: string;
  weekEndsAt: string;
  timeZone: string;
  channels: ChannelSummaryModel[];
  workers: WorkerHealthModel[];
  automation: AutomationRunModel;
  delivery: DeliverySettingsModel;
  approvedUnplanned: number;
  queued: number;
  running: number;
  failed: number;
  unknown: number;
  held: number;
  attentionPlans: ScheduleModel[];
  attentionDeliveries: PublicationOperationModel[];
  observations: EngagementObservationModel[];
  providerObservations: MetricObservationModel[];
  observationCount: number;
}
