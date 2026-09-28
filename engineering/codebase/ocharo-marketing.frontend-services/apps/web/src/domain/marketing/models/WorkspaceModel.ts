import type { PublicationOperationModel } from './PublicationOperationModel';
import type { DeliverySettingsModel } from './DeliverySettingsModel';
import type { BrandModel } from './BrandModel';
import type { ProductModel } from './ProductModel';
import type { AssetModel } from './AssetModel';
import type { PresetModel } from './PresetModel';
import type { DraftModel } from './DraftModel';
import type { ScheduleModel } from './ScheduleModel';
import type { ConnectionModel } from './ConnectionModel';
import type { MetricModel } from './MetricModel';
import type { GenerationJobModel } from './GenerationJobModel';

/** Represents the current local marketing workspace. */
export interface WorkspaceModel {
  /** The brand presentation and timezone. */
  brand: BrandModel;

  /** The imported product records. */
  products: ProductModel[];

  /** The recorded media library. */
  assets: AssetModel[];

  /** The available card compositions. */
  presets: PresetModel[];

  /** The saved post revisions. */
  drafts: DraftModel[];

  /** The planned or dispatched publications. */
  schedules: ScheduleModel[];

  /** The actual channel and generation configuration. */
  connections: ConnectionModel[];

  /** The available observation values. */
  metrics: MetricModel[];

  /** The recorded image and card jobs. */
  generationJobs: GenerationJobModel[];
  /** Recorded queue evidence; older APIs cannot enable delivery. */
  publicationOperations?: PublicationOperationModel[];
  /** Separate server gates for provider activity. */
  deliverySettings?: DeliverySettingsModel;
}
