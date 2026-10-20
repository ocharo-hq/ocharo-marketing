import { flushPromises } from '@vue/test-utils';
import { effectScope } from 'vue';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ServiceVersionState, useServiceVersion } from '@ocharomarketing/connect/ServiceVersion';
afterEach(() => vi.unstubAllGlobals());

/** Runs the account menu's version read inside its own scope, as the frame does. */
function inScope() {
  const scope = effectScope();
  const version = scope.run(() => useServiceVersion())!;
  return { ...version, stop: () => scope.stop() };
}

describe('API version in the account menu', () => {
  it('reads the service version and commit anonymously, only when the menu asks', async () => {
    const fetcher = vi.fn().mockResolvedValue(Response.json({ version: '0.1.8+abc123' }));
    vi.stubGlobal('fetch', fetcher);
    const { version, refresh, stop } = inScope();
    expect(version.value).toBe(ServiceVersionState.NotChecked);
    expect(fetcher).not.toHaveBeenCalled();
    const read = refresh();
    expect(version.value).toBe(ServiceVersionState.Checking);
    await read;
    expect(fetcher).toHaveBeenCalledWith(
      '/api/system/status',
      expect.objectContaining({ cache: 'no-store', credentials: 'omit' }),
    );
    expect(version.value).toBe('0.1.8+abc123');
    stop();
  });

  it('shows unavailable instead of inventing a backend version after failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));
    const { version, refresh, stop } = inScope();
    await refresh();
    expect(version.value).toBe(ServiceVersionState.Unavailable);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ version: 7 })));
    await refresh();
    expect(version.value).toBe(ServiceVersionState.Unavailable);
    stop();
  });

  it('aborts an in-flight read when the owning frame unmounts', async () => {
    const fetcher = vi.fn().mockReturnValue(new Promise(() => {}));
    vi.stubGlobal('fetch', fetcher);
    const { refresh, stop } = inScope();
    void refresh();
    await flushPromises();
    const signal = fetcher.mock.calls[0]![1].signal as AbortSignal;
    stop();
    expect(signal.aborted).toBe(true);
  });
});
