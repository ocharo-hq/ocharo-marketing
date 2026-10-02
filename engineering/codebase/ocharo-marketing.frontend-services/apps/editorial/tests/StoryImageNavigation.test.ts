import { computed, defineComponent, ref, type ComputedRef, type Ref } from 'vue';
import type { ArticleInput } from '../src/domain/blog/BlogModels';
import { createMemoryHistory, createRouter, RouterView, type Router } from 'vue-router';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import type { ImageEditRecipe } from '@wow-two-beta/ui-vue/domain/imageEditing';
import { emptyArticle } from '../src/domain/blog/BlogRules';
import StoryEditorPage from '../src/presentation/blog/StoryEditorPage.vue';
const mocks = vi.hoisted(() => ({
  editor: null as unknown,
  store: null as unknown,
  editingCapabilities: vi.fn(),
  previewMediaEdit: vi.fn(),
  applyMediaEdit: vi.fn(),
}));
vi.mock('../src/application/blog', () => ({
  useArticleEditor: () => mocks.editor,
  useBlogStore: () => mocks.store,
  loadStoryOptions: async () => [],
}));
vi.mock('../src/integration/blog', () => ({ blogApi: mocks, describeFailure: () => 'Request failed.' }));
const sha = 'a'.repeat(64),
  media = {
    sha256: sha,
    fileName: 'cover.png',
    contentType: 'image/png',
    byteLength: 12,
    width: 640,
    height: 480,
    url: `/media/${sha}`,
    previewUrl: `/media/${sha}/preview`,
  };
const capabilities = {
  crop: true,
  rotate: true,
  flip: true,
  resize: true,
  formats: ['png', 'jpeg', 'webp'],
  maximumBytes: 20971520,
  maximumPixels: 20000000,
  maximumDimension: 8192,
  backgroundRemoval: { available: false, model: null },
};
const imageStub = {
  name: 'ImageEditor',
  props: ['modelValue'],
  emits: ['update:modelValue', 'preview', 'apply'],
  template: '<div data-tools />',
};
let wrapper: VueWrapper, router: Router;
let editor: {
  draft: Ref<ArticleInput>;
  dirty: ComputedRef<boolean>;
  busy: Ref<boolean>;
  save: ReturnType<typeof vi.fn>;
};
beforeEach(() => {
  vi.clearAllMocks();
  vi.stubGlobal(
    'confirm',
    vi.fn(() => false),
  );
  vi.stubGlobal(
    'URL',
    class extends URL {
      static createObjectURL = vi.fn(() => 'blob:image-preview');
      static revokeObjectURL = vi.fn();
    },
  );
  const document = {
    ...emptyArticle('pieces'),
    title: 'Saved story',
    slug: 'saved-story',
    status: 'draft',
    cover: { mediaSha256: sha, alt: 'Cover', caption: 'Original caption', cardClass: null },
  };
  const draft = ref(structuredClone(document)),
    baseline = JSON.stringify(document);
  editor = { draft, dirty: computed(() => JSON.stringify(draft.value) !== baseline), busy: ref(false), save: vi.fn() };
  mocks.editor = {
    ...editor,
    detail: ref({ id: 'story', revision: 1, document }),
    canEdit: ref(true),
    loading: ref(false),
    history: ref([]),
    media: ref([media]),
    products: ref([]),
    stats: ref({ words: 10, minutes: 1 }),
    blockers: ref([]),
    previewNonce: ref(0),
    load: async () => {},
    loadLibraries: async () => {},
  };
  mocks.store = {
    topics: ref([{ key: 'pieces', short: 'Pieces' }]),
    links: { preview: () => '/preview/story', media: (value: string) => `/media/${value}/preview` },
  };
  mocks.editingCapabilities.mockResolvedValue(ResultExtensions.ok(capabilities));
  mocks.previewMediaEdit.mockResolvedValue(
    ResultExtensions.ok({ blob: new Blob(['pixels']), width: 480, height: 640 }),
  );
});
afterEach(() => {
  wrapper?.unmount();
  vi.unstubAllGlobals();
});
async function setup() {
  router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/stories/:id', component: StoryEditorPage },
      { path: '/', component: defineComponent({ template: '<div>Stories</div>' }) },
    ],
  });
  await router.push('/stories/story');
  await router.isReady();
  wrapper = mount(RouterView, {
    global: { plugins: [router], stubs: { Icon: true, SectionTextEditor: true, ImageEditor: imageStub } },
  });
  await flushPromises();
  await wrapper
    .findAll('button')
    .find((button) => button.text() === 'Edit image')!
    .trigger('click');
  await flushPromises();
  const image = wrapper.get('.image-workbench img');
  Object.defineProperty(image.element, 'naturalWidth', { value: 640 });
  Object.defineProperty(image.element, 'naturalHeight', { value: 480 });
  await image.trigger('load');
  await flushPromises();
}
function unloadPrevented(): boolean {
  const event = new Event('beforeunload', { cancelable: true });
  window.dispatchEvent(event);
  return event.defaultPrevented;
}
describe('unapplied cover edits navigation', () => {
  it('guards a saved story after rotation and preview; explicit Cancel clears only the image guard', async () => {
    await setup();
    expect(editor.dirty.value).toBe(false);
    expect(unloadPrevented()).toBe(false);
    const tools = wrapper.getComponent({ name: 'ImageEditor' }),
      recipe = { ...(tools.props('modelValue') as ImageEditRecipe), rotate: 90 };
    tools.vm.$emit('update:modelValue', recipe);
    await flushPromises();
    tools.vm.$emit('preview', recipe);
    await flushPromises();
    expect(mocks.previewMediaEdit).toHaveBeenCalledOnce();
    expect(editor.dirty.value).toBe(false);
    expect(unloadPrevented()).toBe(true);
    expect(wrapper.text()).toContain('Apply or cancel image edits before saving.');
    await router.push('/');
    expect(window.confirm).toHaveBeenCalledOnce();
    expect(router.currentRoute.value.path).toBe('/stories/story');
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Cancel image edits')!
      .trigger('click');
    await flushPromises();
    expect(unloadPrevented()).toBe(false);
    await router.push('/');
    expect(router.currentRoute.value.path).toBe('/');
    expect(window.confirm).toHaveBeenCalledOnce();
    expect(editor.save).not.toHaveBeenCalled();
    expect(mocks.applyMediaEdit).not.toHaveBeenCalled();
  });
  it('preserves the story dirty guard when image edits are cancelled', async () => {
    await setup();
    editor.draft.value.title = 'Unsaved words';
    await flushPromises();
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Cancel image edits')!
      .trigger('click');
    await flushPromises();
    expect(unloadPrevented()).toBe(true);
    await router.push('/');
    expect(router.currentRoute.value.path).toBe('/stories/story');
  });
});
