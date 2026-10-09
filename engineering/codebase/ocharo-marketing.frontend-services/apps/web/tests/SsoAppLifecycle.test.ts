import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { defineComponent, ref, type Ref } from 'vue';
import { createMemoryHistory, createRouter } from 'vue-router';
import MarketingRoot from '../src/bootstrap/AppRoot.vue';
import EditorialRoot from '../../editorial/src/bootstrap/AppRoot.vue';

const controls = vi.hoisted(() => ({
  access: undefined as unknown as Ref<boolean>,
  workspace: undefined as unknown as Ref<unknown>,
  refresh: vi.fn(),
  loadTopics: vi.fn(),
}));
vi.mock('@ocharomarketing/connect', async () => {
  const { ref } = await import('vue');
  controls.access = ref(false);
  return { integrationAccess: controls.access };
});
vi.mock('../src/application/marketing', async () => {
  const { ref } = await import('vue');
  controls.workspace = ref(null);
  return {
    useMarketingWorkspace: () => ({
      workspace: controls.workspace,
      error: ref(null),
      notice: ref(null),
      pendingJobs: ref(false),
      busy: ref(null),
      loading: ref(false),
      refresh: controls.refresh,
      navigate: vi.fn(),
    }),
  };
});
vi.mock('../../editorial/src/application/blog', async () => {
  const { ref } = await import('vue');
  return { useBlogStore: () => ({ error: ref(null), notice: ref(null), loadTopics: controls.loadTopics }) };
});
vi.mock('@ocharomarketing/connect/ConnectDesk.vue', () => ({
  default: { template: '<div>Connection controls</div>' },
}));
vi.mock('@ocharomarketing/connect/BuildVersion.vue', () => ({ default: { template: '<span>test</span>' } }));

const disposers: (() => void)[] = [];
beforeEach(() => {
  controls.access.value = false;
  controls.workspace.value = null;
  controls.refresh.mockReset().mockImplementation(async () => {
    controls.workspace.value = { drafts: [], brand: { handle: 'Ocharo' } };
  });
  controls.loadTopics.mockReset().mockResolvedValue(undefined);
});
afterEach(() => {
  for (const dispose of disposers.splice(0)) dispose();
});

for (const [name, root] of [
  ['Marketing', MarketingRoot],
  ['Editorial', EditorialRoot],
] as const) {
  describe(`${name} private editor lifecycle`, () => {
    it('waits for authentication and keeps the mounted draft when the session expires', async () => {
      const editor = defineComponent({
        setup() {
          return { draft: ref('Original draft') };
        },
        template: '<input aria-label="Draft body" v-model="draft" />',
      });
      const router = createRouter({
        history: createMemoryHistory(),
        routes: [{ path: '/:pathMatch(.*)*', component: editor }],
      });
      await router.push('/');
      await router.isReady();
      const wrapper = mount(root, {
        global: {
          plugins: [router],
          stubs: {
            FeedbackToastHost: true,
            RouteAnnouncer: true,
            LanguagePicker: true,
            AppErrorBoundary: { template: '<slot />' },
          },
        },
      });
      disposers.push(() => wrapper.unmount());
      await flushPromises();
      expect(controls.refresh).not.toHaveBeenCalled();
      expect(controls.loadTopics).not.toHaveBeenCalled();
      expect(wrapper.find('input[aria-label="Draft body"]').exists()).toBe(false);
      controls.access.value = true;
      await flushPromises();
      const field = wrapper.get('input[aria-label="Draft body"]');
      await field.setValue('Unfinished work');
      controls.access.value = false;
      await flushPromises();
      expect(wrapper.get('input[aria-label="Draft body"]').element).toBe(field.element);
      expect((field.element as HTMLInputElement).value).toBe('Unfinished work');
      expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined();
      controls.access.value = true;
      await flushPromises();
      expect(wrapper.get('fieldset').attributes('disabled')).toBeUndefined();
      expect((field.element as HTMLInputElement).value).toBe('Unfinished work');
      expect(name === 'Marketing' ? controls.refresh : controls.loadTopics).toHaveBeenCalledTimes(1);
    });
  });
}
