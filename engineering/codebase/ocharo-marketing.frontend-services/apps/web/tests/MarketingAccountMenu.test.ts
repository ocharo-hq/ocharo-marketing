import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DOMWrapper, flushPromises, type VueWrapper } from '@vue/test-utils';
import { integrationAccess, integrationSession, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { EndSessionUrl, mountMarketing, navigations, SignedIn, type MarketingAppOptions } from './helpers/MarketingApp';

let wrapper: VueWrapper | undefined;

/** Mounts the desk with an Ocharo session, as startup resolves it before mounting. */
async function renderSignedIn(path = '/channels', options: MarketingAppOptions = {}) {
  const mounted = await mountMarketing(path, options);
  wrapper = mounted.wrapper;
  return mounted;
}

async function openAccountMenu() {
  await wrapper!.get('header button[aria-haspopup="menu"]').trigger('click');
  await flushPromises();
  return new DOMWrapper(document.querySelector<HTMLElement>('[role="menu"]')!);
}

/** The requests the desk sent to end its Ocharo session. */
function signOutCalls() {
  return vi.mocked(fetch).mock.calls.filter(([input]) => String(input) === '/api/identity/sign-out');
}

/** The identity lines at the top of the open menu. */
function identityLines(menu: DOMWrapper<HTMLElement>) {
  return menu.findAll('[data-account-menu-identity] p').map((line) => line.text().trim());
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
  it('says a session without a name or contact is signed in, and ends with sign out', async () => {
    await renderSignedIn();

    const menu = await openAccountMenu();

    expect(menu.get('[data-account-menu-identity]').text()).toBe('Signed in to Ocharo');
    expect(wrapper!.get('header button[aria-haspopup="menu"]').attributes('aria-label')).toBe('Account menu');
    const rows = menu.findAll('[role="menuitem"]');
    expect(rows.map((item) => item.text().trim())).toEqual(['Sign out']);
    expect(menu.findAll('[role="menuitem"], [role="menuitemradio"]').at(-1)!.attributes()).toHaveProperty(
      'data-account-menu-sign-out',
    );
  });

  it('names the person with the Identity name and email, without the session fallback', async () => {
    await renderSignedIn('/channels', {
      session: {
        ...SignedIn,
        person: { ...SignedIn.person, name: 'Maftuna Rakhimova', email: 'maftuna@ocharo.example.test' },
      },
    });

    const menu = await openAccountMenu();

    expect(identityLines(menu)).toEqual(['Maftuna Rakhimova', 'maftuna@ocharo.example.test']);
    expect(menu.text()).not.toContain('Signed in to Ocharo');
    expect(wrapper!.get('header button[aria-haspopup="menu"]').attributes('aria-label')).toBe(
      'Account menu for Maftuna Rakhimova',
    );
  });

  it.each([
    ['only a name', { name: 'Maftuna Rakhimova' }, ['Maftuna Rakhimova']],
    ['only an email', { email: 'maftuna@ocharo.example.test' }, ['maftuna@ocharo.example.test']],
    [
      'a name and a phone number',
      { name: 'Maftuna Rakhimova', phoneNumber: '+998901234567' },
      ['Maftuna Rakhimova', '+998901234567'],
    ],
    [
      'a name that repeats the email',
      { name: 'maftuna@ocharo.example.test', email: 'maftuna@ocharo.example.test' },
      ['maftuna@ocharo.example.test'],
    ],
  ])('shows %s without the session fallback', async (_case, identity, lines) => {
    await renderSignedIn('/channels', { session: { ...SignedIn, person: { ...SignedIn.person, ...identity } } });

    const menu = await openAccountMenu();

    expect(identityLines(menu)).toEqual(lines);
  });

  it('signs out at the API with its CSRF token, then leaves for Identity to end the Ocharo sign-in', async () => {
    await renderSignedIn();
    const expired = vi.fn();
    const unsubscribe = subscribeIntegrationExpired(expired);

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(signOutCalls()).toHaveLength(1);
    const [, init] = signOutCalls()[0]!;
    expect(init).toMatchObject({ method: 'POST', credentials: 'same-origin' });
    expect(new Headers(init?.headers).get('X-Csrf-Token')).toBe('csrf-token');
    expect(navigations).toEqual([EndSessionUrl]);
    expect(integrationSession.value).toMatchObject({ active: false, canEdit: false });
    expect(integrationAccess.value).toBe(false);
    expect(expired).not.toHaveBeenCalled();
    expect(wrapper!.get('fieldset').attributes('disabled')).toBeDefined();
    expect(wrapper!.get('[aria-label="Ocharo connection"]').text()).toContain('Sign in to Ocharo');
    expect(wrapper!.get('[aria-label="Ocharo connection"] a').attributes('target')).toBe('_blank');
    await wrapper!.get('header button[aria-haspopup="menu"]').trigger('click');
    await flushPromises();
    expect(document.querySelector('[data-account-menu-sign-out]')).toBeNull();
    unsubscribe();
  });

  it('offers sign-in, in another tab, to a browser the API answers as signed out', async () => {
    await renderSignedIn('/channels', { session: { isAuthenticated: false } });
    const panel = wrapper!.get('[aria-label="Ocharo connection"]');

    expect(panel.text()).toContain('Reconnect to Ocharo');
    const signIn = panel.get('a');
    expect(signIn.text()).toBe('Sign in to Ocharo');
    expect(signIn.attributes('href')).toMatch(/^\/api\/identity\/sign-in\?returnUrl=%2F/);
    expect(signIn.attributes('target')).toBe('_blank');
    expect(signIn.attributes('rel')).toContain('noopener');
    expect(integrationAccess.value).toBe(false);
  });

  it('says a signed-in person without marketing:edit has read-only access', async () => {
    await renderSignedIn('/channels', { session: { ...SignedIn, permissions: [] } });

    expect(wrapper!.get('[aria-label="Ocharo connection"]').text()).toContain('You have read-only access.');
    expect(integrationAccess.value).toBe(true);
  });

  it('holds sign-out while a post has unsaved edits, and says why', async () => {
    await renderSignedIn('/posts/new');
    const notify = vi.spyOn(feedbackBus, 'notify');
    await wrapper!.get('main input[aria-label="Post title"]').setValue('Unsaved title');

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(signOutCalls()).toHaveLength(0);
    expect(integrationAccess.value).toBe(true);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Save or discard your unsaved changes before signing out.' }),
    );
    expect((wrapper!.get('main input[aria-label="Post title"]').element as HTMLInputElement).value).toBe(
      'Unsaved title',
    );
  });

  it.each([
    [503, 'Ocharo could not end this sign-in. Your work stays here; try again shortly.'],
    [400, 'This tab could not confirm its Ocharo sign-in. Reload the app, then sign out.'],
  ])('keeps the session and reports a display-safe reason when sign-out answers %i', async (status, reason) => {
    await renderSignedIn('/channels', { signOutStatus: status });
    const notify = vi.spyOn(feedbackBus, 'notify');

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(signOutCalls()).toHaveLength(1);
    expect(integrationAccess.value).toBe(true);
    expect(navigations).toEqual([]);
    expect(notify).toHaveBeenCalledWith(expect.objectContaining({ tone: 'danger', description: reason }));
  });
});
