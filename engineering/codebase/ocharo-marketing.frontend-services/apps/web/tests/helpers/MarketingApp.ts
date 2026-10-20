import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { vi } from 'vitest';
import type { Plugin } from 'vue';
import { importMetaEnvSource } from '@wow-two-beta/fe/foundation/config';
import { ResultExtensions } from '@wow-two-beta/fe/foundation/results';
import { RouterHistoryMode } from '@wow-two-beta/fe/router';
import { refreshIntegration } from '@ocharomarketing/connect';
import { loadOcharoApps, ocharoAppsPlugin } from '@ocharomarketing/connect/OcharoAppsConfig';

import { createMarketingWorkspace, MarketingWorkspaceKey } from '../../src/application/marketing';
import AppRoot from '../../src/bootstrap/AppRoot.vue';
import { createMarketingRouter } from '../../src/bootstrap/MarketingRouter';
import type { WorkspaceModel } from '../../src/domain/marketing';
import { marketingApi } from '../../src/integration/marketing';

/** The informational version the stubbed `/api/system/status` reports: a release and the commit it was built from. */
export const ApiVersion = '0.1.4+8f2c1d9e4b7a';

/** One signed-in Ocharo session, the same identity in every test, so the identity-change rule never trips. */
export const SignedIn = {
  enabled: true,
  authenticated: true,
  userId: 'operator-1',
  organizationId: 'ocharo',
  canEdit: true,
  canApprove: true,
  csrfToken: 'csrf-token',
  accountUrl: 'https://desk.example.test/',
};

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
  /** The session `/api/sso/session` serves; default SSO disabled, so the local integration session applies. */
  readonly ssoSession?: Record<string, unknown>;
  /** The status `POST /api/sso/logout` answers; default 204. */
  readonly logoutStatus?: number;
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
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input instanceof Request ? input.url : input);
      const pathname = new URL(url, 'https://localhost').pathname;
      if (pathname === '/api/runtime-config') return Response.json(options.runtimeConfig ?? {});
      if (pathname === '/api/system/status')
        return Response.json({ service: 'OcharoMarketing', status: 'healthy', version: ApiVersion });
      if (pathname === '/api/sso/session')
        return Response.json({ data: options.ssoSession ?? { enabled: false, authenticated: false } });
      if (pathname === '/api/integrations/session')
        return Response.json({ mode: 'local', active: true, configured: true, canEdit: true, canApprove: true });
      if (pathname === '/api/sso/logout') return new Response(null, { status: options.logoutStatus ?? 204 });
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
