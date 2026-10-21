import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DOMWrapper, flushPromises, type VueWrapper } from '@vue/test-utils';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { ApiVersion, mountMarketing, type MarketingAppOptions } from './helpers/MarketingApp';

/** Where this browser remembers the desktop sidebar's rail choice. */
const SidebarCollapsedKey = 'ocharo.marketing.sidebar-collapsed';
let wrapper: VueWrapper | undefined;

async function renderAt(path: string, options: MarketingAppOptions = {}) {
  const mounted = await mountMarketing(path, options);
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

/** The open popup menu or dialog, which the SDK renders outside the frame. */
function popup(selector: string) {
  const element = document.querySelector<HTMLElement>(selector);
  expect(element).not.toBeNull();
  return new DOMWrapper(element!);
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

describe('Ocharo frame in Marketing', () => {
  it.each([390, 1440])('keeps one reachable main landmark and the suite controls at %ipx', async (width) => {
    await renderAt('/channels', { width });

    const main = wrapper!.get('main');
    expect(wrapper!.findAll('main')).toHaveLength(1);
    expect(wrapper!.get(`a[href="#${main.attributes('id')}"]`).text()).toBe('Skip to main content');
    expect(main.attributes('tabindex')).toBe('-1');
    const header = wrapper!.get('header[role="banner"]');
    expect(header.find('button[aria-label="Ocharo apps — Marketing"]').exists()).toBe(true);
    expect(header.get('[title="App version"]').text()).toBe('dev');
    expect(header.find('button[aria-label="Refresh status"]').exists()).toBe(true);
    expect(header.text()).toContain('New post');
    const controls = header.findAll('button');
    expect(controls.at(-1)!.attributes()).toMatchObject({ 'aria-label': 'Account menu', 'aria-haspopup': 'menu' });
    expect(header.find('[aria-label="Interface language"]').exists()).toBe(false);
    // The old identity card and platform link list are gone; the apps menu replaces both.
    expect(wrapper!.text()).not.toContain('Maftuna');
    expect(wrapper!.find('[aria-label="Ocharo platform"]').exists()).toBe(false);
    expect(wrapper!.find('[aria-label="Mobile sections"]').exists()).toBe(false);
  });

  it.each([
    [390, true],
    [1440, false],
  ])('opens the Ocharo apps from the app button at %ipx, full screen only on a phone', async (width, fullScreen) => {
    await renderAt('/channels', { width });

    await wrapper!.get('header button[aria-label="Ocharo apps — Marketing"]').trigger('click');
    await flushPromises();

    expect(document.body.textContent).toContain('Ocharo apps');
    expect(document.querySelector('[aria-current="true"]')?.textContent).toContain('Marketing');
    expect(appLink('https://localhost:8264/')?.textContent).toContain('Desk');
    expect(appLink('https://localhost:8284/studio/')?.textContent).toContain('Editorial');
    expect(appLink('https://localhost:8284/atlas/')?.target).toBe('_blank');
    expect(document.querySelector('[data-phone-layout="fullscreen"]') !== null).toBe(fullScreen);
  });

  it('links the apps menu to the Desk address Marketing serves at runtime', async () => {
    await renderAt('/channels', { runtimeConfig: { OCHARO_DESK_URL: 'https://ocharo-desk-dev.example.ts.net/' } });

    await wrapper!.get('header button[aria-label="Ocharo apps — Marketing"]').trigger('click');
    await flushPromises();

    expect(appLink('https://ocharo-desk-dev.example.ts.net/')?.textContent).toContain('Desk');
    expect(appLink('https://localhost:8264/')).toBeUndefined();
    expect(appLink('https://localhost:8282/catalogues')?.textContent).toContain('Catalogue');
  });

  it('lists every section under Create, Plan, Results and Settings, marks the open one and names the place', async () => {
    await renderAt('/channels');

    const links = sidebar().findAll('a');
    expect(links.map((link) => link.text())).toEqual([
      'Today',
      'Posts',
      'Products',
      'Atlas',
      'Studio imports',
      'Calendar',
      'Automation',
      'Channels',
      'Enquiries',
      'Monitoring',
      'Results',
      'Connections',
    ]);
    expect(links.map((link) => link.attributes('href'))).toEqual([
      '/',
      '/posts',
      '/products',
      '/atlas',
      '/creative-imports',
      '/calendar',
      '/automation',
      '/channels',
      '/inquiries',
      '/monitoring',
      '/results',
      '/connections',
    ]);
    const headings = sidebar()
      .findAll('ul[aria-labelledby]')
      .map((list) => document.getElementById(list.attributes('aria-labelledby')!)?.textContent?.trim());
    expect(headings).toEqual(['Create', 'Plan', 'Results', 'Settings']);
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Channels');
    expect(wrapper!.get('nav[aria-label="You are here"]').text()).toBe('PlanChannels');
  });

  it('moves between sections, names the document and keeps the composer under Posts', async () => {
    const { router } = await renderAt('/channels');

    await sidebar().get('a[href="/connections"]').trigger('click');
    await arrive(router, '/connections');
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Connections');
    expect(wrapper!.get('nav[aria-label="You are here"]').text()).toBe('SettingsConnections');
    expect(document.title).toBe('Connections · Ocharo Marketing');

    await router.push('/posts/new');
    await flushPromises();
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Posts');
    expect(document.title).toBe('New post · Ocharo Marketing');
    expect(wrapper!.get('nav[aria-label="You are here"]').text()).toBe('CreatePostsNew post');
  });

  it('renames the sections and the place in the chosen interface language', async () => {
    await renderAt('/channels');

    useUiLocale().setUiLocale('uz');
    await flushPromises();

    expect(sidebar().get('[aria-current="page"]').text()).toBe('Kanallar');
    expect(sidebar().text()).toContain('Reja');
    expect(document.title).toBe('Kanallar · Ocharo Marketing');
  });

  it('counts saved drafts beside Posts and hides the count in the rail', async () => {
    await renderAt('/channels', { drafts: 3 });

    expect(sidebar().get('a[href="/posts"]').text()).toBe('Posts3');

    await wrapper!.get('button[aria-label="Collapse sidebar"]').trigger('click');
    expect(sidebar().get('a[href="/posts"]').text()).toBe('Posts');
  });

  it('keeps the sidebar rail choice this browser stored, and stores a change under the same key', async () => {
    localStorage.setItem(SidebarCollapsedKey, 'true');
    await renderAt('/channels');

    expect(sidebar().get('a[href="/channels"]').attributes('title')).toBe('Channels');
    await wrapper!.get('button[aria-label="Expand sidebar"]').trigger('click');

    expect(wrapper!.find('button[aria-label="Collapse sidebar"]').exists()).toBe(true);
    expect(localStorage.getItem(SidebarCollapsedKey)).toBe('false');
  });

  it('shows the icon rail without a collapse control on a tablet', async () => {
    await renderAt('/channels', { width: 900 });

    expect(sidebar().text()).toContain('Channels');
    expect(wrapper!.find('button[aria-label="Expand sidebar"]').exists()).toBe(false);
    expect(wrapper!.find('button[aria-label="Collapse sidebar"]').exists()).toBe(false);
  });

  it('swaps the sidebar for a bottom bar of four sections and More on a phone', async () => {
    const { router } = await renderAt('/', { width: 390 });

    expect(wrapper!.find('aside').exists()).toBe(false);
    const bar = wrapper!.get('[aria-label="Marketing sections"]');
    // The SDK glass variant paints the fill, edge and upward shadow; the solid page fill would hide the glass.
    expect(bar.classes()).toEqual(
      expect.arrayContaining(['surface-glass', 'border-glass-edge', 'shadow-(--glass-bottom-shadow)', 'md:hidden']),
    );
    expect(bar.classes()).not.toContain('bg-background');
    expect(bar.findAll('a').map((link) => link.text())).toEqual(['Today', 'Posts', 'Products', 'Atlas']);
    expect(bar.get('[aria-current="page"]').text()).toBe('Today');

    await bar.get('button[aria-haspopup="dialog"]').trigger('click');
    await flushPromises();
    const dialog = popup('[role="dialog"]');
    expect(dialog.text()).toContain('More in Marketing');
    expect(dialog.findAll('a').map((link) => link.text())).toEqual([
      'Studio imports',
      'Calendar',
      'Automation',
      'Channels',
      'Enquiries',
      'Monitoring',
      'Results',
      'Connections',
    ]);
    await dialog.get('a[href="/connections"]').trigger('click');
    await arrive(router, '/connections');

    expect(document.querySelector('[role="dialog"]')).toBeNull();
    expect(
      wrapper!.get('[aria-label="Marketing sections"]').get('button[aria-haspopup="dialog"]').attributes(),
    ).toHaveProperty('aria-current', 'page');
  });

  it('reports the app and API versions in the account menu, with no sign-out outside an Ocharo session', async () => {
    await renderAt('/channels');

    await wrapper!.get('header button[aria-label="Account menu"]').trigger('click');
    await flushPromises();

    const menu = popup('[role="menu"]');
    const versions = menu
      .findAll('[data-account-menu-version]')
      .map((entry) => [...entry.element.children].map((part) => part.textContent?.trim()));
    expect(versions).toEqual([
      ['Marketing', 'dev'],
      ['API', `${ApiVersion.split('+')[0]} · 8f2c1d9`],
    ]);
    expect(fetch).toHaveBeenCalledWith('/api/system/status', expect.objectContaining({ credentials: 'omit' }));
    expect(menu.find('[data-account-menu-sign-out]').exists()).toBe(false);
    expect(menu.find('[data-account-menu-identity]').exists()).toBe(false);
    expect(menu.find('[data-account-menu-appearance]').exists()).toBe(true);
    expect(menu.findAll('[data-account-menu-languages] [role="menuitemradio"]').map((row) => row.text())).toEqual([
      'O‘zbekcha',
      'Русский',
      'English',
    ]);
  });

  it('switches the interface language and the colour mode from the account menu', async () => {
    await renderAt('/channels');

    await wrapper!.get('header button[aria-label="Account menu"]').trigger('click');
    await flushPromises();
    popup('[role="menuitemradio"][lang="ru"]').element.click();
    await flushPromises();

    expect(useUiLocale().uiLocale.value).toBe('ru');
    expect(sidebar().get('[aria-current="page"]').text()).toBe('Каналы');

    await wrapper!.get('header button[aria-haspopup="menu"]').trigger('click');
    await flushPromises();
    const dark = popup('[data-account-menu-appearance]')
      .findAll('[role="menuitemradio"]')
      .find((row) => row.text() === 'Тёмная')!;
    await dark.trigger('click');
    await flushPromises();
    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(localStorage.getItem('ocharo.marketing.theme')).toBe('dark');
  });

  it('still asks before a sidebar section change discards an unsaved post', async () => {
    const { router } = await renderAt('/posts/new');
    const title = wrapper!.get('main input[aria-label="Post title"]');
    await title.setValue('Unsaved title');

    await sidebar().get('a[href="/calendar"]').trigger('click');
    await flushPromises();

    const guard = popup('[role="dialog"]');
    expect(guard.text()).toContain('Unsaved changes');
    expect(router.currentRoute.value.path).toBe('/posts/new');
    guard
      .findAll('button')
      .find((button) => button.text() === 'Continue editing')!
      .element.click();
    await flushPromises();
    expect(router.currentRoute.value.path).toBe('/posts/new');
    expect((wrapper!.get('main input[aria-label="Post title"]').element as HTMLInputElement).value).toBe(
      'Unsaved title',
    );

    await sidebar().get('a[href="/calendar"]').trigger('click');
    await flushPromises();
    popup('[role="dialog"]')
      .findAll('button')
      .find((button) => button.text() === 'Leave without saving')!
      .element.click();
    await arrive(router, '/calendar');
  });
});
