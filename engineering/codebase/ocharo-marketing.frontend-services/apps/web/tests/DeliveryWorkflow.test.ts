import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { beforeEach as beforeLocaleTest } from 'vitest';
beforeLocaleTest(() => useUiLocale().setUiLocale('uz'));
import { mount, flushPromises } from '@vue/test-utils';
import { ref, shallowRef } from 'vue';
import { describe, it, expect, vi, afterEach } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/ui-vue/foundation/http';
import { MarketingWorkspaceKey, type MarketingWorkspace } from '../src/application/marketing';
import {
  DeliveryExtensions,
  type WorkspaceModel,
  type PublicationOperationModel,
  type MetricCacheModel,
} from '../src/domain/marketing';
import DeliveryPanel from '../src/presentation/marketing/DeliveryPanel.vue';
import ObservedMetricsPanel from '../src/presentation/marketing/ObservedMetricsPanel.vue';
import SourceImageAnalysisPanel from '../src/presentation/marketing/SourceImageAnalysisPanel.vue';

const mounted: ReturnType<typeof mount>[] = [];
afterEach(() => {
  mounted.splice(0).forEach((item) => item.unmount());
  vi.useRealTimers();
});
/** Synthetic enabled state exists only in tests and never contacts a provider. */
function fixture(): WorkspaceModel {
  return {
    brand: { name: 'Ocharo', timezone: 'Asia/Tashkent', handle: null },
    products: [],
    presets: [],
    metrics: [],
    generationJobs: [],
    assets: [
      {
        id: 'front',
        fileName: 'front.png',
        contentType: 'image/png',
        byteLength: 100,
        sha256: 'immutable-hash',
        provenance: 'Test source',
        url: '/front.png',
        role: 'front',
      },
    ],
    drafts: [
      {
        id: 'draft',
        title: 'Nimcha',
        productId: 'p',
        presetId: 't02-bone',
        locale: 'uz',
        caption: 'Reviewed exact caption',
        channels: ['telegram'],
        mediaIds: ['front'],
        revision: 2,
        approvedRevision: 2,
        status: 'approved',
        createdAt: '2026-09-01T00:00:00Z',
        updatedAt: '2026-09-01T00:00:00Z',
      },
    ],
    schedules: [
      {
        id: 'plan',
        draftId: 'draft',
        draftRevision: 2,
        draftTitle: 'Nimcha',
        channel: 'telegram',
        scheduledAt: '2099-10-01T07:00:00Z',
        status: 'planned',
        message: null,
        externalId: null,
      },
    ],
    connections: [
      {
        id: 'telegram',
        name: 'Telegram',
        kind: 'social',
        status: 'configured',
        capabilities: ['text', 'photo'],
        setupHint: 'Test',
        documentationUrl: 'https://core.telegram.org/bots/api',
        connectionRevision: 3,
        verificationStatus: 'verified',
        executionEnabled: true,
        metricReadsEnabled: true,
        accountId: 'bot123',
        destinationId: '-100123',
        displayName: 'Ocharo test channel',
        verifiedAt: '2026-09-01T00:00:00Z',
        safeMessage: 'Test',
        canVerify: true,
      },
    ],
    deliverySettings: { publishingEnabled: true, metricReadsEnabled: true, accountVerificationEnabled: true },
    publicationOperations: [],
  };
}
/** Captures an immutable delivery record for fake UI tests. */
function operation(source: WorkspaceModel): PublicationOperationModel {
  const draft = source.drafts[0]!;
  return {
    id: 'operation',
    scheduleId: 'plan',
    provider: 'telegram',
    connectionId: 'telegram',
    connectionRevision: 3,
    status: 'queued',
    phase: 'authorized',
    dueAt: source.schedules[0]!.scheduledAt,
    authorizedAt: '2026-09-01T00:00:00Z',
    dispatchStartedAt: null,
    updatedAt: '2026-09-01T00:00:00Z',
    message: null,
    receipt: null,
    canCancel: true,
    canReconcile: false,
    reconciliationMessage: 'No provider read',
    snapshot: {
      draft: structuredClone(draft),
      identity: {
        connectionId: 'telegram',
        provider: 'telegram',
        revision: 3,
        accountId: 'bot123',
        destinationId: '-100123',
        displayName: 'Ocharo test channel',
        capabilities: ['text', 'photo'],
        verifiedAt: '2026-09-01T00:00:00Z',
      },
      media: [{ assetId: 'front', sha256: 'immutable-hash', contentType: 'image/png', fileName: 'front.png' }],
    },
  };
}
/** Explicit ports let tests prove commands without network or paid calls. */
function desk(source = fixture()) {
  return {
    workspace: shallowRef(source),
    busy: ref(null),
    error: ref<string | null>(null),
    notice: ref<string | null>(null),
    reviewedCopy: ref(null),
    navigate: vi.fn(),
    refresh: vi.fn(),
    failureMessage: () => 'Safe failure',
    queuePublication: vi.fn(async () => ResultExtensions.ok(operation(source))),
    cancelSchedule: vi.fn(async () => ResultExtensions.ok({ ...source.schedules[0]!, status: 'cancelled' })),
    getMetrics: vi.fn(async () => ResultExtensions.ok({ scheduleId: 'plan', observations: [], lastAttempt: null })),
    refreshMetrics: vi.fn(),
    getSourceAnalyses: vi.fn(async () => ResultExtensions.ok([])),
    analyzeSources: vi.fn(),
    reviewSourceAnalysis: vi.fn(),
  } as unknown as MarketingWorkspace;
}
const overlays = {
  Modal: { props: ['open'], template: '<div v-if="open"><slot /></div>' },
  ModalContent: { template: '<div><slot /></div>' },
  ModalHeader: { template: '<div><slot /></div>' },
  ModalTitle: { template: '<h2><slot /></h2>' },
  ModalDescription: { template: '<p><slot /></p>' },
  ModalFooter: { template: '<div><slot /></div>' },
  OverlayTitle: { template: '<h2><slot /></h2>' },
  OverlayDescription: { template: '<p><slot /></p>' },
  OverlayHeader: { template: '<div><slot /></div>' },
  OverlayFooter: { template: '<div><slot /></div>' },
};
function render(
  component: Parameters<typeof mount>[0],
  state: MarketingWorkspace,
  props: Record<string, unknown> = {},
) {
  const wrapper = mount(component, {
    props,
    global: { provide: { [MarketingWorkspaceKey as symbol]: state }, stubs: overlays },
  });
  mounted.push(wrapper);
  return wrapper;
}
function button(wrapper: ReturnType<typeof mount>, label: string) {
  return wrapper.findAll('button').find((item) => item.text().includes(label))!;
}

describe('operator delivery confirmation', () => {
  it('shows exact snapshot and requires acknowledgement before one queue command', async () => {
    const state = desk();
    const wrapper = render(DeliveryPanel, state, { plan: state.workspace.value!.schedules[0] });
    await button(wrapper, 'Yuborishni ko‘rib chiqish').trigger('click');
    expect(wrapper.text()).toContain('Reviewed exact caption');
    expect(wrapper.text()).toContain('-100123');
    expect(button(wrapper, 'Yuborish navbatiga').attributes('disabled')).toBeDefined();
    await wrapper.find('input[type="checkbox"]').setValue(true);
    await button(wrapper, 'Yuborish navbatiga').trigger('click');
    await flushPromises();
    expect(state.queuePublication).toHaveBeenCalledTimes(1);
    const request = vi.mocked(state.queuePublication).mock.calls[0]![1];
    expect(request).toMatchObject({
      expectedDraftRevision: 2,
      expectedConnectionRevision: 3,
      connectionId: 'telegram',
    });
    expect(request.operationId).toMatch(/^[a-f0-9-]{36}$/i);
  });
  it('invalidates consent when destination revision changes and keeps reviewed evidence visible', async () => {
    const state = desk();
    const wrapper = render(DeliveryPanel, state, { plan: state.workspace.value!.schedules[0] });
    await button(wrapper, 'Yuborishni ko‘rib chiqish').trigger('click');
    await wrapper.find('input[type="checkbox"]').setValue(true);
    const source = state.workspace.value!;
    state.workspace.value = {
      ...source,
      connections: [{ ...source.connections[0]!, connectionRevision: 4, destinationId: '-100999' }],
    };
    await flushPromises();
    expect(wrapper.text()).toContain('Post, manzil yoki reja o‘zgardi');
    expect(wrapper.text()).toContain('-100123');
    expect(state.queuePublication).not.toHaveBeenCalled();
    expect(button(wrapper, 'Qayta ko‘rib chiqish')).toBeDefined();
  });
  it('reports an expired plan at submit and never queues it', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2099-10-01T06:59:00Z'));
    const state = desk();
    const wrapper = render(DeliveryPanel, state, { plan: state.workspace.value!.schedules[0] });
    await button(wrapper, 'Yuborishni ko‘rib chiqish').trigger('click');
    await wrapper.find('input[type="checkbox"]').setValue(true);
    vi.setSystemTime(new Date('2099-10-01T07:01:00Z'));
    await button(wrapper, 'Yuborish navbatiga').trigger('click');
    expect(state.queuePublication).not.toHaveBeenCalled();
    expect(state.error.value).toContain('kelajakda');
  });
  it('keeps frozen title/caption after a newer draft exists and exposes safe cancellation', async () => {
    const source = fixture();
    source.publicationOperations = [operation(source)];
    source.drafts = [{ ...source.drafts[0]!, title: 'New title', caption: 'New caption', revision: 3 }];
    const state = desk(source);
    const wrapper = render(DeliveryPanel, state, { plan: source.schedules[0] });
    expect(wrapper.text()).toContain('Nimcha');
    expect(wrapper.text()).toContain('Reviewed exact caption');
    expect(wrapper.text()).not.toContain('New caption');
    await button(wrapper, 'Navbatni bekor qilish').trigger('click');
    expect(state.cancelSchedule).toHaveBeenCalledWith('plan');
  });
  it('blocks multiple photos and overlong captions without implying truncation', () => {
    const source = fixture();
    source.drafts[0]!.mediaIds = ['front', 'front'];
    source.drafts[0]!.caption = 'x'.repeat(1025);
    const reasons = DeliveryExtensions.blockers(source, source.schedules[0]!);
    expect(reasons.join(' ')).toContain('bitta tasvir');
    expect(reasons.join(' ')).toContain('1024');
  });
});

describe('saved metric evidence', () => {
  it('reads only cache on mount and preserves observed zero after an explicit failed provider read', async () => {
    const source = fixture();
    source.schedules[0]!.status = 'published';
    source.schedules[0]!.channel = 'x';
    source.connections[0]!.id = 'x';
    source.connections[0]!.capabilities = ['text', 'photo', 'public-metrics'];
    const op = operation(source);
    op.status = 'published';
    op.provider = 'x';
    op.connectionId = 'x';
    op.snapshot.identity.provider = 'x';
    op.snapshot.identity.connectionId = 'x';
    op.receipt = { provider: 'telegram', stage: 'published', externalId: '42', relatedIds: { chat_id: '-100123' } };
    source.publicationOperations = [op];
    const state = desk(source);
    const cache: MetricCacheModel = {
      scheduleId: 'plan',
      observations: [
        {
          id: 'observation',
          scheduleId: 'plan',
          connectionId: 'telegram',
          provider: 'telegram',
          receiptId: '42',
          name: 'views',
          value: 0,
          availability: 'observed',
          observedAt: '2026-09-01T00:00:00Z',
          reason: null,
          scope: 'post',
          aggregation: 'cumulative',
        },
      ],
      lastAttempt: null,
    };
    vi.mocked(state.getMetrics).mockResolvedValue(ResultExtensions.ok(cache));
    vi.mocked(state.refreshMetrics).mockResolvedValue(ResultExtensions.fail(ApiFailureFactory.create('transport')));
    const wrapper = render(ObservedMetricsPanel, state, { plan: source.schedules[0] });
    await flushPromises();
    expect(state.getMetrics).toHaveBeenCalledTimes(1);
    expect(state.refreshMetrics).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Kuzatildi:');
    await button(wrapper, 'Kanaldan yangilash').trigger('click');
    await flushPromises();
    expect(state.refreshMetrics).toHaveBeenCalledTimes(1);
    expect(wrapper.text()).toContain('Safe failure');
    expect(wrapper.text()).toContain('Ko‘rishlar: 0');
  });
  it('disables provider reads when metrics gate is off while showing old cache', async () => {
    const state = desk();
    state.workspace.value!.deliverySettings!.metricReadsEnabled = false;
    const wrapper = render(ObservedMetricsPanel, state, { plan: state.workspace.value!.schedules[0] });
    await flushPromises();
    expect(button(wrapper, 'Kanaldan yangilash').attributes('disabled')).toBeDefined();
    expect(state.refreshMetrics).not.toHaveBeenCalled();
  });
});

describe('source-linked wording review', () => {
  it('never calls analysis as a readiness probe when the provider is unconfigured', async () => {
    const state = desk();
    const wrapper = render(SourceImageAnalysisPanel, state);
    await flushPromises();
    await wrapper.find('input[type=checkbox]').setValue(true);
    expect(button(wrapper, 'Tasvirlarni tahlil qilish').attributes('disabled')).toBeDefined();
    await button(wrapper, 'Xizmat holatini yangilash').trigger('click');
    expect(state.refresh).toHaveBeenCalledTimes(1);
    expect(state.analyzeSources).not.toHaveBeenCalled();
  });
  const analysis = {
    id: 'analysis',
    revision: 1,
    status: 'ready' as const,
    provider: 'openai',
    model: 'test-model',
    locale: 'uz',
    prompt: '',
    sources: [{ assetId: 'front', sha256: 'immutable-hash', fileName: 'front.png', provenance: 'Test source' }],
    suggestedText: 'Original source suggestion',
    reviewedText: null,
    message: 'Review suggested copy',
    createdAt: '2026-09-01T00:00:00Z',
    updatedAt: '2026-09-01T00:00:00Z',
    reviewedAt: null,
  };
  it('loads cached suggestions without generation and never copies unreviewed wording', async () => {
    const state = desk();
    vi.mocked(state.getSourceAnalyses).mockResolvedValue(ResultExtensions.ok([analysis]));
    const wrapper = render(SourceImageAnalysisPanel, state);
    await flushPromises();
    expect(state.analyzeSources).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Original source suggestion');
    expect(button(wrapper, 'Postda ishlatish').attributes('disabled')).toBeDefined();
    expect(state.reviewedCopy.value).toBeNull();
  });
  it('preserves text typed after a review submission and requires another save before reuse', async () => {
    const state = desk();
    vi.mocked(state.getSourceAnalyses).mockResolvedValue(ResultExtensions.ok([analysis]));
    let resolve: (value: Awaited<ReturnType<MarketingWorkspace['reviewSourceAnalysis']>>) => void = () => {};
    vi.mocked(state.reviewSourceAnalysis).mockImplementation(
      () =>
        new Promise((done) => {
          resolve = done;
        }),
    );
    const wrapper = render(SourceImageAnalysisPanel, state);
    await flushPromises();
    await wrapper.find('#review-text').setValue('Reviewed A');
    await button(wrapper, 'Tekshirilgan matnni saqlash').trigger('click');
    await wrapper.find('#review-text').setValue('Newer B');
    resolve(
      ResultExtensions.ok({
        ...analysis,
        status: 'reviewed',
        revision: 2,
        reviewedText: 'Reviewed A',
        reviewedAt: '2026-09-01T01:00:00Z',
      }),
    );
    await flushPromises();
    expect((wrapper.find('#review-text').element as HTMLTextAreaElement).value).toBe('Newer B');
    expect(wrapper.text()).toContain('Original source suggestion');
    expect(button(wrapper, 'Postda ishlatish').attributes('disabled')).toBeDefined();
  });
  it('offers only saved reviewed text to a draft without changing catalogue records', async () => {
    const state = desk();
    vi.mocked(state.getSourceAnalyses).mockResolvedValue(
      ResultExtensions.ok([
        {
          ...analysis,
          status: 'reviewed',
          revision: 2,
          reviewedText: 'Reviewed copy',
          reviewedAt: '2026-09-01T01:00:00Z',
        },
      ]),
    );
    const original = structuredClone(state.workspace.value!.products);
    const wrapper = render(SourceImageAnalysisPanel, state);
    await flushPromises();
    await button(wrapper, 'Postda ishlatish').trigger('click');
    expect(state.reviewedCopy.value).toEqual({ analysisId: 'analysis', revision: 2, text: 'Reviewed copy' });
    expect(state.navigate).toHaveBeenCalledWith('/posts/new');
    expect(state.workspace.value!.products).toEqual(original);
  });
  it('revokes review and reuse eligibility if the source hash changes', async () => {
    const state = desk();
    vi.mocked(state.getSourceAnalyses).mockResolvedValue(
      ResultExtensions.ok([{ ...analysis, status: 'reviewed', reviewedText: 'Reviewed copy' }]),
    );
    const wrapper = render(SourceImageAnalysisPanel, state);
    await flushPromises();
    const source = state.workspace.value!;
    state.workspace.value = { ...source, assets: [{ ...source.assets[0]!, sha256: 'changed' }] };
    await flushPromises();
    expect(button(wrapper, 'Postda ishlatish').attributes('disabled')).toBeDefined();
    expect(button(wrapper, 'Tekshirilgan matnni saqlash').attributes('disabled')).toBeDefined();
    expect(wrapper.text()).toContain('Manba izi o‘zgargan');
  });
});
