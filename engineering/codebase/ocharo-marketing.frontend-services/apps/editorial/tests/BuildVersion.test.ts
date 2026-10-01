import { flushPromises, mount } from '@vue/test-utils';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BuildVersion from '@ocharomarketing/connect/BuildVersion.vue';
afterEach(() => vi.unstubAllGlobals());
describe('build identity disclosure', () => {
  it('reports the app version and requests the service commit only on explicit opening', async () => {
    const fetcher = vi.fn().mockResolvedValue(Response.json({ version: '0.1.8+abc123' }));
    vi.stubGlobal('fetch', fetcher);
    const wrapper = mount(BuildVersion, { props: { appVersion: 'dev', serviceLabel: 'Editorial' } });
    expect(wrapper.get('summary').text()).toBe('dev');
    expect(fetcher).not.toHaveBeenCalled();
    (wrapper.get('details').element as HTMLDetailsElement).open = true;
    await wrapper.get('details').trigger('toggle');
    await flushPromises();
    expect(fetcher).toHaveBeenCalledWith('/api/system/status', expect.objectContaining({ cache: 'no-store' }));
    expect(wrapper.text()).toContain('0.1.8+abc123');
    wrapper.unmount();
  });
  it('shows unavailable instead of inventing a backend version after failure', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));
    const wrapper = mount(BuildVersion, { props: { appVersion: '0.1.8', serviceLabel: 'Marketing' } });
    (wrapper.get('details').element as HTMLDetailsElement).open = true;
    await wrapper.get('details').trigger('toggle');
    await flushPromises();
    expect(wrapper.text()).toContain('Service version unavailable');
    expect(wrapper.text()).toContain('App: 0.1.8');
    wrapper.unmount();
  });
  it('aborts an in-flight read when the owning app unmounts', async () => {
    const fetcher = vi.fn().mockReturnValue(new Promise(() => {}));
    vi.stubGlobal('fetch', fetcher);
    const wrapper = mount(BuildVersion, { props: { appVersion: 'dev', serviceLabel: 'Editorial' } });
    (wrapper.get('details').element as HTMLDetailsElement).open = true;
    await wrapper.get('details').trigger('toggle');
    const signal = fetcher.mock.calls[0]![1].signal as AbortSignal;
    wrapper.unmount();
    expect(signal.aborted).toBe(true);
  });
});
