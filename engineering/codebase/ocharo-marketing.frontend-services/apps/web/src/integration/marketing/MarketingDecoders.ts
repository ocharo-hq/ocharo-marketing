import { ApiFailureFactory, type ApiFailure } from '@wow-two-beta/ui-vue/foundation/http';
import { ResultExtensions, type Result } from '@wow-two-beta/ui-vue/foundation/results';

/** @internal A field validator used only at the wire boundary. */
type Guard = (value: unknown) => boolean;

/** @internal Validates a JSON object. */
const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value);
/** @internal Validates a string scalar. */
const StringField: Guard = (value) => typeof value === 'string';
/** @internal Validates a finite number scalar. */
const NumberField: Guard = (value) => typeof value === 'number' && Number.isFinite(value);
/** @internal Validates a boolean scalar. */
const BooleanField: Guard = (value) => typeof value === 'boolean';
/** @internal Validates a nullable string scalar. */
const NullableString: Guard = (value) => value === null || StringField(value);
/** @internal Validates a nullable number scalar. */
const NullableNumber: Guard = (value) => value === null || NumberField(value);
/** @internal Validates an array using its item rule. */
const list =
  (guard: Guard): Guard =>
  (value) =>
    Array.isArray(value) && value.every(guard);
/** @internal Validates required fields without changing unrecognized server data. */
const shape =
  (fields: Record<string, Guard>): Guard =>
  (value) =>
    isObject(value) && Object.entries(fields).every(([key, guard]) => guard(value[key]));

/** Validates every rendering and workflow field used by the interface. */
export const MarketingDecoders = {
  product: shape({
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
  }),
  asset: shape({
    id: StringField,
    fileName: StringField,
    contentType: StringField,
    byteLength: NumberField,
    sha256: StringField,
    provenance: StringField,
    url: StringField,
    role: StringField,
  }),
  preset: shape({
    id: StringField,
    name: StringField,
    family: StringField,
    surface: StringField,
    description: StringField,
    requiresFabric: BooleanField,
    width: NumberField,
    height: NumberField,
  }),
  draft: shape({
    id: StringField,
    title: StringField,
    productId: StringField,
    presetId: StringField,
    locale: StringField,
    caption: StringField,
    channels: list(StringField),
    mediaIds: list(StringField),
    composition: (value) =>
      value === undefined ||
      value === null ||
      shape({
        title: StringField,
        subtitle: StringField,
        fabricAssetId: NullableString,
        scale: NumberField,
        offsetX: NumberField,
        offsetY: NumberField,
        boundMediaIds: (item) => item === undefined || item === null || list(StringField)(item),
      })(value),
    revision: NumberField,
    approvedRevision: NullableNumber,
    status: StringField,
    createdAt: StringField,
    updatedAt: StringField,
  }),
  schedule: shape({
    id: StringField,
    draftId: StringField,
    draftRevision: NumberField,
    draftTitle: (value) => value === undefined || NullableString(value),
    channel: StringField,
    scheduledAt: StringField,
    status: StringField,
    message: NullableString,
    externalId: NullableString,
  }),
  connection: shape({
    connectionRevision: (value) => value === undefined || NumberField(value),
    verificationStatus: (value) => value === undefined || StringField(value),
    executionEnabled: (value) => value === undefined || BooleanField(value),
    metricReadsEnabled: (value) => value === undefined || BooleanField(value),
    accountId: (value) => value === undefined || NullableString(value),
    destinationId: (value) => value === undefined || NullableString(value),
    displayName: (value) => value === undefined || NullableString(value),
    verifiedAt: (value) => value === undefined || NullableString(value),
    safeMessage: (value) => value === undefined || NullableString(value),
    canVerify: (value) => value === undefined || BooleanField(value),
    id: StringField,
    name: StringField,
    kind: (value) => ['social', 'image', 'text'].includes(String(value)),
    status: StringField,
    capabilities: list(StringField),
    setupHint: StringField,
    documentationUrl: StringField,
  }),
  metric: shape({
    channel: StringField,
    name: StringField,
    value: NullableNumber,
    status: StringField,
    observedAt: NullableString,
    explanation: StringField,
  }),
  job: shape({
    id: StringField,
    kind: StringField,
    provider: StringField,
    prompt: StringField,
    productId: NullableString,
    status: StringField,
    createdAt: StringField,
    outputAssetIds: list(StringField),
    message: NullableString,
  }),
  preview: shape({ html: StringField, issues: list(StringField) }),
  scan: shape({ summary: StringField, suggestions: list(StringField), issues: list(StringField) }),
  text: shape({ text: StringField, provider: StringField, model: StringField }),
  deliverySettings: shape({
    publishingEnabled: BooleanField,
    metricReadsEnabled: BooleanField,
    accountVerificationEnabled: BooleanField,
  }),
  identity: shape({
    connectionId: StringField,
    provider: StringField,
    revision: NumberField,
    accountId: StringField,
    destinationId: StringField,
    displayName: StringField,
    capabilities: list(StringField),
    verifiedAt: StringField,
  }),
  deliveryAsset: shape({ assetId: StringField, sha256: StringField, contentType: StringField, fileName: StringField }),
  receipt: shape({
    provider: StringField,
    stage: StringField,
    externalId: StringField,
    relatedIds: (value) => value === null || (isObject(value) && Object.values(value).every(StringField)),
  }),
  publication(value: unknown): boolean {
    return shape({
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
      receipt: (item) => item === null || MarketingDecoders.receipt(item),
      snapshot: shape({
        draft: MarketingDecoders.draft,
        identity: MarketingDecoders.identity,
        media: list(MarketingDecoders.deliveryAsset),
      }),
      canCancel: BooleanField,
      canReconcile: BooleanField,
      reconciliationMessage: StringField,
    })(value);
  },
  observation: shape({
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
  }),
  metricCache(value: unknown): boolean {
    return shape({
      scheduleId: StringField,
      observations: list(MarketingDecoders.observation),
      history: (value) => value === undefined || list(MarketingDecoders.observation)(value),
      historyTruncated: (value) => value === undefined || BooleanField(value),
      lastAttempt: (item) =>
        item === null || shape({ attemptedAt: StringField, status: StringField, message: StringField })(item),
    })(value);
  },
  sourceAnalysis: shape({
    id: StringField,
    revision: NumberField,
    status: (value) => ['pending', 'ready', 'reviewed', 'unavailable', 'failed'].includes(String(value)),
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
  }),
  sourceAnalyses(value: unknown): boolean {
    return list(MarketingDecoders.sourceAnalysis)(value);
  },
  automation: shape({
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
    captionTemplates: isObject,
    autoSyncHours: NumberField,
    autoFillDays: NumberField,
  }),
  automationRun: shape({
    lastSyncAt: NullableString,
    lastFillAt: NullableString,
    lastFillPlanned: NumberField,
    lastError: NullableString,
    lastErrorAt: NullableString,
  }),
  syncReport: shape({
    received: NumberField,
    created: NumberField,
    updated: NumberField,
    imagesStored: NumberField,
    newProductIds: list(StringField),
    draftIds: list(StringField),
    warnings: list(StringField),
  }),
  fillReport(value: unknown): boolean {
    return shape({ planned: list(MarketingDecoders.schedule), skipped: list(StringField) })(value);
  },
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
  channelStrategy: shape({
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
  }),
  engagement: shape({
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
  }),
  engagementObservation(value: unknown): boolean {
    return shape({
      observation: MarketingDecoders.engagement,
      recordedAt: StringField,
      recordedBy: StringField,
      source: (value) => value === 'manual',
      engagementRate: NullableNumber,
    })(value);
  },
  channelStrategies(value: unknown): boolean {
    return list(MarketingDecoders.channelStrategy)(value);
  },
  operations(value: unknown): boolean {
    return shape({
      observedAt: StringField,
      weekStartsAt: StringField,
      weekEndsAt: StringField,
      timeZone: StringField,
      channels: list(
        shape({
          strategy: MarketingDecoders.channelStrategy,
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
      automation: MarketingDecoders.automationRun,
      delivery: MarketingDecoders.deliverySettings,
      approvedUnplanned: NumberField,
      queued: NumberField,
      running: NumberField,
      failed: NumberField,
      unknown: NumberField,
      held: NumberField,
      attentionPlans: list(MarketingDecoders.schedule),
      attentionDeliveries: list(MarketingDecoders.publication),
      observationCount: NumberField,
      observations: list(MarketingDecoders.engagementObservation),
      providerObservations: list(MarketingDecoders.observation),
    })(value);
  },
  caption: shape({ caption: StringField }),
  drafts(value: unknown): boolean {
    return list(MarketingDecoders.draft)(value);
  },
  decode<T>(guard: Guard, value: unknown): Result<T, ApiFailure> {
    return guard(value) ? ResultExtensions.ok(value as T) : ResultExtensions.fail(ApiFailureFactory.create('protocol'));
  },
  workspace(value: unknown): boolean {
    return shape({
      brand: shape({ name: StringField, timezone: StringField, handle: NullableString }),
      products: list(MarketingDecoders.product),
      assets: list(MarketingDecoders.asset),
      presets: list(MarketingDecoders.preset),
      drafts: list(MarketingDecoders.draft),
      schedules: list(MarketingDecoders.schedule),
      connections: list(MarketingDecoders.connection),
      metrics: list(MarketingDecoders.metric),
      generationJobs: list(MarketingDecoders.job),
      publicationOperations: (item) => item === undefined || list(MarketingDecoders.publication)(item),
      deliverySettings: (item) => item === undefined || MarketingDecoders.deliverySettings(item),
    })(value);
  },
} as const;
