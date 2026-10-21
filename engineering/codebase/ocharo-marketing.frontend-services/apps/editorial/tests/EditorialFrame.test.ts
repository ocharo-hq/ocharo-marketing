import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DOMWrapper, flushPromises, type VueWrapper } from '@vue/test-utils';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import {
  ApiVersion,
  blogApplication,
  mountEditorial,
  placeStub,
  type EditorialAppOptions,
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

/** Where this browser remembers the desktop sidebar's rail choice. */
const SidebarCollapsedKey = 'ocharo.editorial.sidebar-collapsed';
let wrapper: VueWrapper | undefined;

async function renderAt(path: string, options: EditorialAppOptions = {}) {
  const mounted = await mountEditorial(path, options);
  wrapper = mounted.wrapper;
  return mounted;
}

/** Waits for a click's navigation; the first visit to a code-split page loads its module first. */
async function arrive(router: { currentRoute: { value: { path: string } } }, path: string) {
  await vi.waitFor(() => expect(router.currentRoute.value.path).toBe(path));
  await flushPromises();
}

/** The sidebar's section list, in whichever language the interface shows. */
function sidebar() {
  return wrapper!.get('aside').get('nav');
}

/** An apps-menu link: the menu renders outside the main region, where the connection panel's Desk link sits. */
function appLink(href: string) {
  return [...document.querySelectorAll<HTMLAnchorElement>(`a[href="${href}"]`)].find((link) => !link.closest('main'));
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

describe('Ocharo frame in Editorial', () => {
  it.each([390, 1440])('keeps one reachable main landmark and the suite controls at %ipx', async (width) => {
    await renderAt('/topics', { width });

    const main = wrapper!.get('main');
    expect(wrapper!.findAll('main')).toHaveLength(1);
    expect(wrapper!.get(`a[href="#${main.attributes('id')}"]`).text()).toBe('Skip to main content');
    expect(main.findAll('h1')).toHaveLength(1);
    const header = wrapper!.get('header[role="banner"]');
    expect(header.find('button[aria-label="Ocharo apps — Editorial"]').exists()).toBe(true);
    expect(header.get('[title="App version"]').text()).toBe('dev');
    expect(header.get('a[href="/stories/new"]').text()).toContain('New story');
    const controls = header.findAll('button');
    expect(controls.at(-1)!.attributes()).toMatchObject({ 'aria-label': 'Account menu', 'aria-haspopup': 'menu' });
    expect(header.find('[aria-label="Interface language"]').exists()).toBe(false);
    expect(wrapper!.find('[aria-label="Ocharo platform"]').exists()).toBe(false);
    expect(wrapper!.find('[aria-label="Mobile sections"]').exists()).toBe(false);
  });

  it('links the apps menu to the Desk address Editorial serves at runtime, with Editorial current', async () => {
    await renderAt('/topics', { runtimeConfig: { OCHARO_DESK_URL: 'https://ocharo-desk-dev.example.ts.net/' } });

    await wrapper!.get('header button[aria-label="Ocharo apps — Editorial"]').trigger('click');
    await flushPromises();

    expect(document.querySelector('[aria-current="true"]')?.textContent).toContain('Editorial');
    expect(appLink('https://ocharo-desk-dev.example.ts.net/')?.textContent).toContain('Desk');
    expect(appLink('https://localhost:8264/')).toBeUndefined();
    expect(appLink('https://localhost:8358/')?.textContent).toContain('Marketing');
    expect(appLink('https://localhost:8284/')?.target).toBe('_blank');
  });

  it('names Editorial in the apps menu by its own key, as Desk does, in the chosen language', async () => {
    await renderAt('/topics');
    useUiLocale().setUiLocale('uz');
    await flushPromises();

    await wrapper!.get('header button[aria-label="Ocharo ilovalari — Tahririyat"]').trigger('click');
    await flushPromises();

    expect(document.querySelector('[aria-current="true"]')?.textContent).toContain('Tahririyat');
    expect(document.querySelector('[aria-current="true"]')?.textContent).toContain('Blog hikoyalari');
  });

  it('lists the sections and the public site in the sidebar, marks the open one and names the place', async () => {
    const { router } = await renderAt('/topics');

    const links = sidebar().findAll('a');
    expect(links.map((link) => link.text())).toEqual([
      'Stories',
      'Topics',
      'Site settings',
      'Open site (opens in a new tab)',
    ]);
    expect(links.map((link) => link.attributes('href'))).toEqual(['/', '/topics', '/settings', '/']);
    expect(links.at(-1)!.attributes('target')).toBe('_blank');
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Topics');
    expect(wrapper!.get('nav[aria-label="You are here"]').text()).toBe('Topics');

    await sidebar().get('a[href="/settings"]').trigger('click');
    await arrive(router, '/settings');
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Site settings');
    expect(document.title).toBe('Site settings · Ocharo Editorial');

    await router.push('/stories/story');
    await flushPromises();
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Stories');
    expect(wrapper!.get('nav[aria-label="You are here"]').text()).toBe('StoriesStory');
  });

  it('keeps the sidebar rail choice this browser stored, and stores a change under the same key', async () => {
    localStorage.setItem(SidebarCollapsedKey, 'true');
    await renderAt('/topics');

    expect(sidebar().get('a[href="/topics"]').attributes('title')).toBe('Topics');
    await wrapper!.get('button[aria-label="Expand sidebar"]').trigger('click');

    expect(wrapper!.find('button[aria-label="Collapse sidebar"]').exists()).toBe(true);
    expect(localStorage.getItem(SidebarCollapsedKey)).toBe('false');
  });

  it('swaps the sidebar for a bottom bar with More holding the public site on a phone', async () => {
    const { router } = await renderAt('/topics', { width: 390 });

    expect(wrapper!.find('aside').exists()).toBe(false);
    const bar = wrapper!.get('[aria-label="Editorial sections"]');
    expect(bar.findAll('a').map((link) => link.text())).toEqual(['Stories', 'Topics', 'Site settings']);
    expect(bar.get('[aria-current="page"]').text()).toBe('Topics');

    await bar.get('button[aria-haspopup="dialog"]').trigger('click');
    await flushPromises();
    const dialog = new DOMWrapper(document.querySelector<HTMLElement>('[role="dialog"]')!);
    expect(dialog.text()).toContain('More in Editorial');
    expect(dialog.get('a[target="_blank"]').attributes('href')).toBe('/');

    await bar.get('a[href="/settings"]').trigger('click');
    await arrive(router, '/settings');
    expect(bar.get('[aria-current="page"]').text()).toBe('Site settings');
  });

  it('reports the app and API versions and offers appearance and language without an Ocharo session', async () => {
    await renderAt('/topics');

    await wrapper!.get('header button[aria-label="Account menu"]').trigger('click');
    await flushPromises();

    const menu = new DOMWrapper(document.querySelector<HTMLElement>('[role="menu"]')!);
    const versions = menu
      .findAll('[data-account-menu-version]')
      .map((entry) => [...entry.element.children].map((part) => part.textContent?.trim()));
    expect(versions).toEqual([
      ['Editorial', 'dev'],
      ['API', `${ApiVersion.split('+')[0]} · 1a2b3c4`],
    ]);
    expect(menu.find('[data-account-menu-sign-out]').exists()).toBe(false);
    expect(menu.find('[data-account-menu-appearance]').exists()).toBe(true);
    expect(menu.findAll('[data-account-menu-languages] [role="menuitemradio"]')).toHaveLength(3);
  });

  it('still asks before a sidebar section change discards an unsaved story', async () => {
    const confirm = vi.fn(() => false);
    vi.stubGlobal('confirm', confirm);
    const { router } = await renderAt('/stories/story');
    app.current.editor.draft.value = { ...app.current.editor.draft.value, title: 'Unsaved title' };
    await flushPromises();

    await sidebar().get('a[href="/topics"]').trigger('click');
    await flushPromises();
    expect(confirm).toHaveBeenCalledTimes(1);
    expect(router.currentRoute.value.path).toBe('/stories/story');
    expect(app.current.editor.draft.value.title).toBe('Unsaved title');

    confirm.mockReturnValue(true);
    await sidebar().get('a[href="/topics"]').trigger('click');
    await arrive(router, '/topics');
  });
});
