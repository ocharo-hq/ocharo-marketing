import { computed, ref } from 'vue';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/ui-vue/foundation/http';
import { MarketingWorkspaceKey, type MarketingWorkspace } from '../src/application/marketing';
import type { ScheduleModel } from '../src/domain/marketing';
import ObservedMetricsPanel from '../src/presentation/marketing/ObservedMetricsPanel.vue';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
const getMetrics = vi.fn(),
  refreshMetrics = vi.fn();
let wrapper: VueWrapper;
beforeEach(() => {
  getMetrics.mockReset();
  refreshMetrics.mockReset();
  useUiLocale().setUiLocale('en');
});
afterEach(() => wrapper?.unmount());
function render() {
  const desk = {
    workspace: ref(null),
    busy: ref(null),
    getMetrics,
    refreshMetrics,
    failureMessage: () => 'Could not complete request',
    pendingJobs: computed(() => false),
  } as unknown as MarketingWorkspace;
  wrapper = mount(ObservedMetricsPanel, {
    props: {
      plan: {
        id: 'plan',
        channel: 'x',
        status: 'published',
        scheduledAt: '2026-10-01T00:00:00Z',
        draftRevision: 1,
        draftTitle: 'Confirmed post',
      } as ScheduleModel,
    },
    global: { provide: { [MarketingWorkspaceKey as symbol]: desk } },
  });
  return wrapper;
}
describe('saved metric reads', () => {
  it('does not represent a failed first GET as successful empty observations', async () => {
    getMetrics.mockResolvedValue(ResultExtensions.fail(ApiFailureFactory.create('transport')));
    render();
    await flushPromises();
    expect(wrapper.text()).not.toContain('No saved observations.');
    expect(wrapper.text()).toContain('Content unavailable');
    expect(wrapper.findAll('button').find((button) => button.text() === 'Refresh')).toBeTruthy();
  });
  it('retries only saved observations through the persistent Refresh control', async () => {
    getMetrics
      .mockResolvedValueOnce(ResultExtensions.fail(ApiFailureFactory.create('transport')))
      .mockResolvedValueOnce(ResultExtensions.ok({ observations: [], history: [] }));
    render();
    await flushPromises();
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Refresh')!
      .trigger('click');
    await flushPromises();
    expect(getMetrics).toHaveBeenCalledTimes(2);
    expect(refreshMetrics).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('No saved observations.');
    expect(wrapper.get('.query-label').classes()).not.toContain('border');
  });
});
