import type { QueuePublicationApiRequest } from './models/QueuePublicationApiRequest';
import type { SourceAnalysisCreateApiRequest } from './models/SourceAnalysisCreateApiRequest';
import {
  ApiFailureFactory,
  createApiClient,
  rawEnvelope,
  type ResponseEnvelope,
} from '@wow-two-beta/ui-vue/foundation/http';

import type {
  AutomationRunModel,
  AutomationSettingsModel,
  BlogStoryModel,
  CatalogueSyncReportModel,
  SlotFillReportModel,
  ConnectionModel,
  PublicationOperationModel,
  MetricCacheModel,
  SourceAnalysisModel,
  AssetModel,
  CardPreviewModel,
  DraftModel,
  GenerationJobModel,
  ScanModel,
  ScheduleModel,
  TextSuggestionModel,
  WorkspaceModel,
} from '../../domain/marketing';
import { MarketingDecoders } from './MarketingDecoders';
import type { CardCreateApiRequest } from './models/CardCreateApiRequest';
import type { DraftCreateUpdateApiRequest } from './models/DraftCreateUpdateApiRequest';
import type { GenerationCreateApiRequest } from './models/GenerationCreateApiRequest';

/** Preserves structured ProblemDetails while keeping raw successful DTOs. */
const MarketingEnvelope: ResponseEnvelope = {
  unwrap: rawEnvelope.unwrap,
  toFailure: (parsed, response) =>
    ApiFailureFactory.create(
      'http',
      response,
      typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)
        ? (parsed as Record<string, unknown>)
        : null,
    ),
};

/** Creates the same-origin marketing endpoint boundary with explicit, non-retried mutations. */
export function createMarketingApi(fetchImplementation: typeof fetch = globalThis.fetch) {
  const client = createApiClient({ envelope: MarketingEnvelope, retry: false, fetch: fetchImplementation });
  const root = '/api/marketing';
  return {
    verifyConnection(id: string) {
      return client.post<ConnectionModel>(`${root}/connections/${encodeURIComponent(id)}/verify`, {
        body: {},
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.connection, value),
      });
    },
    queuePublication(id: string, body: QueuePublicationApiRequest) {
      return client.post<PublicationOperationModel>(`${root}/schedules/${encodeURIComponent(id)}/queue`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.publication, value),
      });
    },
    getPublication(id: string, signal?: AbortSignal) {
      return client.get<PublicationOperationModel | null>(`${root}/schedules/${encodeURIComponent(id)}/publication`, {
        signal,
        decode: (value) =>
          MarketingDecoders.decode((item) => item === null || MarketingDecoders.publication(item), value),
      });
    },
    reconcilePublication(id: string) {
      return client.post<PublicationOperationModel>(`${root}/schedules/${encodeURIComponent(id)}/reconcile`, {
        body: {},
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.publication, value),
      });
    },
    getMetrics(id: string, signal?: AbortSignal) {
      return client.get<MetricCacheModel>(`${root}/schedules/${encodeURIComponent(id)}/metrics`, {
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.metricCache, value),
      });
    },
    refreshMetrics(id: string) {
      return client.post<MetricCacheModel>(`${root}/schedules/${encodeURIComponent(id)}/metrics/refresh`, {
        body: {},
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.metricCache, value),
      });
    },
    getSourceAnalyses(signal?: AbortSignal) {
      return client.get<SourceAnalysisModel[]>(`${root}/source-analysis`, {
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.sourceAnalyses, value),
      });
    },
    analyzeSources(body: SourceAnalysisCreateApiRequest) {
      return client.post<SourceAnalysisModel>(`${root}/source-analysis`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.sourceAnalysis, value),
      });
    },
    reviewSourceAnalysis(id: string, expectedRevision: number, text: string) {
      return client.post<SourceAnalysisModel>(`${root}/source-analysis/${encodeURIComponent(id)}/review`, {
        body: { expectedRevision, text },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.sourceAnalysis, value),
      });
    },
    getWorkspace(signal?: AbortSignal) {
      return client.get<WorkspaceModel>(`${root}/workspace`, {
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.workspace, value),
      });
    },
    saveDraft(body: DraftCreateUpdateApiRequest, id?: string) {
      const path = id ? `${root}/drafts/${encodeURIComponent(id)}` : `${root}/drafts`;
      return (id ? client.put : client.post)<DraftModel>(path, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.draft, value),
      });
    },
    approveDraft(id: string, expectedRevision: number) {
      return client.post<DraftModel>(`${root}/drafts/${encodeURIComponent(id)}/approve`, {
        body: { expectedRevision },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.draft, value),
      });
    },
    scheduleDraft(id: string, expectedRevision: number, channel: string, scheduledAt: string) {
      return client.post<ScheduleModel>(`${root}/drafts/${encodeURIComponent(id)}/schedule`, {
        body: { expectedRevision, channel, scheduledAt },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.schedule, value),
      });
    },
    cancelSchedule(id: string) {
      return client.post<ScheduleModel>(`${root}/schedules/${encodeURIComponent(id)}/cancel`, {
        body: {},
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.schedule, value),
      });
    },
    exportDraft(id: string) {
      return client.get(`${root}/drafts/${encodeURIComponent(id)}/export`, { response: 'blob' });
    },
    exportAtlas(assetIds: string[]) {
      return client.post(`${root}/atlas/export`, { body: { assetIds }, response: 'blob' });
    },
    previewCard(body: CardCreateApiRequest, signal?: AbortSignal) {
      return client.post<CardPreviewModel>(`${root}/cards/preview`, {
        body,
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.preview, value),
      });
    },
    renderCard(body: CardCreateApiRequest) {
      return client.post<GenerationJobModel>(`${root}/cards/render`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.job, value),
      });
    },
    scan(text: string) {
      return client.post<ScanModel>(`${root}/scan`, {
        body: { text },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.scan, value),
      });
    },
    assist(prompt: string, operation: string, locale: string) {
      return client.post<TextSuggestionModel>(`${root}/assist/text`, {
        body: { prompt, operation, locale },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.text, value),
      });
    },
    generate(body: GenerationCreateApiRequest) {
      return client.post<GenerationJobModel>(`${root}/generation-jobs`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.job, value),
      });
    },
    syncCatalogue() {
      return client.post<CatalogueSyncReportModel>(`${root}/catalogue/sync`, {
        body: {},
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.syncReport, value),
      });
    },
    getAutomation(signal?: AbortSignal) {
      return client.get<AutomationSettingsModel>(`${root}/automation`, {
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.automation, value),
      });
    },
    saveAutomation(body: AutomationSettingsModel) {
      return client.put<AutomationSettingsModel>(`${root}/automation`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.automation, value),
      });
    },
    getAutomationStatus(signal?: AbortSignal) {
      return client.get<AutomationRunModel>(`${root}/automation/status`, {
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.automationRun, value),
      });
    },
    fillSlots(days: number) {
      return client.post<SlotFillReportModel>(`${root}/automation/fill`, {
        body: { days },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.fillReport, value),
      });
    },
    createDrafts(body: {
      productIds: string[];
      locale: string;
      presetId: string;
      channels: string[];
      template: string | null;
    }) {
      return client.post<DraftModel[]>(`${root}/automation/drafts`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.drafts, value),
      });
    },
    renderCaption(productId: string, locale: string) {
      return client.post<{ caption: string }>(`${root}/automation/caption`, {
        body: { productId, locale },
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.caption, value),
      });
    },
    getBlogStories(signal?: AbortSignal) {
      return client.get<BlogStoryModel[]>(`${root}/blog/stories`, {
        signal,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.stories, value),
      });
    },
    draftFromStory(body: { link: string; productId: string; channels: string[]; locale: string; presetId: string }) {
      return client.post<DraftModel>(`${root}/blog/stories/draft`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.draft, value),
      });
    },
    upload(file: File, provenance: string, role: string) {
      const body = new FormData();
      body.append('file', file);
      body.append('provenance', provenance);
      body.append('role', role);
      return client.post<AssetModel>(`${root}/assets`, {
        body,
        decode: (value) => MarketingDecoders.decode(MarketingDecoders.asset, value),
      });
    },
  } as const;
}

/** The live same-origin endpoint edge. */
export const marketingApi = createMarketingApi();
