import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { vi } from 'vitest';
import type { Plugin } from 'vue';
import { importMetaEnvSource } from '@wow-two-beta/fe/foundation/config';
import { ResultExtensions } from '@wow-two-beta/fe/foundation/results';
import { RouterHistoryMode } from '@wow-two-beta/fe/router';
import { configureIntegration, refreshIntegration } from '@ocharomarketing/connect';
import { loadOcharoApps, ocharoAppsPlugin } from '@ocharomarketing/connect/OcharoAppsConfig';

import { createMarketingWorkspace, MarketingWorkspaceKey } from '../../src/application/common';
import AppRoot from '../../src/bootstrap/AppRoot.vue';
import { createMarketingRouter } from '../../src/bootstrap/MarketingRouter';
import type { WorkspaceModel } from '../../src/domain/marketing';
import { marketingApi } from '../../src/integration/common';

/** The informational version the stubbed `/api/system/status` reports: a release and the commit it was built from. */
export const ApiVersion = '0.1.4+8f2c1d9e4b7a';

/**
 * What `GET /api/identity/session` answers for one signed-in person, the same in every test, so the
 * identity-change rule never trips.
 */
export const SignedIn = {
  isAuthenticated: true,
  person: { sub: 'operator-1', name: null, picture: null, email: null, phoneNumber: null },
  organization: { id: 'b8c672a6-c2e4-4516-82ad-5b178e71ace1', name: 'Ocharo', role: 'owner', slug: 'ocharo' },
  permissions: ['marketing:edit', 'marketing:approve'],
  csrfToken: 'csrf-token',
  expiresAt: '2026-11-04T08:00:00Z',
};

/** Identity's end-session page, where a sign-out sends the browser. */
export const EndSessionUrl = 'https://identity.example.test/connect/endsession?client_id=marketing';

/** The addresses the app sent the browser to, oldest first; each mount clears it, and navigation is never performed. */
export const navigations: string[] = [];

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

/** A deliberately small workspace: one product, one preset and `drafts` saved posts. */
export function workspaceFixture(drafts = 0): WorkspaceModel {
  return {
    brand: { name: 'Ocharo', timezone: 'Asia/Tashkent', handle: null },
    products: [
      {
        id: 'product',
        name: 'Nimcha',
        category: 'Nimcha',
        material: 'Synthetic',
        description: '',
        heroAssetId: 'front',
        assetIds: ['front'],
        sizes: ['S'],
        price: null,
        currency: 'UZS',
        reviewStatus: 'pending',
        sourceRevision: 'test',
        issues: [],
      },
    ],
    assets: [
      {
        id: 'front',
        fileName: 'front.png',
        contentType: 'image/png',
        byteLength: 1,
        sha256: 'test',
        provenance: 'test fixture',
        url: '/front.png',
        role: 'front',
      },
    ],
    presets: [
      {
        id: 't02-bone',
        name: 'Bone',
        family: 'T02',
        surface: 'bone',
        description: '',
        requiresFabric: false,
        width: 1080,
        height: 1350,
      },
    ],
    drafts: Array.from({ length: drafts }, (_, index) => ({
      id: `draft-${index}`,
      title: `Draft ${index}`,
      productId: 'product',
      presetId: 't02-bone',
      locale: 'uz',
      caption: '',
      channels: ['instagram'],
      mediaIds: [],
      revision: 1,
      approvedRevision: null,
      status: 'draft',
      createdAt: '2026-09-27T00:00:00Z',
      updatedAt: '2026-09-27T00:00:00Z',
    })),
    schedules: [],
    connections: [],
    metrics: [],
    generationJobs: [],
  };
}

export interface MarketingAppOptions {
  /** The viewport width; default a 1440px desktop. */
  readonly width?: number;
  /** When given, `/api/runtime-config` serves it and the app menu resolves it the way startup does. */
  readonly runtimeConfig?: Record<string, string>;
  /** What `GET /api/identity/session` serves; default a signed-in person who edits and approves. */
  readonly session?: Record<string, unknown>;
  /** The status `POST /api/identity/sign-out` answers; default 200 with Identity's end-session address. */
  readonly signOutStatus?: number;
  /** The saved posts the workspace holds. */
  readonly drafts?: number;
}

/**
 * Mounts the whole desk — the real route table, the Ocharo frame and the pages — at `path`, the way startup does:
 * the session is read first, then the app mounts. Every unknown API read answers 404.
 */
export async function mountMarketing(
  path: string,
  options: MarketingAppOptions = {},
): Promise<{ wrapper: VueWrapper; router: ReturnType<typeof createMarketingRouter> }> {
  stubViewport(options.width ?? 1440);
  navigations.length = 0;
  vi.spyOn(window.location, 'assign').mockImplementation((url) => {
    navigations.push(String(url));
  });
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input instanceof Request ? input.url : input);
      const pathname = new URL(url, 'https://localhost').pathname;
      if (pathname === '/api/runtime-config') return Response.json(options.runtimeConfig ?? {});
      if (pathname === '/api/system/status')
        return Response.json({ service: 'OcharoMarketing', status: 'healthy', version: ApiVersion });
      if (pathname === '/api/identity/session') return Response.json({ data: options.session ?? SignedIn });
      if (pathname === '/api/identity/sign-out')
        return options.signOutStatus && options.signOutStatus !== 200
          ? new Response(null, { status: options.signOutStatus })
          : Response.json({ data: { endSessionUrl: EndSessionUrl } });
      return Response.json({ title: 'Not found' }, { status: 404 });
    }),
  );
  vi.spyOn(marketingApi, 'getWorkspace').mockResolvedValue(ResultExtensions.ok(workspaceFixture(options.drafts)));
  const router = createMarketingRouter({ history: RouterHistoryMode.Memory });
  const workspace = createMarketingWorkspace((target) => {
    void router.push(target);
  });
  await router.push(path);
  await router.isReady();
  configureIntegration({ permissionNamespace: 'marketing' });
  await refreshIntegration();
  const plugins: Plugin[] = [router];
  if (options.runtimeConfig) plugins.push(ocharoAppsPlugin(await loadOcharoApps(importMetaEnvSource(import.meta.env))));
  const wrapper = mount(AppRoot, {
    attachTo: document.body,
    global: { plugins, provide: { [MarketingWorkspaceKey as symbol]: workspace } },
  });
  await flushPromises();
  return { wrapper, router };
}
