import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const success = (data: unknown) =>
  new Response(JSON.stringify({ data }), {
    headers: { 'Content-Type': 'application/json' },
  });
const authenticated = {
  enabled: true,
  authenticated: true,
  userId: 'operator-1',
  organizationId: 'ocharo',
  canEdit: true,
  canApprove: false,
  csrfToken: 'memory-only-token',
};
let fetcher: ReturnType<typeof vi.fn<typeof fetch>>;
beforeEach(() => {
  vi.resetModules();
  fetcher = vi.fn<typeof fetch>();
  vi.stubGlobal('fetch', fetcher);
  window.history.replaceState({}, '', '/');
  localStorage.clear();
  sessionStorage.clear();
});
afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});
const load = () => import('../../../packages/connect/src/session');

describe('Ocharo shared sign-in', () => {
  it('loads SSO before any legacy endpoint and keeps its token only in memory', async () => {
    const storeWrite = vi.spyOn(Storage.prototype, 'setItem');
    fetcher.mockResolvedValueOnce(success(authenticated));
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/sso/session');
    expect(session.integrationSession.value).toEqual({
      mode: 'sso',
      active: true,
      configured: true,
      canEdit: true,
      canApprove: false,
    });
    expect(session.integrationSession.value).not.toHaveProperty('csrfToken');
    expect(session.integrationAccess.value).toBe(true);
    expect(storeWrite).not.toHaveBeenCalled();
  });

  it('automatically redirects an anonymous initial visit to central sign-in once', async () => {
    fetcher.mockResolvedValueOnce(success({ enabled: true, authenticated: false }));
    const assign = vi.spyOn(window.location, 'assign').mockImplementation(() => {});
    window.history.replaceState({}, '', '/studio/stories?status=draft');
    const session = await load();
    expect(await session.initializeIntegration()).toBe(false);
    expect(assign).toHaveBeenCalledExactlyOnceWith('/api/sso/login?returnUrl=%2Fstudio%2Fstories%3Fstatus%3Ddraft');
    expect(session.integrationAccess.value).toBe(false);
  });

  it('uses legacy tickets only after SSO explicitly reports disabled and removes the fragment first', async () => {
    window.history.replaceState({}, '', '/#platformToken=legacy-ticket');
    const legacy = { mode: 'cookie', active: true, configured: true, canEdit: true };
    fetcher.mockImplementation(async (url) => {
      expect(window.location.hash).toBe('');
      if (url === '/api/sso/session') return success({ enabled: false, authenticated: false });
      if (url === '/api/integrations/session') return new Response(JSON.stringify(legacy));
      return new Response(null, { status: 204 });
    });
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(session.integrationMode.value).toBe('legacy');
    expect(fetcher.mock.calls.map((call) => call[0])).toEqual([
      '/api/sso/session',
      '/api/integrations/session',
      '/api/integrations/connect',
      '/api/sso/session',
      '/api/integrations/session',
    ]);
    expect(fetcher.mock.calls[2]?.[1]?.body).toBe(JSON.stringify({ ticket: 'legacy-ticket' }));
  });

  it('discards legacy fragments without redeeming them when SSO is enabled', async () => {
    window.history.replaceState({}, '', '/#ocharo-ticket=old-ticket');
    fetcher.mockResolvedValueOnce(success(authenticated));
    const session = await load();
    await session.initializeIntegration();
    expect(window.location.hash).toBe('');
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it.each([
    new Response(null, { status: 503 }),
    success({ enabled: true, authenticated: true, userId: 'operator-1' }),
    success({ enabled: false }),
  ])('blocks safely when SSO availability cannot be established', async (response) => {
    fetcher.mockResolvedValueOnce(response);
    const assign = vi.spyOn(window.location, 'assign').mockImplementation(() => {});
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(session.integrationMode.value).toBe('unknown');
    expect(session.integrationAccess.value).toBe(false);
    expect(session.integrationError.value).not.toBe('');
    expect(assign).not.toHaveBeenCalled();
    expect(fetcher).toHaveBeenCalledTimes(1);
    await expect(session.integrationFetch('/api/drafts', { method: 'POST' })).rejects.toThrow('Sign in again');
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it.each(['POST', 'PUT', 'PATCH', 'DELETE'])(
    'attaches CSRF and same-origin cookies to %s without changing the payload',
    async (method) => {
      fetcher.mockResolvedValueOnce(success(authenticated)).mockResolvedValueOnce(new Response(null, { status: 204 }));
      const session = await load();
      await session.initializeIntegration();
      await session.integrationFetch('/api/drafts', {
        method,
        body: 'payload',
        headers: { 'Content-Type': 'text/plain', 'X-CSRF-Token': 'stale' },
      });
      const options = fetcher.mock.calls[1]?.[1];
      expect(new Headers(options?.headers).get('X-CSRF-Token')).toBe('memory-only-token');
      expect(new Headers(options?.headers).get('Content-Type')).toBe('text/plain');
      expect(options?.credentials).toBe('same-origin');
      expect(options?.redirect).toBe('error');
      expect(options?.body).toBe('payload');
      expect(options?.method).toBe(method);
    },
  );

  it('preserves Request headers and never adds CSRF to reads or another origin', async () => {
    fetcher.mockResolvedValueOnce(success(authenticated));
    fetcher.mockImplementation(async () => new Response(null, { status: 204 }));
    const session = await load();
    await session.initializeIntegration();
    const request = new Request(new URL('/api/drafts', window.location.href), {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: '{}',
    });
    await session.integrationFetch(request);
    expect(new Headers(fetcher.mock.calls[1]?.[1]?.headers).get('Content-Type')).toBe('application/json');
    expect(new Headers(fetcher.mock.calls[1]?.[1]?.headers).get('X-CSRF-Token')).toBe('memory-only-token');
    await session.integrationFetch('/api/drafts', { headers: { 'X-CSRF-Token': 'stale' } });
    expect(new Headers(fetcher.mock.calls[2]?.[1]?.headers).has('X-CSRF-Token')).toBe(false);
    const external = { method: 'POST', body: 'external' };
    await session.integrationFetch('https://elsewhere.example/upload', external);
    expect(fetcher.mock.calls[3]?.[1]).toBe(external);
  });

  it('expires without navigation, clears permissions, and blocks later writes', async () => {
    fetcher.mockResolvedValueOnce(success(authenticated)).mockResolvedValueOnce(new Response(null, { status: 401 }));
    const assign = vi.spyOn(window.location, 'assign').mockImplementation(() => {});
    const session = await load();
    await session.initializeIntegration();
    const response = await session.integrationFetch('/api/drafts', { method: 'POST' });
    expect(response.status).toBe(401);
    expect(session.integrationSession.value).toMatchObject({ active: false, canEdit: false, canApprove: false });
    await expect(session.integrationFetch('/api/drafts', { method: 'DELETE' })).rejects.toThrow();
    expect(fetcher).toHaveBeenCalledTimes(2);
    expect(assign).not.toHaveBeenCalled();
  });

  it('announces one expiry per signed-in session with the page to return to', async () => {
    fetcher
      .mockResolvedValueOnce(success(authenticated))
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 401 }));
    window.history.replaceState({}, '', '/studio/stories/story-1?tab=copy');
    const session = await load();
    const expired = vi.fn();
    session.subscribeIntegrationExpired(expired);
    await session.initializeIntegration();
    expect(expired).not.toHaveBeenCalled();
    await session.integrationFetch('/api/drafts', { method: 'POST' });
    await session.integrationFetch('/api/drafts');
    expect(expired).toHaveBeenCalledExactlyOnceWith({ returnUrl: '/studio/stories/story-1?tab=copy' });
    expect(session.integrationReturnPath.value).toBe('/studio/stories/story-1?tab=copy');
  });

  it('announces a status read that finds the session signed out, but not an anonymous first visit', async () => {
    fetcher
      .mockResolvedValueOnce(success(authenticated))
      .mockResolvedValueOnce(success({ enabled: true, authenticated: false }))
      .mockResolvedValueOnce(success({ enabled: true, authenticated: false }));
    const session = await load();
    const expired = vi.fn();
    const unsubscribe = session.subscribeIntegrationExpired(expired);
    await session.initializeIntegration();
    await session.refreshIntegration();
    await session.refreshIntegration();
    expect(expired).toHaveBeenCalledTimes(1);
    expect(session.integrationAccess.value).toBe(false);
    unsubscribe();

    vi.resetModules();
    fetcher.mockReset().mockResolvedValueOnce(success({ enabled: true, authenticated: false }));
    vi.spyOn(window.location, 'assign').mockImplementation(() => {});
    const fresh = await load();
    const anonymous = vi.fn();
    fresh.subscribeIntegrationExpired(anonymous);
    expect(await fresh.initializeIntegration()).toBe(false);
    expect(anonymous).not.toHaveBeenCalled();
  });

  it('does not resurrect a session from an older status read after a 401', async () => {
    let resolveStatus!: (response: Response) => void;
    fetcher
      .mockResolvedValueOnce(success(authenticated))
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveStatus = resolve;
          }),
      )
      .mockResolvedValueOnce(new Response(null, { status: 401 }));
    const session = await load();
    await session.initializeIntegration();
    const refresh = session.refreshIntegration();
    await session.integrationFetch('/api/drafts', { method: 'POST' });
    resolveStatus(success(authenticated));
    await refresh;
    expect(session.integrationSession.value?.active).toBe(false);
  });

  it('requires a reload before another account can authorize preserved drafts', async () => {
    fetcher
      .mockResolvedValueOnce(success(authenticated))
      .mockResolvedValueOnce(success({ ...authenticated, userId: 'operator-2', csrfToken: 'other-token' }));
    const session = await load();
    await session.initializeIntegration();
    await session.refreshIntegration();
    expect(session.integrationIdentityChanged.value).toBe(true);
    expect(session.integrationAccess.value).toBe(false);
    await expect(session.integrationFetch('/api/drafts', { method: 'POST' })).rejects.toThrow();
    expect(fetcher).toHaveBeenCalledTimes(2);
  });

  it.each(['//foreign.example', '/api/session', '/API/sso', '/a%2fb', '/a\\b', '/path\nnext'])(
    'rejects unsafe return path %s',
    async (path) => {
      const session = await load();
      expect(session.ssoLoginUrl(path)).toBe('/api/sso/login?returnUrl=%2F');
    },
  );
});
