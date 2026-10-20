import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { defineComponent, h } from 'vue';
import { ConfigError, importMetaEnvSource, staticSource } from '@wow-two-beta/fe/foundation/config';
import { feedbackBus } from '@ocharomarketing/connect/Feedback';
import { ocharoApps } from '@ocharomarketing/connect/OcharoApps';
import {
  OcharoAppsProblem,
  loadOcharoApps,
  ocharoAppsPlugin,
  reportOcharoAppsStartup,
  resolveOcharoAppsConfig,
  runtimeConfigSource,
  useOcharoApps,
  usePlatformUrl,
} from '@ocharomarketing/connect/OcharoAppsConfig';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';

/** The local-run addresses both apps fall back to when nothing configures an app; the app keys match Desk's. */
const LocalDefaults = {
  OCHARO_DESK_URL: 'https://localhost:8264/',
  OCHARO_CATALOGUE_URL: 'https://localhost:8282/catalogues',
  OCHARO_STUDIO_URL: 'https://localhost:8262/studio',
  OCHARO_MARKETING_URL: 'https://localhost:8358/',
  OCHARO_EDITORIAL_URL: 'https://localhost:8284/studio/',
  OCHARO_BRAND_URL: 'https://localhost:8288/',
  OCHARO_ATLAS_URL: 'https://localhost:8284/atlas/',
  OCHARO_WEAR_URL: 'https://localhost:8284/wear/',
  OCHARO_BLOG_URL: 'https://localhost:8284/',
  PLATFORM_URL: 'https://localhost:8264/',
};

/** The app side of the seam: only an app module can hand its `import.meta.env` over. */
const buildTime = () => importMetaEnvSource(import.meta.env);

/** Answers `/api/runtime-config` with `body`, and records every request. */
function serveRuntimeConfig(body: unknown | (() => Promise<Response>)) {
  const fetch = vi
    .fn()
    .mockImplementation(() =>
      typeof body === 'function' ? (body as () => Promise<Response>)() : Promise.resolve(Response.json(body)),
    );
  vi.stubGlobal('fetch', fetch);
  return fetch;
}

/** Reads the seam's composables inside a component, with or without the startup plugin. */
function withLinks(plugin?: ReturnType<typeof ocharoAppsPlugin>) {
  let result: { apps: ReturnType<typeof useOcharoApps>; platformUrl: string } | undefined;
  const probe = defineComponent({
    setup() {
      result = { apps: useOcharoApps(), platformUrl: usePlatformUrl() };
      return () => h('div');
    },
  });
  mount(probe, { global: { plugins: plugin ? [plugin] : [] } }).unmount();
  return result!;
}

beforeEach(() => useUiLocale().setUiLocale('en'));
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

describe('Ocharo app addresses', () => {
  it('uses the local-run defaults when no source configures an app', () => {
    expect({ ...resolveOcharoAppsConfig([]) }).toEqual(LocalDefaults);
  });

  it('lets a runtime value override the build-time value of the same key', () => {
    const config = resolveOcharoAppsConfig([
      runtimeConfigSource({ OCHARO_DESK_URL: 'https://desk.example.test/' }),
      staticSource({
        VITE_OCHARO_DESK_URL: 'https://build.example.test/',
        VITE_OCHARO_BLOG_URL: 'https://blog.example.test/',
      }),
    ]);

    expect(config.OCHARO_DESK_URL).toBe('https://desk.example.test/');
    expect(config.OCHARO_BLOG_URL).toBe('https://blog.example.test/');
    expect(config.OCHARO_CATALOGUE_URL).toBe(LocalDefaults.OCHARO_CATALOGUE_URL);
  });

  it('reads the VITE_OCHARO_* and VITE_PLATFORM_URL build variables the app hands over', () => {
    vi.stubEnv('VITE_OCHARO_WEAR_URL', 'https://wear.example.test/');
    vi.stubEnv('VITE_PLATFORM_URL', 'https://platform.example.test/');

    const config = resolveOcharoAppsConfig([buildTime()]);

    expect(config.OCHARO_WEAR_URL).toBe('https://wear.example.test/');
    expect(config.PLATFORM_URL).toBe('https://platform.example.test/');
  });

  it('rejects a malformed runtime value instead of falling back to the build-time one', () => {
    const resolve = () =>
      resolveOcharoAppsConfig([
        runtimeConfigSource({ OCHARO_DESK_URL: 'not a link' }),
        staticSource({ VITE_OCHARO_DESK_URL: 'https://build.example.test/' }),
      ]);

    expect(resolve).toThrow(ConfigError);
    try {
      resolve();
    } catch (error) {
      expect((error as ConfigError).issues).toEqual([
        expect.objectContaining({ key: 'OCHARO_DESK_URL', lookupKey: 'VITE_OCHARO_DESK_URL', reason: 'invalid' }),
      ]);
    }
  });

  it('keeps the menu to plain HTTPS links even when a configured value is a well-formed URL', () => {
    const config = resolveOcharoAppsConfig([
      runtimeConfigSource({
        OCHARO_STUDIO_URL: 'http://studio.example.test/',
        OCHARO_BRAND_URL: 'https://brand.example.test/?token=x',
      }),
    ]);
    const keys = ocharoApps({
      studio: config.OCHARO_STUDIO_URL,
      brand: config.OCHARO_BRAND_URL,
      desk: config.OCHARO_DESK_URL,
    }).map((app) => app.key);

    expect(keys).toEqual(['desk']);
  });

  it('lists the same apps, labels and summaries as Desk, in Desk’s order', () => {
    const apps = ocharoApps(
      Object.fromEntries(
        ['desk', 'catalogue', 'studio', 'marketing', 'editorial', 'brand', 'atlas', 'wear', 'blog'].map((key) => [
          key,
          `https://${key}.example.test/`,
        ]),
      ),
    );

    expect(apps.map(({ key, label, summary, kind }) => [key, label, summary, kind])).toEqual([
      ['desk', 'Desk', 'Events, sourcing and products', 'workspace'],
      ['catalogue', 'Catalogue', 'Products, prices and editions', 'workspace'],
      ['studio', 'Studio', 'Garments and fabric', 'workspace'],
      ['marketing', 'Marketing', 'Posts and leads', 'workspace'],
      ['editorial', 'Editorial', 'Blog stories', 'workspace'],
      ['brand', 'Brand', 'Logo and brand guide', 'workspace'],
      ['atlas', 'Atlas', 'Public site', 'public-site'],
      ['wear', 'Wear', 'Public site', 'public-site'],
      ['blog', 'Blog', 'Public site', 'public-site'],
    ]);
  });
});

describe('startup resolution', () => {
  it('reads the public settings once, anonymously, and applies them ahead of the build values and defaults', async () => {
    const fetch = serveRuntimeConfig({ OCHARO_DESK_URL: 'https://desk.example.test/', UNRELATED: 'ignored' });
    vi.stubEnv('VITE_OCHARO_DESK_URL', 'https://build.example.test/');

    const startup = await loadOcharoApps(buildTime());

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(fetch).toHaveBeenCalledWith(
      '/api/runtime-config',
      expect.objectContaining({ method: 'GET', credentials: 'omit' }),
    );
    expect(startup.problems).toEqual([]);
    expect(startup.addresses).toMatchObject({
      desk: 'https://desk.example.test/',
      catalogue: LocalDefaults.OCHARO_CATALOGUE_URL,
    });
    expect(startup.platformUrl).toBe(LocalDefaults.PLATFORM_URL);
    expect(ocharoApps(startup.addresses)).toHaveLength(9);
  });

  it.each([
    ['offline', () => Promise.reject(new TypeError('offline'))],
    ['server error', () => Promise.resolve(Response.json({ title: 'Unavailable' }, { status: 503 }))],
    ['sign-in required', () => Promise.resolve(Response.json({ title: 'Sign in' }, { status: 401 }))],
    [
      'HTML fallback',
      () => Promise.resolve(new Response('<html></html>', { headers: { 'Content-Type': 'text/html' } })),
    ],
    ['non-object body', () => Promise.resolve(Response.json(['https://desk.example.test/']))],
  ])('continues with the build-time values and reports an unreadable response (%s)', async (_label, respond) => {
    serveRuntimeConfig(respond);
    vi.stubEnv('VITE_OCHARO_DESK_URL', 'https://build.example.test/');

    const startup = await loadOcharoApps(buildTime());

    expect(startup.addresses.desk).toBe('https://build.example.test/');
    expect(startup.problems.map((problem) => problem.message)).toEqual([OcharoAppsProblem.Unavailable]);
  });

  it('leaves out an app whose served value is malformed and reports it, keeping every other address', async () => {
    serveRuntimeConfig({
      OCHARO_DESK_URL: 'not a link',
      OCHARO_ATLAS_URL: 42,
      OCHARO_BLOG_URL: 'https://blog.example.test/',
    });
    vi.stubEnv('VITE_OCHARO_DESK_URL', 'https://build.example.test/');

    const startup = await loadOcharoApps(buildTime());

    expect(startup.addresses.desk).toBeUndefined();
    expect(startup.addresses.atlas).toBeUndefined();
    expect(startup.addresses.blog).toBe('https://blog.example.test/');
    expect(startup.addresses.catalogue).toBe(LocalDefaults.OCHARO_CATALOGUE_URL);
    expect(startup.problems.map((problem) => problem.message)).toEqual([OcharoAppsProblem.Rejected]);
    expect(startup.problems[0]?.detail).toContain('VITE_OCHARO_DESK_URL');
  });

  it('reports each problem once through the app feedback, in the operator language, without blocking', () => {
    const notify = vi.spyOn(feedbackBus, 'notify');
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    useUiLocale().setUiLocale('ru');

    reportOcharoAppsStartup({
      addresses: {},
      platformUrl: LocalDefaults.PLATFORM_URL,
      problems: [{ message: OcharoAppsProblem.Unavailable, detail: 'GET /api/runtime-config failed: timeout.' }],
    });

    expect(notify).toHaveBeenCalledTimes(1);
    expect(notify).toHaveBeenCalledWith(
      expect.objectContaining({
        tone: 'danger',
        description:
          'Не удалось загрузить ссылки на приложения Ocharo, поэтому меню приложений использует встроенные адреса.',
      }),
    );
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('timeout'));
  });

  it('hands the startup links to every component, and the local defaults where startup provided none', async () => {
    serveRuntimeConfig({ OCHARO_DESK_URL: 'https://desk.example.test/' });
    vi.stubEnv('VITE_PLATFORM_URL', 'https://platform.example.test/');
    const provided = withLinks(ocharoAppsPlugin(await loadOcharoApps(buildTime())));
    const fallback = withLinks();

    expect(provided.apps.find((app) => app.key === 'desk')?.url).toBe('https://desk.example.test/');
    expect(provided.platformUrl).toBe('https://platform.example.test/');
    expect(fallback.apps.find((app) => app.key === 'desk')?.url).toBe(LocalDefaults.OCHARO_DESK_URL);
    expect(fallback.platformUrl).toBe(LocalDefaults.PLATFORM_URL);
  });

  it('keeps the connection panel on the local Desk when the build sets a non-HTTPS platform address', async () => {
    serveRuntimeConfig({});
    vi.stubEnv('VITE_PLATFORM_URL', 'http://platform.example.test/');

    const { platformUrl } = withLinks(ocharoAppsPlugin(await loadOcharoApps(buildTime())));

    expect(platformUrl).toBe(LocalDefaults.PLATFORM_URL);
  });
});
