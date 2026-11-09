import { flushPromises, mount } from '@vue/test-utils';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BootSplash from '@ocharomarketing/connect/BootSplash.vue';

afterEach(() => {
  vi.useRealTimers();
});

describe('BootSplash', () => {
  it('takes over the static twin with the same wordmarks, fills the bar, then lifts', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'requestAnimationFrame', 'cancelAnimationFrame'] });
    const wrapper = mount(BootSplash, { props: { base: '/studio/' }, attachTo: document.body });
    const splash = () => document.body.querySelector('[role="status"]');
    expect(splash()).not.toBeNull();
    expect(
      [...document.body.querySelectorAll<HTMLImageElement>('img.boot-splash-wordmark')].map((image) => [
        image.className,
        image.getAttribute('src'),
      ]),
    ).toEqual([
      ['boot-splash-wordmark boot-splash-wordmark-ink', '/studio/ocharo-wordmark-ink.png'],
      ['boot-splash-wordmark boot-splash-wordmark-reverse', '/studio/ocharo-wordmark-reverse.png'],
    ]);
    const bar = () => document.body.querySelector('[role="progressbar"]');
    expect(bar()?.getAttribute('aria-valuenow')).toBe('35');
    await vi.advanceTimersToNextFrame();
    await flushPromises();
    expect(bar()?.getAttribute('aria-valuenow')).toBe('100');
    await vi.advanceTimersByTimeAsync(300);
    await flushPromises();
    expect(splash()?.className ?? '').toContain('animate-');
    wrapper.unmount();
  });
});
