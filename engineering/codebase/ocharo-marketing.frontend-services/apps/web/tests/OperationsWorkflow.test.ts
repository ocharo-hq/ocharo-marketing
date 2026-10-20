import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { beforeEach as beforeLocaleTest } from 'vitest';
beforeLocaleTest(() => {
  useUiLocale().setUiLocale('uz');
  vi.spyOn(feedbackBus, 'notify');
});
import { afterEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { shallowRef, ref } from 'vue';
import { createAppRouter, RouterHistoryMode } from '@wow-two-beta/fe/router';
import { ResultExtensions } from '@wow-two-beta/fe/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/fe/foundation/http';
import { MarketingWorkspaceKey, type MarketingWorkspace } from '../src/application/marketing';
import type {
  ChannelStrategyModel,
  EngagementInput,
  EngagementObservationModel,
  OperationsOverviewModel,
} from '../src/domain/marketing';
import { OperationsExtensions } from '../src/domain/marketing/OperationsExtensions';
import { MarketingDecoders, createMarketingApi } from '../src/integration/marketing';
import ChannelsPage from '../src/presentation/marketing/ChannelsPage.vue';
import ManualEngagementPanel from '../src/presentation/marketing/ManualEngagementPanel.vue';
import PostMediaPicker from '../src/presentation/marketing/PostMediaPicker.vue';
const wrappers: ReturnType<typeof mount>[] = [];
afterEach(() => {
  wrappers.splice(0).forEach((w) => w.unmount());
  vi.useRealTimers();
});
const strategy: ChannelStrategyModel = {
  channel: 'instagram',
  revision: 0,
  active: false,
  audience: '',
  purpose: '',
  formats: '',
  pillars: '',
  owner: '',
  profileUrl: '',
  weeklyTarget: 0,
  reviewEveryDays: 7,
  updatedAt: null,
};
function overview(): OperationsOverviewModel {
  return {
    observedAt: '2026-09-29T12:00:00Z',
    weekStartsAt: '2026-09-27T19:00:00Z',
    weekEndsAt: '2026-10-04T19:00:00Z',
    timeZone: 'Asia/Tashkent',
    channels: [
      {
        strategy,
        weeklySlots: 0,
        upcomingPlans: 0,
        confirmedReceiptsThisWeek: 0,
        manualPostsThisWeek: 0,
        duePlans: 0,
        lastObservationAt: null,
        warnings: [],
      },
    ],
    workers: [],
    automation: {
      lastSyncAt: null,
      lastSync: null,
      lastFillAt: null,
      lastFillPlanned: 0,
      lastError: null,
      lastErrorAt: null,
    },
    delivery: { publishingEnabled: false, metricReadsEnabled: false, accountVerificationEnabled: false },
    approvedUnplanned: 0,
    queued: 0,
    running: 0,
    failed: 0,
    unknown: 0,
    held: 0,
    attentionPlans: [],
    attentionDeliveries: [],
    observations: [],
    providerObservations: [],
    observationCount: 0,
  };
}
async function render(component: typeof ChannelsPage | typeof ManualEngagementPanel) {
  const desk = {
    workspace: shallowRef({ schedules: [] }),
    busy: ref(null),
    error: ref(null),
    notice: ref(null),
    navigate: vi.fn(),
    failureMessage: () => 'Reload before saving; your input is preserved.',
    getOperations: vi.fn(async () => ResultExtensions.ok(overview())),
    saveChannelStrategy: vi.fn<MarketingWorkspace['saveChannelStrategy']>(async (input: ChannelStrategyModel) =>
      ResultExtensions.ok({ ...input, revision: 1 }),
    ),
    recordEngagement: vi.fn<MarketingWorkspace['recordEngagement']>(async (input: EngagementInput) =>
      ResultExtensions.ok({
        observation: input,
        recordedAt: new Date().toISOString(),
        recordedBy: 'test',
        source: 'manual',
        engagementRate: null,
      }),
    ),
  };
  const router = createAppRouter(
    [
      { path: '/', component },
      { path: '/elsewhere', component: { template: '<p>Elsewhere</p>' } },
    ],
    { history: RouterHistoryMode.Memory },
  );
  await router.push('/');
  await router.isReady();
  const wrapper = mount(component, {
    global: {
      plugins: [router],
      provide: { [MarketingWorkspaceKey as symbol]: desk as unknown as MarketingWorkspace },
    },
  });
  wrappers.push(wrapper);
  await flushPromises();
  return { wrapper, desk, router };
}
function evidence(id: string, observedAt: string, reach: number | null): EngagementObservationModel {
  return {
    observation: {
      id,
      channel: 'instagram',
      postUrl: 'https://example.test/post',
      title: 'Post',
      scheduleId: null,
      publishedAt: '2026-09-27T01:00:00Z',
      observedAt,
      views: null,
      reach,
      likes: 0,
      comments: null,
      shares: null,
      saves: null,
      clicks: null,
      enquiries: null,
      orders: null,
      evidenceNote: 'Manual',
    },
    recordedAt: observedAt,
    recordedBy: 'reviewer',
    source: 'manual',
    engagementRate: null,
  };
}

describe('channel operations', () => {
  it('retains channel edits after a stale revision and keeps navigation guarded', async () => {
    const { wrapper, desk, router } = await render(ChannelsPage);
    desk.saveChannelStrategy.mockImplementationOnce(async () =>
      ResultExtensions.fail(ApiFailureFactory.create('http', new Response(null, { status: 409 }))),
    );
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'Rejani tahrirlash')!
      .trigger('click');
    await wrapper.findAll('textarea')[0]!.setValue('International recognition');
    await wrapper.get('form').trigger('submit');
    await flushPromises();
    expect((wrapper.findAll('textarea')[0]!.element as HTMLTextAreaElement).value).toBe('International recognition');
    expect(wrapper.text()).toContain('Saqlanmagan o‘zgarishlar');
    await router.push('/elsewhere');
    await flushPromises();
    expect(router.currentRoute.value.path).toBe('/');
    expect(desk.saveChannelStrategy).toHaveBeenCalledOnce();
  });
  it('records missing values as null and observed zero as zero, reusing the request ID after failure', async () => {
    const { wrapper, desk } = await render(ManualEngagementPanel);
    desk.recordEngagement.mockImplementationOnce(async () =>
      ResultExtensions.fail(ApiFailureFactory.create('transport')),
    );
    await wrapper
      .findAll('button')
      .find((b) => b.text() === 'Kuzatuv qo‘shish')!
      .trigger('click');
    await wrapper.get('input[maxlength="160"]').setValue('Manual post');
    await wrapper.get('input[type="url"]').setValue('https://example.test/post');
    await wrapper.findAll('input[type="number"]')[0]!.setValue('0');
    await wrapper.get('textarea').setValue('Insights captured manually');
    await wrapper.get('form').trigger('submit');
    await flushPromises();
    expect(wrapper.text()).not.toContain('input is preserved');
    expect(feedbackBus.notify).toHaveBeenCalledWith(
      expect.objectContaining({ description: expect.stringContaining('input is preserved') }),
    );
    await wrapper.get('form').trigger('submit');
    await flushPromises();
    const first = desk.recordEngagement.mock.calls[0]![0];
    const second = desk.recordEngagement.mock.calls[1]![0];
    expect(first.id).toBe(second.id);
    expect(first.views).toBe(0);
    expect(first.reach).toBeNull();
    expect(first.observedAt.endsWith('Z')).toBe(true);
    expect(wrapper.find('form').exists()).toBe(false);
  });
  it('chooses the latest observation per post without summing cumulative counts', () => {
    const first = evidence('a', '2026-09-28T10:00:00Z', 100);
    const last = evidence('b', '2026-09-29T10:00:00Z', 150);
    expect(OperationsExtensions.latest([first, last])).toEqual([last]);
    expect(OperationsExtensions.previous(last, [first, last])).toEqual(first);
    expect(
      OperationsExtensions.latest([first, { ...last, observation: { ...last.observation, channel: 'telegram' } }]),
    ).toHaveLength(2);
  });
  it('rejects malformed operations evidence instead of rendering invented defaults', () => {
    expect(MarketingDecoders.operations(overview())).toBe(true);
    expect(MarketingDecoders.operations({ ...overview(), observationCount: undefined })).toBe(false);
    expect(
      MarketingDecoders.engagementObservation({ ...evidence('a', '2026-09-29T10:00:00Z', null), source: 'provider' }),
    ).toBe(false);
  });
  it('never retries a manual evidence mutation automatically', async () => {
    const fetcher = vi.fn<typeof fetch>(async () => Response.json({ detail: 'Conflict' }, { status: 409 }));
    await createMarketingApi(fetcher).recordEngagement(evidence('a', '2026-09-29T10:00:00Z', 10).observation);
    expect(fetcher).toHaveBeenCalledOnce();
  });
  it('selects real image identifiers in explicit order and bounds the selection', async () => {
    const asset = {
      id: 'photo',
      fileName: 'source.png',
      contentType: 'image/png',
      byteLength: 10,
      sha256: 'test',
      provenance: 'Source',
      url: '/source.png',
      role: 'front',
    };
    const wrapper = mount(PostMediaPicker, { props: { assets: [asset], modelValue: [], productAssetIds: ['photo'] } });
    wrappers.push(wrapper);
    await wrapper.get('button[aria-label="source.png"]').trigger('click');
    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([['photo']]);
    await wrapper.setProps({ modelValue: Array.from({ length: 10 }, (_, i) => `selected-${i}`) });
    expect(wrapper.get('button').attributes('disabled')).toBeDefined();
  });
});
