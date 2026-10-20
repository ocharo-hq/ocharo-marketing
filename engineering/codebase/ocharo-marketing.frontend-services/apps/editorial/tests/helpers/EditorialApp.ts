import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { vi } from 'vitest';
import { computed, defineComponent, h, ref, type Plugin } from 'vue';
import { importMetaEnvSource } from '@wow-two-beta/fe/foundation/config';
import { RouterHistoryMode } from '@wow-two-beta/fe/router';
import { refreshIntegration } from '@ocharomarketing/connect';
import { loadOcharoApps, ocharoAppsPlugin } from '@ocharomarketing/connect/OcharoAppsConfig';

import AppRoot from '../../src/bootstrap/AppRoot.vue';
import { createBlogRouter } from '../../src/bootstrap/BlogRouter';
import { emptyArticle } from '../../src/domain/blog/BlogRules';

/** The informational version the stubbed `/api/system/status` reports: a release and the commit it was built from. */
export const ApiVersion = '0.2.0+1a2b3c4d5e6f';

/** One signed-in Ocharo session, the same identity in every test, so the identity-change rule never trips. */
export const SignedIn = {
  enabled: true,
  authenticated: true,
  userId: 'editor-1',
  organizationId: 'ocharo',
  canEdit: true,
  canApprove: true,
  csrfToken: 'csrf-token',
  accountUrl: 'https://desk.example.test/',
};

/** A list page that names itself, standing in for the real stories, topics and settings pages. */
export function placeStub(name: string, heading: string) {
  return defineComponent({ name, setup: () => () => h('h1', heading) });
}

/** The application layer the frame and the story editor read, with one saved story and nothing else. */
export function blogApplication() {
  const document = { ...emptyArticle('pieces'), title: 'Saved story', slug: 'saved-story', status: 'draft' };
  const draft = ref(structuredClone(document));
  const baseline = JSON.stringify(document);
  const editor = {
    draft,
    dirty: computed(() => JSON.stringify(draft.value) !== baseline),
    busy: ref(false),
    save: vi.fn(),
    detail: ref({ id: 'story', revision: 1, document }),
    canEdit: ref(true),
    loading: ref(false),
    history: ref([]),
    media: ref([]),
    products: ref([]),
    stats: ref({ words: 0, minutes: 0 }),
    blockers: ref([]),
    previewNonce: ref(0),
    load: async () => {},
    loadLibraries: async () => {},
    startNew: vi.fn(),
  };
  const store = {
    error: ref(null),
    notice: ref(null),
    topics: ref([{ key: 'pieces', short: 'Pieces' }]),
    loadTopics: vi.fn(async () => {}),
    // The attached preview iframe must not load a page in the test DOM.
    links: { preview: () => 'about:blank', media: (value: string) => `/media/${value}/preview` },
  };
  return { editor, store, loadStoryOptions: async () => [] };
}

/** Answers every `min-width` media query for a viewport `width` wide; colour-scheme queries read as light. */
export function stubViewport(width: number): void {
  vi.stubGlobal('matchMedia', (query: string) => ({
    matches: width >= Number(query.match(/min-width:\s*(\d+)px/)?.[1] ?? Infinity),
    media: query,
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }));
}

export interface EditorialAppOptions {
  /** The viewport width; default a 1440px desktop. */
  readonly width?: number;
  /** When given, `/api/runtime-config` serves it and the app menu resolves it the way startup does. */
  readonly runtimeConfig?: Record<string, string>;
  /** The session `/api/sso/session` serves; default SSO disabled, so the local integration session applies. */
  readonly ssoSession?: Record<string, unknown>;
  /** The status `POST /api/sso/logout` answers; default 204. */
  readonly logoutStatus?: number;
}

/**
 * Mounts the Studio — the real route table under the frame, with the stand-in list pages the caller mocks — at
 * `path`, the way startup does: the session is read first, then the app mounts. Unknown API reads answer 404.
 */
export async function mountEditorial(
  path: string,
  options: EditorialAppOptions = {},
): Promise<{ wrapper: VueWrapper; router: ReturnType<typeof createBlogRouter> }> {
  stubViewport(options.width ?? 1440);
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input instanceof Request ? input.url : input);
      const pathname = new URL(url, 'https://localhost').pathname;
      if (pathname === '/api/runtime-config') return Response.json(options.runtimeConfig ?? {});
      if (pathname === '/api/system/status')
        return Response.json({ service: 'OcharoBlog', status: 'healthy', version: ApiVersion });
      if (pathname === '/api/sso/session')
        return Response.json({ data: options.ssoSession ?? { enabled: false, authenticated: false } });
      if (pathname === '/api/integrations/session')
        return Response.json({ mode: 'local', active: true, configured: true, canEdit: true, canApprove: true });
      if (pathname === '/api/sso/logout') return new Response(null, { status: options.logoutStatus ?? 204 });
      return Response.json({ title: 'Not found' }, { status: 404 });
    }),
  );
  const router = createBlogRouter({ history: RouterHistoryMode.Memory });
  await router.push(path);
  await router.isReady();
  await refreshIntegration();
  const plugins: Plugin[] = [router];
  if (options.runtimeConfig) plugins.push(ocharoAppsPlugin(await loadOcharoApps(importMetaEnvSource(import.meta.env))));
  const wrapper = mount(AppRoot, {
    attachTo: document.body,
    global: {
      plugins,
      stubs: { SectionTextEditor: true, CoverImageEditor: true, ImageEditor: true },
    },
  });
  await flushPromises();
  return { wrapper, router };
}
