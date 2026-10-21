import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DOMWrapper, flushPromises, type VueWrapper } from '@vue/test-utils';
import { integrationAccess, integrationSession, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { mountMarketing, SignedIn, type MarketingAppOptions } from './helpers/MarketingApp';

let wrapper: VueWrapper | undefined;

/** Mounts the desk with an Ocharo session, as startup resolves it before mounting. */
async function renderSignedIn(path = '/channels', options: MarketingAppOptions = {}) {
  const mounted = await mountMarketing(path, { ssoSession: SignedIn, ...options });
  wrapper = mounted.wrapper;
  return mounted;
}

async function openAccountMenu() {
  await wrapper!.get('header button[aria-haspopup="menu"]').trigger('click');
  await flushPromises();
  return new DOMWrapper(document.querySelector<HTMLElement>('[role="menu"]')!);
}

/** The requests the desk sent to end its Desk grant. */
function logoutCalls() {
  return vi.mocked(fetch).mock.calls.filter(([input]) => String(input) === '/api/sso/logout');
}

beforeEach(() => {
  localStorage.clear();
  useUiLocale().setUiLocale('en');
});
afterEach(() => {
  wrapper?.unmount();
  wrapper = undefined;
  document.body.innerHTML = '';
  vi.unstubAllGlobals();
});

describe('Marketing account menu with an Ocharo session', () => {
  it('names the session when Desk sends no name or email, links the Ocharo account and ends with sign out', async () => {
    await renderSignedIn();

    const menu = await openAccountMenu();

    expect(menu.get('[data-account-menu-identity]').text()).toBe('Signed in through Ocharo Desk');
    expect(wrapper!.get('header button[aria-haspopup="menu"]').attributes('aria-label')).toBe('Account menu');
    const rows = menu.findAll('[role="menuitem"]');
    expect(rows.map((item) => item.text().trim())).toEqual(['Ocharo account (opens in a new tab)', 'Sign out']);
    expect(menu.findAll('[role="menuitem"], [role="menuitemradio"]').at(-1)!.attributes()).toHaveProperty(
      'data-account-menu-sign-out',
    );
    expect(rows[0]!.element.tagName).toBe('A');
    expect(rows[0]!.attributes()).toMatchObject({
      href: 'https://desk.example.test/',
      target: '_blank',
      rel: 'noopener noreferrer',
    });
  });

  it('names the person with the Desk display name and email, without the session fallback', async () => {
    await renderSignedIn('/channels', {
      ssoSession: { ...SignedIn, name: 'Maftuna Rakhimova', email: 'maftuna@ocharo.example.test' },
    });

    const menu = await openAccountMenu();

    expect(menu.get('[data-account-menu-identity]').text()).toBe('Maftuna Rakhimovamaftuna@ocharo.example.test');
    expect(menu.text()).not.toContain('Signed in through Ocharo Desk');
    expect(wrapper!.get('header button[aria-haspopup="menu"]').attributes('aria-label')).toBe(
      'Account menu for Maftuna Rakhimova',
    );
  });

  it.each([
    ['only a display name', { name: 'Maftuna Rakhimova', email: null }, 'Maftuna Rakhimova'],
    ['only an email', { name: null, email: 'maftuna@ocharo.example.test' }, 'maftuna@ocharo.example.test'],
  ])('shows %s without the session fallback', async (_case, identity, shown) => {
    await renderSignedIn('/channels', { ssoSession: { ...SignedIn, ...identity } });

    const menu = await openAccountMenu();

    expect(menu.get('[data-account-menu-identity]').text()).toBe(shown);
  });

  it('signs this app out with its CSRF token, keeps the page and offers to reconnect without an expiry notice', async () => {
    await renderSignedIn();
    const expired = vi.fn();
    const unsubscribe = subscribeIntegrationExpired(expired);

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(logoutCalls()).toHaveLength(1);
    const [, init] = logoutCalls()[0]!;
    expect(init).toMatchObject({ method: 'POST', credentials: 'same-origin' });
    expect(new Headers(init?.headers).get('X-CSRF-Token')).toBe('csrf-token');
    expect(integrationSession.value).toMatchObject({ active: false, canEdit: false });
    expect(integrationAccess.value).toBe(false);
    expect(expired).not.toHaveBeenCalled();
    expect(wrapper!.get('fieldset').attributes('disabled')).toBeDefined();
    expect(wrapper!.get('[aria-label="Platform connection"]').text()).toContain('Reconnect');
    expect(wrapper!.get('[aria-label="Platform connection"] a').attributes('target')).toBe('_blank');
    await wrapper!.get('header button[aria-haspopup="menu"]').trigger('click');
    await flushPromises();
    expect(document.querySelector('[data-account-menu-sign-out]')).toBeNull();
    unsubscribe();
  });

  it('holds sign-out while a post has unsaved edits, and says why', async () => {
    await renderSignedIn('/posts/new');
    const notify = vi.spyOn(feedbackBus, 'notify');
    await wrapper!.get('main input[aria-label="Post title"]').setValue('Unsaved title');

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(logoutCalls()).toHaveLength(0);
    expect(integrationAccess.value).toBe(true);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Save or discard your unsaved changes before signing out.' }),
    );
    expect((wrapper!.get('main input[aria-label="Post title"]').element as HTMLInputElement).value).toBe(
      'Unsaved title',
    );
  });

  it.each([
    [503, 'Ocharo Desk could not end this sign-in. Your work stays here; try again shortly.'],
    [400, 'This tab could not confirm its Ocharo sign-in. Reload the app, then sign out.'],
  ])('keeps the session and reports a display-safe reason when sign-out answers %i', async (status, reason) => {
    await renderSignedIn('/channels', { logoutStatus: status });
    const notify = vi.spyOn(feedbackBus, 'notify');

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(logoutCalls()).toHaveLength(1);
    expect(integrationAccess.value).toBe(true);
    expect(notify).toHaveBeenCalledWith(expect.objectContaining({ tone: 'danger', description: reason }));
  });
});
