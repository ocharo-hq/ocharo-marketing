import { describe, expect, it, vi } from 'vitest';
import { createBlogApi } from '../src/integration/blog/BlogApi';
import type { ImageEditRecipe } from '@wow-two-beta/fe/domain/imageEditing';
const sha = 'a'.repeat(64);
const recipe: ImageEditRecipe = {
  crop: null,
  rotate: 90,
  flipHorizontal: false,
  flipVertical: false,
  resize: null,
  output: { format: 'png', quality: 90 },
  removeBackground: false,
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
const media = {
  sha256: sha,
  fileName: 'edited.png',
  contentType: 'image/png',
  byteLength: 4,
  width: 640,
  height: 480,
  url: `/media/${sha}`,
  previewUrl: `/media/${sha}/preview`,
};
describe('image editing API contracts', () => {
  it('validates host capabilities and never exposes unavailable background removal as available', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(Response.json(capabilities))
      .mockResolvedValueOnce(Response.json({ ...capabilities, maximumPixels: -1 }));
    const api = createBlogApi(fetcher),
      signal = new AbortController().signal;
    expect(await api.editingCapabilities(signal)).toMatchObject({
      ok: true,
      value: { backgroundRemoval: { available: false } },
    });
    expect(await api.editingCapabilities(signal)).toMatchObject({ ok: false, failure: { code: 'protocol' } });
  });
  it('previews binary pixels without calling the persistence endpoint', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      new Response(new Uint8Array([1, 2, 3]), {
        headers: { 'Content-Type': 'image/png', 'X-Image-Width': '640', 'X-Image-Height': '480' },
      }),
    );
    const signal = new AbortController().signal,
      result = await createBlogApi(fetcher).previewMediaEdit(sha, recipe, signal);
    expect(result).toMatchObject({ ok: true, value: { width: 640, height: 480 } });
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(fetcher.mock.calls[0]![0]).toBe(`/api/blog/media/${sha}/edits/preview`);
    expect(fetcher.mock.calls[0]![1]).toMatchObject({
      method: 'POST',
      signal,
      credentials: 'same-origin',
      body: JSON.stringify(recipe),
    });
  });
  it('rejects malformed binary dimensions or non-image responses', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(
        new Response('html', {
          headers: { 'Content-Type': 'text/html', 'X-Image-Width': '640', 'X-Image-Height': '480' },
        }),
      )
      .mockResolvedValueOnce(new Response('pixels', { headers: { 'Content-Type': 'image/png' } }));
    const api = createBlogApi(fetcher),
      signal = new AbortController().signal;
    expect(await api.previewMediaEdit(sha, recipe, signal)).toMatchObject({ ok: false, failure: { code: 'protocol' } });
    expect(await api.previewMediaEdit(sha, recipe, signal)).toMatchObject({ ok: false, failure: { code: 'protocol' } });
  });
  it('applies once, decodes only owned SHA-addressed media, and rejects external asset URLs', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(Response.json(media))
      .mockResolvedValueOnce(Response.json({ ...media, url: 'https://untrusted.example/pixels' }));
    const api = createBlogApi(fetcher),
      signal = new AbortController().signal;
    expect(await api.applyMediaEdit(sha, recipe, signal)).toMatchObject({ ok: true, value: media });
    expect(fetcher.mock.calls[0]![0]).toBe(`/api/blog/media/${sha}/edits`);
    expect(await api.applyMediaEdit(sha, recipe, signal)).toMatchObject({ ok: false, failure: { code: 'protocol' } });
  });
  it('does not retry an expired connection, and distinguishes cancellation from failure', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValueOnce(Response.json({ detail: 'Reconnect from Platform' }, { status: 401 }))
      .mockRejectedValueOnce(new DOMException('Aborted', 'AbortError'));
    const api = createBlogApi(fetcher),
      controller = new AbortController();
    expect(await api.previewMediaEdit(sha, recipe, controller.signal)).toMatchObject({
      ok: false,
      failure: { status: 401 },
    });
    expect(fetcher).toHaveBeenCalledTimes(1);
    controller.abort();
    expect(await api.previewMediaEdit(sha, recipe, controller.signal)).toMatchObject({
      ok: false,
      failure: { code: 'cancelled' },
    });
  });
});
