import { defineComponent, ref } from 'vue';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/fe/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/fe/foundation/http';
import { ImageEditRecipeExtensions } from '@wow-two-beta/fe/domain/imageEditing';
import { useMediaEditing } from '../src/application/blog/UseMediaEditing';
import type { MediaView } from '../src/domain/blog/BlogModels';
const api = vi.hoisted(() => ({ editingCapabilities: vi.fn(), previewMediaEdit: vi.fn(), applyMediaEdit: vi.fn() }));
vi.mock('../src/integration/blog', () => ({
  blogApi: api,
  describeFailure: () => 'Request failed; your cover is unchanged.',
}));
const original: MediaView = {
  sha256: 'a'.repeat(64),
  fileName: 'original.png',
  contentType: 'image/png',
  byteLength: 12,
  width: 640,
  height: 480,
  url: '/media/' + 'a'.repeat(64),
  previewUrl: '/media/' + 'a'.repeat(64) + '/preview',
};
const edited: MediaView = {
  ...original,
  sha256: 'b'.repeat(64),
  url: '/media/' + 'b'.repeat(64),
  previewUrl: '/media/' + 'b'.repeat(64) + '/preview',
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
const source = ref(original),
  allowed = ref(true),
  createUrl = vi.fn(() => 'blob:preview'),
  revokeUrl = vi.fn();
let controls: ReturnType<typeof useMediaEditing>, wrapper: VueWrapper;
const Host = defineComponent({
  setup() {
    controls = useMediaEditing(
      () => source.value,
      () => allowed.value,
    );
    return () => null;
  },
});
function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => {
    resolve = done;
  });
  return { promise, resolve };
}
async function setup(): Promise<void> {
  wrapper = mount(Host);
  await flushPromises();
}
beforeEach(() => {
  vi.clearAllMocks();
  source.value = original;
  allowed.value = true;
  vi.stubGlobal('URL', { createObjectURL: createUrl, revokeObjectURL: revokeUrl });
  api.editingCapabilities.mockResolvedValue(ResultExtensions.ok(capabilities));
  api.previewMediaEdit.mockResolvedValue(ResultExtensions.ok({ blob: new Blob(['pixels']), width: 640, height: 480 }));
  api.applyMediaEdit.mockResolvedValue(ResultExtensions.ok(edited));
});
afterEach(() => {
  wrapper?.unmount();
  vi.unstubAllGlobals();
});
describe('image editing session', () => {
  it('previews ephemeral bytes, clears stale previews after edits, and only applies explicitly', async () => {
    await setup();
    expect(api.applyMediaEdit).not.toHaveBeenCalled();
    await controls.preview(controls.recipe.value);
    expect(controls.previewUrl.value).toBe('blob:preview');
    expect(api.applyMediaEdit).not.toHaveBeenCalled();
    controls.update({ ...controls.recipe.value, rotate: 90 });
    expect(controls.previewUrl.value).toBe('');
    expect(revokeUrl).toHaveBeenCalledWith('blob:preview');
    expect(await controls.apply(controls.recipe.value)).toEqual(edited);
    expect(source.value.sha256).toBe(original.sha256);
    expect(api.applyMediaEdit).toHaveBeenCalledWith(
      original.sha256,
      expect.objectContaining({ rotate: 90 }),
      expect.any(AbortSignal),
    );
  });
  it('does not process viewer edits or unavailable background removal', async () => {
    allowed.value = false;
    await setup();
    expect(await controls.apply(controls.recipe.value)).toBeNull();
    allowed.value = true;
    await flushPromises();
    controls.update({ ...controls.recipe.value, removeBackground: true });
    expect(await controls.apply(controls.recipe.value)).toBeNull();
    expect(api.applyMediaEdit).not.toHaveBeenCalled();
  });
  it('aborts and ignores an applied asset when the source changed while processing', async () => {
    const response = deferred<ReturnType<typeof ResultExtensions.ok<MediaView>>>();
    api.applyMediaEdit.mockReturnValueOnce(response.promise);
    await setup();
    const pending = controls.apply(controls.recipe.value);
    const signal = api.applyMediaEdit.mock.calls[0]![2] as AbortSignal;
    source.value = edited;
    await flushPromises();
    expect(signal.aborted).toBe(true);
    response.resolve(ResultExtensions.ok(edited));
    expect(await pending).toBeNull();
    expect(controls.previewUrl.value).toBe('');
  });
  it('ignores late previews after permission loss or disposal and revokes completed preview URLs', async () => {
    const response = deferred<ReturnType<typeof ResultExtensions.ok<{ blob: Blob; width: number; height: number }>>>();
    api.previewMediaEdit.mockReturnValueOnce(response.promise);
    await setup();
    controls.update({ ...controls.recipe.value, rotate: 90 });
    const pending = controls.preview(controls.recipe.value);
    allowed.value = false;
    await flushPromises();
    expect((api.previewMediaEdit.mock.calls[0]![2] as AbortSignal).aborted).toBe(true);
    response.resolve(ResultExtensions.ok({ blob: new Blob(['pixels']), width: 640, height: 480 }));
    await pending;
    expect(createUrl).not.toHaveBeenCalled();
    expect(controls.recipe.value.rotate).toBe(90);
    allowed.value = true;
    await flushPromises();
    await controls.preview(controls.recipe.value);
    wrapper.unmount();
    expect(revokeUrl).toHaveBeenCalledWith('blob:preview');
  });
  it('does not replace the current cover after a processing error and allows retry', async () => {
    api.applyMediaEdit.mockResolvedValueOnce(
      ResultExtensions.fail(ApiFailureFactory.create('http', new Response(null, { status: 503 }))),
    );
    await setup();
    expect(await controls.apply(controls.recipe.value)).toBeNull();
    expect(source.value.sha256).toBe(original.sha256);
    expect(controls.error.value).toContain('unchanged');
    expect(controls.busy.value).toBe(false);
    expect(await controls.apply(controls.recipe.value)).toEqual(edited);
  });
  it('requires readable source dimensions and ignores an outdated recipe event', async () => {
    await setup();
    controls.update({ ...controls.recipe.value, rotate: 90 });
    expect(await controls.apply(ImageEditRecipeExtensions.create())).toBeNull();
    expect(api.applyMediaEdit).not.toHaveBeenCalled();
    source.value = { ...original, width: null };
    await flushPromises();
    expect(controls.locked.value).toBe(true);
  });
});
