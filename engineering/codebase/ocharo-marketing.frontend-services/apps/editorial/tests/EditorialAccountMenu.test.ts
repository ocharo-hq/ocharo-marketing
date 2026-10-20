import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DOMWrapper, flushPromises, type VueWrapper } from '@vue/test-utils';
import { integrationAccess, subscribeIntegrationExpired } from '@ocharomarketing/connect';
import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { blogApplication, mountEditorial, placeStub, SignedIn } from './helpers/EditorialApp';

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

/** The requests Editorial sent to end its Desk grant. */
function logoutCalls() {
  return vi.mocked(fetch).mock.calls.filter(([input]) => String(input) === '/api/sso/logout');
}

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
  it('names the session, opens the Ocharo account in a new tab and signs out with the CSRF token', async () => {
    wrapper = (await mountEditorial('/topics', { ssoSession: SignedIn })).wrapper;
    const open = vi.fn();
    vi.stubGlobal('open', open);
    const expired = vi.fn();
    const unsubscribe = subscribeIntegrationExpired(expired);

    let menu = await openAccountMenu();
    expect(menu.get('[data-account-menu-identity]').text()).toBe('Signed in through Ocharo Desk');
    const rows = menu.findAll('[role="menuitem"]');
    expect(rows.map((item) => item.text().trim())).toEqual(['Ocharo account', 'Sign out']);
    await rows[0]!.trigger('click');
    expect(open).toHaveBeenCalledWith('https://desk.example.test/', '_blank', 'noopener,noreferrer');

    menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(logoutCalls()).toHaveLength(1);
    const [, init] = logoutCalls()[0]!;
    expect(init).toMatchObject({ method: 'POST', credentials: 'same-origin' });
    expect(new Headers(init?.headers).get('X-CSRF-Token')).toBe('csrf-token');
    expect(integrationAccess.value).toBe(false);
    expect(expired).not.toHaveBeenCalled();
    expect(wrapper.get('fieldset').attributes('disabled')).toBeDefined();
    expect(wrapper.get('main h1').text()).toBe('Topics');
    unsubscribe();
  });

  it('holds sign-out while the story has unsaved edits, and says why', async () => {
    wrapper = (await mountEditorial('/stories/story', { ssoSession: SignedIn })).wrapper;
    const notify = vi.spyOn(feedbackBus, 'notify');
    app.current.editor.draft.value = { ...app.current.editor.draft.value, title: 'Unsaved title' };
    await flushPromises();

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(logoutCalls()).toHaveLength(0);
    expect(integrationAccess.value).toBe(true);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Save or discard your unsaved changes before signing out.' }),
    );
  });

  it('holds sign-out while a save is in flight', async () => {
    wrapper = (await mountEditorial('/stories/story', { ssoSession: SignedIn })).wrapper;
    app.current.editor.busy.value = true;
    await flushPromises();

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(logoutCalls()).toHaveLength(0);
  });

  it('keeps the session and reports a display-safe reason when Desk cannot end the sign-in', async () => {
    wrapper = (await mountEditorial('/topics', { ssoSession: SignedIn, logoutStatus: 503 })).wrapper;
    const notify = vi.spyOn(feedbackBus, 'notify');

    const menu = await openAccountMenu();
    await menu.get('[data-account-menu-sign-out]').trigger('click');
    await flushPromises();

    expect(integrationAccess.value).toBe(true);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({
        tone: 'danger',
        description: 'Ocharo Desk could not end this sign-in. Your work stays here; try again shortly.',
      }),
    );
  });
});
