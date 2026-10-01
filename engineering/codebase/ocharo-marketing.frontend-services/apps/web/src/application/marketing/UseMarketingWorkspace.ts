import { computed, inject, ref, shallowRef, type InjectionKey } from 'vue';

import type { ApiFailure } from '@wow-two-beta/ui-vue/foundation/http';
import { type Result } from '@wow-two-beta/ui-vue/foundation/results';

import type {
  ChannelStrategyModel,
  EngagementInput,
  AutomationSettingsModel,
  CardCompositionModel,
  WorkspaceModel,
} from '../../domain/marketing';
import {
  marketingApi,
  type DraftCreateUpdateApiRequest,
  type GenerationCreateApiRequest,
  type QueuePublicationApiRequest,
  type SourceAnalysisCreateApiRequest,
} from '../../integration/marketing';

/** Creates the explicit workspace state and safe mutation coordinator. */
export function createMarketingWorkspace(navigate: (path: string) => void) {
  const reviewedCopy = shallowRef<{ analysisId: string; revision: number; text: string } | null>(null);
  const workspace = shallowRef<WorkspaceModel | null>(null);
  const loading = ref(true);
  const error = ref<string | null>(null);
  const notice = ref<string | null>(null);
  const busy = ref<string | null>(null);
  const pendingJobs = computed(
    () => workspace.value?.generationJobs.some((job) => ['queued', 'running'].includes(job.status)) ?? false,
  );
  let loadRevision = 0;

  /** Resolves an actionable message without exposing unexpected server diagnostics. */
  function failureMessage(failure: ApiFailure): string {
    const detail = failure.problem?.detail;
    if (typeof detail === 'string' && detail.length <= 600) return detail;
    if (failure.status === 409)
      return 'Amal mavjud holat bilan mos kelmadi. Ma’lumotlar saqlanib turibdi; holatni tekshiring.';
    if (failure.code === 'protocol') return "Server javobi kutilgan formatda emas. Qayta urinib ko'ring.";
    if (failure.code === 'transport') return "Serverga ulanib bo'lmadi. Mahalliy xizmat ishlayotganini tekshiring.";
    return failure.status ? `Amal bajarilmadi (${failure.status}). Qayta urinib ko'ring.` : 'Amal bajarilmadi.';
  }

  /** Refreshes recorded state without inventing a fallback workspace. */
  async function refresh(quiet = false, signal?: AbortSignal) {
    const revision = ++loadRevision;
    if (!quiet) loading.value = true;
    const result = await marketingApi.getWorkspace(signal);
    if (revision !== loadRevision || signal?.aborted) return result;
    if (result.ok) {
      workspace.value = result.value;
      if (!quiet) error.value = null;
    } else if (result.failure.code !== 'cancelled') error.value = failureMessage(result.failure);
    loading.value = false;
    return result;
  }

  /** Reports command outcomes and reloads the persisted workspace on success. */
  async function execute<T>(label: string, operation: () => Promise<Result<T, ApiFailure>>) {
    busy.value = label;
    error.value = null;
    notice.value = null;
    try {
      const result = await operation();
      if (result.ok) await refresh(true);
      else error.value = failureMessage(result.failure);
      return result;
    } finally {
      busy.value = null;
    }
  }

  return {
    workspace,
    reviewedCopy,
    loading,
    error,
    notice,
    busy,
    pendingJobs,
    navigate,
    refresh,
    failureMessage,
    getOperations: (signal?: AbortSignal) => marketingApi.getOperations(signal),
    getChannelStrategies: (signal?: AbortSignal) => marketingApi.getChannelStrategies(signal),
    saveChannelStrategy: (body: ChannelStrategyModel) =>
      execute('channel', () => marketingApi.saveChannelStrategy(body)),
    recordEngagement: (body: EngagementInput) => execute('engagement', () => marketingApi.recordEngagement(body)),
    verifyConnection: (id: string) => execute('verify', () => marketingApi.verifyConnection(id)),
    queuePublication: (id: string, body: QueuePublicationApiRequest) =>
      execute('queue', () => marketingApi.queuePublication(id, body)),
    reconcilePublication: (id: string) => execute('reconcile', () => marketingApi.reconcilePublication(id)),
    getMetrics: (id: string, signal?: AbortSignal) => marketingApi.getMetrics(id, signal),
    refreshMetrics: (id: string) => execute('metrics', () => marketingApi.refreshMetrics(id)),
    getSourceAnalyses: (signal?: AbortSignal) => marketingApi.getSourceAnalyses(signal),
    analyzeSources: (body: SourceAnalysisCreateApiRequest) =>
      execute('analysis', () => marketingApi.analyzeSources(body)),
    reviewSourceAnalysis: (id: string, revision: number, text: string) =>
      execute('review', () => marketingApi.reviewSourceAnalysis(id, revision, text)),
    saveDraft: (body: DraftCreateUpdateApiRequest, id?: string) =>
      execute('save', () => marketingApi.saveDraft(body, id)),
    approveDraft: (id: string, revision: number) => execute('approve', () => marketingApi.approveDraft(id, revision)),
    scheduleDraft: (id: string, revision: number, channel: string, time: string) =>
      execute('schedule', () => marketingApi.scheduleDraft(id, revision, channel, time)),
    cancelSchedule: (id: string) => execute('cancel', () => marketingApi.cancelSchedule(id)),
    exportDraft: (id: string) => execute('export', () => marketingApi.exportDraft(id)),
    exportAtlas: (ids: string[]) => execute('atlas', () => marketingApi.exportAtlas(ids)),
    previewCard: (body: CardCompositionModel, signal?: AbortSignal) => marketingApi.previewCard(body, signal),
    renderCard: (body: CardCompositionModel) => execute('render', () => marketingApi.renderCard(body)),
    scan: (text: string) => execute('scan', () => marketingApi.scan(text)),
    assist: (prompt: string, operation: string, locale: string) =>
      execute('assist', () => marketingApi.assist(prompt, operation, locale)),
    generate: (body: GenerationCreateApiRequest) => execute('generate', () => marketingApi.generate(body)),
    upload: (file: File, provenance: string, role: string) =>
      execute('upload', () => marketingApi.upload(file, provenance, role)),
    syncCatalogue: () => execute('catalogue', () => marketingApi.syncCatalogue()),
    getAutomation: (signal?: AbortSignal) => marketingApi.getAutomation(signal),
    saveAutomation: (body: AutomationSettingsModel) => execute('automation', () => marketingApi.saveAutomation(body)),
    getAutomationStatus: (signal?: AbortSignal) => marketingApi.getAutomationStatus(signal),
    fillSlots: (days: number) => execute('fill', () => marketingApi.fillSlots(days)),
    createDrafts: (body: {
      productIds: string[];
      locale: string;
      presetId: string;
      channels: string[];
      template: string | null;
    }) => execute('drafts', () => marketingApi.createDrafts(body)),
    renderCaption: (productId: string, locale: string) => marketingApi.renderCaption(productId, locale),
    getBlogStories: (signal?: AbortSignal) => marketingApi.getBlogStories(signal),
    draftFromStory: (body: { link: string; productId: string; channels: string[]; locale: string; presetId: string }) =>
      execute('story', () => marketingApi.draftFromStory(body)),
  };
}

/** The shared workspace orchestration contract. */
export type MarketingWorkspace = ReturnType<typeof createMarketingWorkspace>;
/** The explicit application injection key. */
export const MarketingWorkspaceKey: InjectionKey<MarketingWorkspace> = Symbol('MarketingWorkspace');

/** Reads the workspace wired by the application bootstrap. */
export function useMarketingWorkspace(): MarketingWorkspace {
  const workspace = inject(MarketingWorkspaceKey);
  if (!workspace) throw new Error('Marketing workspace is not configured.');
  return workspace;
}
