import { ApiFailureFactory, type ApiFailure } from '@wow-two-beta/fe/foundation/http';
import { ResultExtensions, type Result } from '@wow-two-beta/fe/foundation/results';

/** @internal Marks a wire value a rule refused. */
const Invalid = Symbol('invalid');

/**
 * @internal Reads one wire value into the value the model holds, or refuses it with `Invalid`. The API writes JSON
 * through the SDK preset, which omits null properties (`WhenWritingNull`): a rule sees `undefined` where an older
 * API sent `null`.
 */
export type MarketingRule = (value: unknown) => unknown;

/** @internal Validates a JSON object. */
const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value);
/** @internal Keeps a value its test accepts. */
const accept =
  (test: (value: unknown) => boolean): MarketingRule =>
  (value) =>
    test(value) ? value : Invalid;
/** @internal Reads a string scalar. */
const StringField = accept((value) => typeof value === 'string');
/** @internal Reads a finite number scalar. */
const NumberField = accept((value) => typeof value === 'number' && Number.isFinite(value));
/** @internal Reads a boolean scalar. */
const BooleanField = accept((value) => typeof value === 'boolean');
/** @internal Reads a JSON object without inspecting its entries. */
const ObjectField = accept(isObject);
/** @internal Reads one of the listed string values. */
const oneOf = (...values: readonly string[]): MarketingRule =>
  accept((value) => typeof value === 'string' && values.includes(value));
/** @internal Reads a value the API omits when it has none; the model holds `null` for both omission and `null`. */
const nullable =
  (rule: MarketingRule): MarketingRule =>
  (value) =>
    value === undefined || value === null ? null : rule(value);
/** @internal Reads a field an older API does not send; the model leaves it absent. */
const optional =
  (rule: MarketingRule): MarketingRule =>
  (value) =>
    value === undefined ? undefined : rule(value);
/** @internal Reads an array using its item rule. */
const list =
  (rule: MarketingRule): MarketingRule =>
  (value) => {
    if (!Array.isArray(value)) return Invalid;
    const items: unknown[] = [];
    for (const item of value) {
      const read = rule(item);
      if (read === Invalid) return Invalid;
      items.push(read);
    }
    return items;
  };
/** @internal Reads a string-keyed object using its value rule. */
const entries =
  (rule: MarketingRule): MarketingRule =>
  (value) => {
    if (!isObject(value)) return Invalid;
    const read: Record<string, unknown> = {};
    for (const [key, item] of Object.entries(value)) {
      const entry = rule(item);
      if (entry === Invalid) return Invalid;
      read[key] = entry;
    }
    return read;
  };
/** @internal Reads the listed fields and keeps unrecognized server data unchanged. */
const shape =
  (fields: Record<string, MarketingRule>): MarketingRule =>
  (value) => {
    if (!isObject(value)) return Invalid;
    const read: Record<string, unknown> = { ...value };
    for (const [key, rule] of Object.entries(fields)) {
      const field = rule(value[key]);
      if (field === Invalid) return Invalid;
      if (field === undefined) delete read[key];
      else read[key] = field;
    }
    return read;
  };

const NullableString = nullable(StringField);
const NullableNumber = nullable(NumberField);
/** @internal Holds an omitted value the interface never reads as `null`, without validating it. */
const AbsentAsNull: MarketingRule = (value) => value ?? null;

const product = shape({
  id: StringField,
  name: StringField,
  category: StringField,
  material: StringField,
  description: StringField,
  heroAssetId: StringField,
  assetIds: list(StringField),
  sizes: list(StringField),
  price: NullableNumber,
  currency: StringField,
  reviewStatus: StringField,
  sourceRevision: StringField,
  issues: list(StringField),
});
const asset = shape({
  id: StringField,
  fileName: StringField,
  contentType: StringField,
  byteLength: NumberField,
  sha256: StringField,
  provenance: StringField,
  url: StringField,
  role: StringField,
});
const preset = shape({
  id: StringField,
  name: StringField,
  family: StringField,
  surface: StringField,
  description: StringField,
  requiresFabric: BooleanField,
  width: NumberField,
  height: NumberField,
});
const draft = shape({
  id: StringField,
  title: StringField,
  productId: StringField,
  presetId: StringField,
  locale: StringField,
  caption: StringField,
  channels: list(StringField),
  mediaIds: list(StringField),
  composition: nullable(
    shape({
      title: StringField,
      subtitle: StringField,
      fabricAssetId: NullableString,
      scale: NumberField,
      offsetX: NumberField,
      offsetY: NumberField,
      boundMediaIds: nullable(list(StringField)),
    }),
  ),
  revision: NumberField,
  approvedRevision: NullableNumber,
  status: StringField,
  createdAt: StringField,
  updatedAt: StringField,
});
const schedule = shape({
  id: StringField,
  draftId: StringField,
  draftRevision: NumberField,
  draftTitle: NullableString,
  channel: StringField,
  scheduledAt: StringField,
  status: StringField,
  message: NullableString,
  externalId: NullableString,
});
const connection = shape({
  connectionRevision: optional(NumberField),
  verificationStatus: optional(StringField),
  executionEnabled: optional(BooleanField),
  metricReadsEnabled: optional(BooleanField),
  accountId: NullableString,
  destinationId: NullableString,
  displayName: NullableString,
  verifiedAt: NullableString,
  safeMessage: NullableString,
  canVerify: optional(BooleanField),
  id: StringField,
  name: StringField,
  kind: oneOf('social', 'image', 'text'),
  status: StringField,
  capabilities: list(StringField),
  setupHint: StringField,
  documentationUrl: StringField,
});
const metric = shape({
  channel: StringField,
  name: StringField,
  value: NullableNumber,
  status: StringField,
  observedAt: NullableString,
  explanation: StringField,
});
const job = shape({
  id: StringField,
  kind: StringField,
  provider: StringField,
  prompt: StringField,
  productId: NullableString,
  status: StringField,
  createdAt: StringField,
  outputAssetIds: list(StringField),
  message: NullableString,
});
const deliverySettings = shape({
  publishingEnabled: BooleanField,
  metricReadsEnabled: BooleanField,
  accountVerificationEnabled: BooleanField,
});
const identity = shape({
  connectionId: StringField,
  provider: StringField,
  revision: NumberField,
  accountId: StringField,
  destinationId: StringField,
  displayName: StringField,
  capabilities: list(StringField),
  verifiedAt: StringField,
});
const deliveryAsset = shape({
  assetId: StringField,
  sha256: StringField,
  contentType: StringField,
  fileName: StringField,
});
const receipt = shape({
  provider: StringField,
  stage: StringField,
  externalId: StringField,
  relatedIds: nullable(entries(StringField)),
});
const publication = shape({
  id: StringField,
  scheduleId: StringField,
  provider: StringField,
  connectionId: StringField,
  connectionRevision: NumberField,
  status: StringField,
  phase: StringField,
  dueAt: StringField,
  authorizedAt: StringField,
  dispatchStartedAt: NullableString,
  updatedAt: StringField,
  message: NullableString,
  receipt: nullable(receipt),
  snapshot: shape({ draft, identity, media: list(deliveryAsset) }),
  canCancel: BooleanField,
  canReconcile: BooleanField,
  reconciliationMessage: StringField,
});
const observation = shape({
  id: StringField,
  scheduleId: StringField,
  connectionId: StringField,
  provider: StringField,
  receiptId: StringField,
  name: StringField,
  value: NullableNumber,
  availability: StringField,
  observedAt: NullableString,
  reason: NullableString,
  scope: StringField,
  aggregation: StringField,
});
const metricCache = shape({
  scheduleId: StringField,
  observations: list(observation),
  history: optional(list(observation)),
  historyTruncated: optional(BooleanField),
  lastAttempt: nullable(shape({ attemptedAt: StringField, status: StringField, message: StringField })),
});
const sourceAnalysis = shape({
  id: StringField,
  revision: NumberField,
  status: oneOf('pending', 'ready', 'reviewed', 'unavailable', 'failed'),
  provider: StringField,
  model: NullableString,
  locale: StringField,
  prompt: StringField,
  sources: list(shape({ assetId: StringField, sha256: StringField, fileName: StringField, provenance: StringField })),
  suggestedText: NullableString,
  reviewedText: NullableString,
  message: StringField,
  createdAt: StringField,
  updatedAt: StringField,
  reviewedAt: NullableString,
});
const automation = shape({
  slots: list(
    shape({
      id: StringField,
      channel: StringField,
      days: list(NumberField),
      time: StringField,
      enabled: BooleanField,
    }),
  ),
  draftNewCatalogueProducts: BooleanField,
  defaultPresetId: StringField,
  defaultChannels: list(StringField),
  defaultLocale: StringField,
  captionTemplates: ObjectField,
  autoSyncHours: NumberField,
  autoFillDays: NumberField,
});
const syncReport = shape({
  received: NumberField,
  created: NumberField,
  updated: NumberField,
  imagesStored: NumberField,
  newProductIds: list(StringField),
  draftIds: list(StringField),
  warnings: list(StringField),
});
const automationRun = shape({
  lastSyncAt: NullableString,
  lastSync: AbsentAsNull,
  lastFillAt: NullableString,
  lastFillPlanned: NumberField,
  lastError: NullableString,
  lastErrorAt: NullableString,
});
const channelStrategy = shape({
  channel: StringField,
  revision: NumberField,
  active: BooleanField,
  audience: StringField,
  purpose: StringField,
  formats: StringField,
  pillars: StringField,
  owner: StringField,
  profileUrl: StringField,
  weeklyTarget: NumberField,
  reviewEveryDays: NumberField,
  updatedAt: NullableString,
});
const engagement = shape({
  id: StringField,
  channel: StringField,
  postUrl: StringField,
  title: StringField,
  scheduleId: NullableString,
  publishedAt: StringField,
  observedAt: StringField,
  views: NullableNumber,
  reach: NullableNumber,
  likes: NullableNumber,
  comments: NullableNumber,
  shares: NullableNumber,
  saves: NullableNumber,
  clicks: NullableNumber,
  enquiries: NullableNumber,
  orders: NullableNumber,
  evidenceNote: StringField,
});
const engagementObservation = shape({
  observation: engagement,
  recordedAt: StringField,
  recordedBy: StringField,
  source: oneOf('manual'),
  engagementRate: NullableNumber,
});

/** Reads every rendering and workflow field the interface uses, with omitted empty values as `null`. */
export const MarketingDecoders = {
  product,
  asset,
  preset,
  draft,
  drafts: list(draft),
  schedule,
  connection,
  metric,
  job,
  preview: shape({ html: StringField, issues: list(StringField) }),
  scan: shape({ summary: StringField, suggestions: list(StringField), issues: list(StringField) }),
  text: shape({ text: StringField, provider: StringField, model: StringField }),
  deliverySettings,
  identity,
  deliveryAsset,
  receipt,
  publication,
  /** A schedule's delivery, or `null` before one was queued. */
  publicationOrNone: nullable(publication),
  observation,
  metricCache,
  sourceAnalysis,
  sourceAnalyses: list(sourceAnalysis),
  automation,
  automationRun,
  syncReport,
  fillReport: shape({ planned: list(schedule), skipped: list(StringField) }),
  stories: list(
    shape({
      title: StringField,
      link: StringField,
      excerpt: StringField,
      topic: NullableString,
      publishedAt: NullableString,
      draftId: NullableString,
    }),
  ),
  channelStrategy,
  channelStrategies: list(channelStrategy),
  engagement,
  engagementObservation,
  operations: shape({
    observedAt: StringField,
    weekStartsAt: StringField,
    weekEndsAt: StringField,
    timeZone: StringField,
    channels: list(
      shape({
        strategy: channelStrategy,
        weeklySlots: NumberField,
        upcomingPlans: NumberField,
        confirmedReceiptsThisWeek: NumberField,
        manualPostsThisWeek: NumberField,
        duePlans: NumberField,
        lastObservationAt: NullableString,
        warnings: list(StringField),
      }),
    ),
    workers: list(
      shape({
        worker: StringField,
        status: StringField,
        startedAt: NullableString,
        checkedAt: NullableString,
        lastSuccessAt: NullableString,
        lastError: NullableString,
        pollSeconds: NumberField,
      }),
    ),
    automation: automationRun,
    delivery: deliverySettings,
    approvedUnplanned: NumberField,
    queued: NumberField,
    running: NumberField,
    failed: NumberField,
    unknown: NumberField,
    held: NumberField,
    attentionPlans: list(schedule),
    attentionDeliveries: list(publication),
    observationCount: NumberField,
    observations: list(engagementObservation),
    providerObservations: list(observation),
  }),
  caption: shape({ caption: StringField }),
  workspace: shape({
    brand: shape({ name: StringField, timezone: StringField, handle: NullableString }),
    products: list(product),
    assets: list(asset),
    presets: list(preset),
    drafts: list(draft),
    schedules: list(schedule),
    connections: list(connection),
    metrics: list(metric),
    generationJobs: list(job),
    publicationOperations: optional(list(publication)),
    deliverySettings: optional(deliverySettings),
  }),
  /** Whether `rule` accepts `value`. */
  accepts(rule: MarketingRule, value: unknown): boolean {
    return rule(value) !== Invalid;
  },
  /** Reads `value` through `rule`; a refused payload is a `protocol` failure. */
  decode<T>(rule: MarketingRule, value: unknown): Result<T, ApiFailure> {
    const read = rule(value);
    return read === Invalid
      ? ResultExtensions.fail(ApiFailureFactory.create('protocol'))
      : ResultExtensions.ok(read as T);
  },
} as const;
