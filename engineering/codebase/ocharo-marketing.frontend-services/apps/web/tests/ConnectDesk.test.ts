import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import ConnectDesk from '../../../packages/connect/src/ConnectDesk.vue';
import {
  integrationError,
  integrationMode,
  integrationSession,
  integrationIdentityChanged,
  integrationReturnPath,
  refreshIntegration,
} from '../../../packages/connect/src/session';

vi.mock('../../../packages/connect/src/session', async () => {
  const { ref } = await import('vue');
  return {
    integrationMode: ref('sso'),
    integrationError: ref(''),
    integrationIdentityChanged: ref(false),
    integrationSession: ref(null),
    integrationChecking: ref(false),
    integrationReturnPath: ref(null),
    refreshIntegration: vi.fn(),
    ssoLoginUrl: (path = '/') => `/api/sso/login?returnUrl=${encodeURIComponent(path)}`,
  };
});
const disposers: (() => void)[] = [];
beforeEach(() => {
  integrationMode.value = 'sso';
  integrationError.value = '';
  integrationIdentityChanged.value = false;
  integrationSession.value = { mode: 'sso', active: false, configured: true };
  integrationReturnPath.value = null;
  vi.mocked(refreshIntegration).mockReset();
});
afterEach(() => {
  for (const dispose of disposers.splice(0)) dispose();
  vi.useRealTimers();
});
function render() {
  const wrapper = mount(ConnectDesk, { props: { platformUrl: 'https://desk.example/' } });
  disposers.push(() => wrapper.unmount());
  return wrapper;
}
it('reconnects in a separate tab without discarding the current editor', () => {
  const wrapper = render();
  const link = wrapper.get('a');
  expect(link.attributes('href')).toBe('/api/sso/login?returnUrl=%2F');
  expect(link.attributes('target')).toBe('_blank');
  expect(link.attributes('rel')).toContain('noopener');
  expect(wrapper.text()).toContain('Your unsaved edits stay here');
  expect(wrapper.text()).not.toContain('Open Connected apps in Platform');
});
it('reconnects to the page where the session expired', () => {
  integrationReturnPath.value = '/posts/draft-1?tab=copy';
  const wrapper = render();
  expect(wrapper.get('a').attributes('href')).toBe('/api/sso/login?returnUrl=%2Fposts%2Fdraft-1%3Ftab%3Dcopy');
});
it('does not present manual connection fallback or retry automatically when SSO status is unknown', () => {
  vi.useFakeTimers();
  integrationMode.value = 'unknown';
  integrationSession.value = null;
  integrationError.value = 'Connection status is unavailable. Your unsaved work stays here.';
  const wrapper = render();
  expect(wrapper.text()).toContain('Check your Ocharo connection');
  expect(wrapper.text()).not.toContain('Open Connected apps in Platform');
  vi.advanceTimersByTime(60000);
  window.dispatchEvent(new Event('focus'));
  expect(refreshIntegration).not.toHaveBeenCalled();
});
it('retains manual integration instructions only when SSO is disabled', () => {
  integrationMode.value = 'legacy';
  integrationSession.value = { mode: 'cookie', active: false, configured: true };
  const wrapper = render();
  expect(wrapper.text()).toContain('Open Connected apps in Platform');
  expect(wrapper.get('a').attributes('href')).toBe('https://desk.example/');
});
