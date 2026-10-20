import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { defineComponent, ref, type Ref } from 'vue';
import { createMemoryHistory, createRouter } from 'vue-router';
import MarketingRoot from '../src/bootstrap/AppRoot.vue';
import MarketingLayout from '../src/bootstrap/AppLayout.vue';
import EditorialRoot from '../../editorial/src/bootstrap/AppRoot.vue';
import EditorialLayout from '../../editorial/src/bootstrap/AppLayout.vue';

const controls = vi.hoisted(() => ({
  access: undefined as unknown as Ref<boolean>,
  workspace: undefined as unknown as Ref<unknown>,
  refresh: vi.fn(),
  loadTopics: vi.fn(),
}));
vi.mock('@ocharomarketing/connect', async (importOriginal) => {
  const { ref } = await import('vue');
  controls.access = ref(false);
  return { ...(await importOriginal<typeof import('@ocharomarketing/connect')>()), integrationAccess: controls.access };
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

const disposers: (() => void)[] = [];
let fetcher: ReturnType<typeof vi.fn>;
beforeEach(() => {
  controls.access.value = false;
  controls.workspace.value = null;
  controls.refresh.mockReset().mockImplementation(async () => {
    controls.workspace.value = { drafts: [], brand: { handle: 'Ocharo' } };
  });
  controls.loadTopics.mockReset().mockResolvedValue(undefined);
  fetcher = vi.fn(async () => Response.json({}));
  vi.stubGlobal('fetch', fetcher);
});
afterEach(() => {
  for (const dispose of disposers.splice(0)) dispose();
  vi.unstubAllGlobals();
});

for (const [name, root, layout] of [
  ['Marketing', MarketingRoot, MarketingLayout],
  ['Editorial', EditorialRoot, EditorialLayout],
] as const) {
  describe(`${name} private editor lifecycle`, () => {
    it('waits for authentication and keeps the mounted draft when the session expires', async () => {
      const editor = defineComponent({
        setup() {
          return { draft: ref('Original draft') };
        },
        template: '<input aria-label="Draft body" v-model="draft" />',
      });
      // The real Ocharo frame as a layout route, as each app's router mounts it, around a stand-in editor.
      const router = createRouter({
        history: createMemoryHistory(),
        routes: [{ path: '/', component: layout, children: [{ path: '', component: editor }] }],
      });
      await router.push('/');
      await router.isReady();
      const wrapper = mount(root, {
        global: {
          plugins: [router],
          stubs: {
            FeedbackToastHost: true,
            RouteAnnouncer: true,
            AppErrorBoundary: { template: '<slot />' },
          },
        },
      });
      disposers.push(() => wrapper.unmount());
      await flushPromises();
      expect(controls.refresh).not.toHaveBeenCalled();
      expect(controls.loadTopics).not.toHaveBeenCalled();
      expect(fetcher).not.toHaveBeenCalled();
      expect(wrapper.find('input[aria-label="Draft body"]').exists()).toBe(false);
      expect(wrapper.find('header button[aria-haspopup="menu"]').exists()).toBe(true);
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
