import { mount, flushPromises } from '@vue/test-utils';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import CardPreview from '../src/presentation/marketing/CardPreview.vue';
import { feedbackBus } from '../src/application/shared/UseFeedback';
const { previewCard } = vi.hoisted(() => ({ previewCard: vi.fn() }));
vi.mock('../src/application/marketing', () => ({ useMarketingWorkspace: () => ({ previewCard }) }));
vi.mock('@wow-two-beta/fe/foundation/observers', () => ({ useResizeObserver: vi.fn() }));
const composition = { productId: 'piece-1', presetId: 'editorial', title: 'A piece', locale: 'en' };
let wrapper: ReturnType<typeof mount>;
beforeEach(() => {
  vi.useFakeTimers();
  previewCard.mockReset();
  vi.spyOn(feedbackBus, 'notify');
});
afterEach(() => {
  wrapper?.unmount();
  vi.useRealTimers();
  vi.restoreAllMocks();
});
async function finishPreview() {
  await vi.advanceTimersByTimeAsync(200);
  await flushPromises();
}
it('keeps last-good artwork after a failed preview refresh and reports only a safe toast', async () => {
  previewCard.mockResolvedValueOnce({ ok: true, value: { html: '<h1>Last good preview</h1>', issues: [] } });
  wrapper = mount(CardPreview, { props: { composition } });
  await finishPreview();
  expect(wrapper.get('iframe').attributes('srcdoc')).toContain('Last good');
  previewCard.mockResolvedValueOnce({
    ok: false,
    failure: { code: 'http', status: 503, message: 'private server response' },
  });
  await wrapper.setProps({ composition: { ...composition, title: 'Revised' } });
  expect(wrapper.find('.animate-pulse').exists()).toBe(true);
  expect(wrapper.text()).not.toContain('Preparing preview');
  await finishPreview();
  expect(wrapper.get('iframe').attributes('srcdoc')).toContain('Last good');
  expect(wrapper.text()).not.toContain('private');
  expect(wrapper.text()).toContain('Retry preview');
  expect(feedbackBus.notify).toHaveBeenCalledWith(
    expect.objectContaining({ description: expect.stringContaining('try again') }),
  );
  previewCard.mockResolvedValueOnce({ ok: true, value: { html: '<h1>Read preview</h1>', issues: [] } });
  await wrapper.get('button').trigger('click');
  await finishPreview();
  expect(wrapper.get('iframe').attributes('srcdoc')).toContain('Read preview');
});
it('shows neutral retry after a first preview failure without a loading message', async () => {
  previewCard.mockResolvedValueOnce({
    ok: false,
    failure: { code: 'http', status: 500, message: 'private server response' },
  });
  wrapper = mount(CardPreview, { props: { composition } });
  expect(wrapper.find('.animate-pulse').exists()).toBe(true);
  expect(wrapper.text()).not.toContain('Preparing preview');
  await finishPreview();
  expect(wrapper.text()).toContain('Preview unavailable');
  expect(wrapper.text()).not.toContain('private');
  expect(wrapper.find('.animate-pulse').exists()).toBe(false);
});
