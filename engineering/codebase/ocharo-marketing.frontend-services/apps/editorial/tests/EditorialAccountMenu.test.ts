import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DOMWrapper, flushPromises, type VueWrapper } from '@vue/test-utils';
import { integrationAccess, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import {
  blogApplication,
  EndSessionUrl,
  mountEditorial,
  navigations,
  placeStub,
  SignedIn,
} from './helpers/EditorialApp';

const app = vi.hoisted(() => ({ current: undefined as unknown as ReturnType<typeof blogApplication> }));
vi.mock('../src/application/blog', () => ({
  useBlogStore: () => app.current.store,
  useArticleEditor: () => app.current.editor,
  loadStoryOptions: () => app.current.loadStoryOptions(),
}));
vi.mock('../src/presentation/blog/StoriesPage.vue', () => ({ default: placeStub('StoriesPage', 'Stories') }));
vi.mock('../src/presentation/blog/TopicsPage.vue', () => ({ default: placeStub('TopicsPage', 'Topics') }));
vi.mock('../src/presentation/blog/SettingsPage.vue', () => ({ default: placeStub('SettingsPage', 'Site') }));

let wrapper: VueWrapper | undefined;

async function openAccountMenu() {
  await wrapper!.get('header button[aria-haspopup="menu"]').trigger('click');
  await flushPromises();
  return new DOMWrapper(document.querySelector<HTMLElement>('[role="menu"]')!);
}

/** The requests Editorial sent to end its Ocharo session. */
function signOutCalls() {
  return vi.mocked(fetch).mock.calls.filter(([input]) => String(input) === '/api/identity/sign-out');
}

/** The identity lines at the top of the open menu. */
function identityLines(menu: DOMWrapper<HTMLElement>) {
  return menu.findAll('[data-account-menu-identity] p').map((line) => line.text().trim());
}

/** What the session route answers for a person Identity names as `person`. */
const signedInAs = (person: Record<string, string | null>) => ({
  ...SignedIn,
  person: { ...SignedIn.person, ...person },
});

beforeEach(() => {
  app.current = blogApplication();
  localStorage.clear();
  useUiLocale().setUiLocale('en');
});
afterEach(() => {
  wrapper?.unmount();
  wrapper = undefined;
  document.body.innerHTML = '';
  vi.unstubAllGlobals();
});

describe('Editorial account menu with an Ocharo session', () => {
  it('names the session without a name or contact and signs out with the CSRF token, then leaves for Identity', async () => {
    wrapper = (await mountEditorial('/topics')).wrapper;
    const expired = vi.fn();
    const unsubscribe = subscribeIntegrationExpired(expired);

    const menu = await openAccountMenu();
    expect(menu.get('[data-account-menu-identity]').text()).toBe('Signed in to Ocharo');
    const rows = menu.findAll('[role="menuitem"]');
    expect(rows.map((item) => item.text().trim())).toEqual(['Sign out']);

    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(signOutCalls()).toHaveLength(1);
    const [, init] = signOutCalls()[0]!;
    expect(init).toMatchObject({ method: 'POST', credentials: 'same-origin' });
    expect(new Headers(init?.headers).get('X-Csrf-Token')).toBe('csrf-token');
    expect(navigations).toEqual([EndSessionUrl]);
    expect(integrationAccess.value).toBe(false);
    expect(expired).not.toHaveBeenCalled();
    expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined();
    expect(wrapper.get('main h1').text()).toBe('Topics');
    unsubscribe();
  });

  it('names the person with the Identity name and email, without the session fallback', async () => {
    wrapper = (
      await mountEditorial('/topics', {
        session: signedInAs({ name: 'Maftuna Rakhimova', email: 'maftuna@ocharo.example.test' }),
      })
    ).wrapper;

    const menu = await openAccountMenu();

    expect(identityLines(menu)).toEqual(['Maftuna Rakhimova', 'maftuna@ocharo.example.test']);
    expect(menu.text()).not.toContain('Signed in to Ocharo');
    expect(wrapper.get('header button[aria-haspopup="menu"]').attributes('aria-label')).toBe(
      'Account menu for Maftuna Rakhimova',
    );
  });

  it('shows only the email when Identity sends no name', async () => {
    wrapper = (
      await mountEditorial('/topics', { session: signedInAs({ name: null, email: 'maftuna@ocharo.example.test' }) })
    ).wrapper;

    const menu = await openAccountMenu();

    expect(identityLines(menu)).toEqual(['maftuna@ocharo.example.test']);
  });

  it('offers sign-in, in another tab, to a browser the API answers as signed out', async () => {
    wrapper = (await mountEditorial('/topics', { session: { isAuthenticated: false } })).wrapper;

    const signIn = wrapper.get('[aria-label="Ocharo connection"] a');
    expect(signIn.text()).toBe('Sign in to Ocharo');
    expect(signIn.attributes('href')).toMatch(/^\/api\/identity\/sign-in\?returnUrl=%2F/);
    expect(signIn.attributes('target')).toBe('_blank');
    expect(integrationAccess.value).toBe(false);
    expect(wrapper.find('header a[href="/stories/new"]').exists()).toBe(false);
  });

  it('says a signed-in person without editorial:edit has read-only access', async () => {
    wrapper = (await mountEditorial('/topics', { session: { ...SignedIn, permissions: [] } })).wrapper;

    expect(wrapper.get('[aria-label="Ocharo connection"]').text()).toContain('You have read-only access.');
    expect(integrationAccess.value).toBe(true);
  });

  it('holds sign-out while the story has unsaved edits, and says why', async () => {
    wrapper = (await mountEditorial('/stories/story')).wrapper;
    const notify = vi.spyOn(feedbackBus, 'notify');
    app.current.editor.draft.value = { ...app.current.editor.draft.value, title: 'Unsaved title' };
    await flushPromises();

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(signOutCalls()).toHaveLength(0);
    expect(integrationAccess.value).toBe(true);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Save or discard your unsaved changes before signing out.' }),
    );
  });

  it('holds sign-out while a save is in flight', async () => {
    wrapper = (await mountEditorial('/stories/story')).wrapper;
    app.current.editor.busy.value = true;
    await flushPromises();

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(signOutCalls()).toHaveLength(0);
  });

  it('keeps the session and reports a display-safe reason when Identity cannot end the sign-in', async () => {
    wrapper = (await mountEditorial('/topics', { signOutStatus: 503 })).wrapper;
    const notify = vi.spyOn(feedbackBus, 'notify');

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(integrationAccess.value).toBe(true);
    expect(navigations).toEqual([]);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({
        tone: 'danger',
        description: 'Ocharo could not end this sign-in. Your work stays here; try again shortly.',
      }),
    );
  });
});
