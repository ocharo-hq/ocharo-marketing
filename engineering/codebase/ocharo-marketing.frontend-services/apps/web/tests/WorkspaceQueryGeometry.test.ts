import { computed, ref, shallowRef } from 'vue';
import { createMemoryHistory, createRouter } from 'vue-router';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/fe/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/fe/foundation/http';
import WorkspaceSkeleton from '../src/presentation/marketing/WorkspaceSkeleton.vue';
import ManualEngagementPanel from '../src/presentation/marketing/ManualEngagementPanel.vue';
import { MarketingWorkspaceKey, type MarketingWorkspace } from '../src/application/marketing';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

const wrappers: VueWrapper[] = [];
beforeEach(() => useUiLocale().setUiLocale('en'));
afterEach(() => wrappers.splice(0).forEach((wrapper) => wrapper.unmount()));
function shape(path: string) {
  const wrapper = mount(WorkspaceSkeleton, { props: { path } });
  wrappers.push(wrapper);
  return wrapper;
}
describe('initial workspace geometry', () => {
  it.each(['/products', '/posts'])('reserves actual gallery photos and responsive columns for %s', (path) => {
    const wrapper = shape(path);
    expect(
      wrapper
        .findAll('div')
        .some((node) => node.classes().includes('sm:grid-cols-2') && node.classes().includes('xl:grid-cols-3')),
    ).toBe(true);
    expect(wrapper.findAll('div').filter((node) => node.classes().includes('aspect-[4/3]'))).toHaveLength(3);
  });
  it('reserves seven calendar days with the existing mobile and desktop geometry', () => {
    const wrapper = shape('/calendar');
    const grid = wrapper.findAll('div').find((node) => node.classes().includes('md:grid-cols-7'))!;
    expect(grid.classes()).toContain('grid-cols-1');
    expect(grid.findAll('div').filter((node) => node.classes().includes('min-h-48'))).toHaveLength(7);
  });
  it('uses an enquiry filter and form cards rather than table rows', () => {
    const wrapper = shape('/inquiries');
    expect(wrapper.findAll('div').filter((node) => node.classes().includes('sm:p-6'))).toHaveLength(3);
    expect(wrapper.findAll('div').some((node) => node.classes().includes('sm:grid-cols-2'))).toBe(true);
  });
});
function manual(getOperations: ReturnType<typeof vi.fn>) {
  const desk = {
    workspace: shallowRef({ schedules: [] }),
    busy: ref(null),
    getOperations,
    recordEngagement: vi.fn(),
    failureMessage: () => 'Request unavailable',
    pendingJobs: computed(() => false),
  } as unknown as MarketingWorkspace;
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/', component: { template: '<div />' } }],
  });
  const wrapper = mount(ManualEngagementPanel, {
    global: { plugins: [router], provide: { [MarketingWorkspaceKey as symbol]: desk } },
  });
  wrappers.push(wrapper);
  return wrapper;
}
it('distinguishes pending and failed manual reads from confirmed empty data', async () => {
  let finish!: (value: unknown) => void;
  const getOperations = vi.fn(
    () =>
      new Promise((resolve) => {
        finish = resolve;
      }),
  );
  const wrapper = manual(getOperations);
  await flushPromises();
  const initialFrame = wrapper.get('section').classes();
  expect(initialFrame).not.toContain('border');
  expect(wrapper.find('.query-region[aria-busy=true]').exists()).toBe(true);
  expect(wrapper.text()).not.toContain('No manual observations');
  finish(ResultExtensions.fail(ApiFailureFactory.create('transport')));
  await flushPromises();
  expect(wrapper.get('section').classes()).toEqual(initialFrame);
  expect(wrapper.get('.query-label').text()).toBe('Manual observations unavailable');
  expect(wrapper.text()).not.toContain('No manual observations');
  getOperations.mockResolvedValueOnce(ResultExtensions.ok({ observations: [], observationCount: 0 }));
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'Refresh')!
    .trigger('click');
  await flushPromises();
  expect(wrapper.get('section').classes()).toEqual(initialFrame);
  expect(wrapper.get('.query-label').text()).toContain('No manual observations');
  expect(wrapper.get('.query-label').classes()).not.toContain('border');
  expect(wrapper.findAll('article')).toHaveLength(0);
});
it('keeps a confirmed empty manual region while its toolbar refresh is pending', async () => {
  let finish!: (value: unknown) => void;
  const getOperations = vi
    .fn()
    .mockResolvedValueOnce(ResultExtensions.ok({ observations: [], observationCount: 0 }))
    .mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          finish = resolve;
        }),
    );
  const wrapper = manual(getOperations);
  await flushPromises();
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'Refresh')!
    .trigger('click');
  expect(wrapper.get('.query-label').text()).toContain('No manual observations');
  expect(wrapper.find('.query-region[aria-busy=true]').exists()).toBe(false);
  expect(wrapper.findAll('article')).toHaveLength(0);
  finish(ResultExtensions.fail(ApiFailureFactory.create('transport')));
  await flushPromises();
  expect(wrapper.get('.query-label').text()).toContain('No manual observations');
});
