import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/fe/foundation/results';
import CoverImageEditor from '../src/presentation/blog/components/CoverImageEditor.vue';
import type { ImageEditRecipe } from '@wow-two-beta/fe/domain/imageEditing';
const api = vi.hoisted(() => ({ editingCapabilities: vi.fn(), previewMediaEdit: vi.fn(), applyMediaEdit: vi.fn() }));
vi.mock('../src/integration/blog', () => ({ blogApi: api, describeFailure: () => 'Request failed.' }));
const sha = 'a'.repeat(64),
  media = {
    sha256: sha,
    fileName: 'original.jpg',
    contentType: 'image/jpeg',
    byteLength: 12,
    width: 1400,
    height: 1000,
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
const stub = {
  name: 'ImageEditor',
  props: ['src', 'naturalWidth', 'naturalHeight', 'modelValue', 'canRemoveBackground'],
  emits: ['update:modelValue', 'preview', 'apply'],
  template: '<div data-tools />',
};
let wrapper: VueWrapper;
beforeEach(() => {
  vi.clearAllMocks();
  api.editingCapabilities.mockResolvedValue(ResultExtensions.ok(capabilities));
});
afterEach(() => wrapper?.unmount());
async function setup(): Promise<void> {
  wrapper = mount(CoverImageEditor, {
    props: { media, alt: 'Our cover', canEdit: true },
    global: { stubs: { ImageEditor: stub } },
  });
  await flushPromises();
}
async function loadImage(): Promise<void> {
  const image = wrapper.get('img');
  Object.defineProperty(image.element, 'naturalWidth', { value: 4000 });
  Object.defineProperty(image.element, 'naturalHeight', { value: 3000 });
  await image.trigger('load');
  await flushPromises();
}
describe('cover image editor integration', () => {
  it('waits for the original to load and passes its real dimensions instead of a stored preview size', async () => {
    await setup();
    expect(wrapper.find('[data-tools]').exists()).toBe(false);
    expect(wrapper.get('img').attributes('src')).toBe(`/media/${sha}`);
    await loadImage();
    const tools = wrapper.getComponent({ name: 'ImageEditor' });
    expect(tools.props('naturalWidth')).toBe(4000);
    expect(tools.props('naturalHeight')).toBe(3000);
    expect(tools.props('src')).not.toContain('/preview');
    expect(tools.props('canRemoveBackground')).toBe(false);
  });
  it('explains the JPEG white background while PNG remains transparent-capable', async () => {
    await setup();
    await loadImage();
    const tools = wrapper.getComponent({ name: 'ImageEditor' });
    expect(wrapper.text()).not.toContain('JPEG fills');
    const recipe = tools.props('modelValue') as ImageEditRecipe;
    tools.vm.$emit('update:modelValue', { ...recipe, output: { format: 'jpeg', quality: 90 } });
    await flushPromises();
    expect(wrapper.text()).toContain('JPEG fills transparent areas with white');
    expect(wrapper.text()).toContain('Automatic background removal is unavailable');
  });
  it('fails safely when the original cannot be loaded', async () => {
    await setup();
    await wrapper.get('img').trigger('error');
    expect(wrapper.get('[role=alert]').text()).toContain('cover is unchanged');
    expect(wrapper.find('[data-tools]').exists()).toBe(false);
    expect(api.previewMediaEdit).not.toHaveBeenCalled();
    expect(api.applyMediaEdit).not.toHaveBeenCalled();
  });
});
