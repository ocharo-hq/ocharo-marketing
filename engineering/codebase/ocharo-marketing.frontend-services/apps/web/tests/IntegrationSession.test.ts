import { createApiClient } from '@wow-two-beta/fe/foundation/http';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const success = (data: unknown) =>
  new Response(JSON.stringify({ data }), {
    headers: { 'Content-Type': 'application/json' },
  });
/** What `GET /api/identity/session` answers for a person who edits but does not approve. */
const signedIn = {
  isAuthenticated: true,
  person: { sub: 'operator-1', name: null, picture: null, email: null, phoneNumber: null },
  organization: { id: 'org-1', name: 'Ocharo', role: 'editor', slug: 'ocharo' },
  permissions: ['marketing:edit'],
  csrfToken: 'memory-only-token',
  expiresAt: '2026-11-04T08:00:00Z',
};
const signedOut = { isAuthenticated: false };
const EndSessionUrl = 'https://identity.example.test/connect/endsession?client_id=marketing';
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
const load = async (permissionNamespace: string | null = 'marketing') => {
  const session = await import('../../../packages/connect/src/session');
  if (permissionNamespace) session.configureIntegration({ permissionNamespace });
  return session;
};
const assignSpy = () => vi.spyOn(window.location, 'assign').mockImplementation(() => {});
/** A write without an authorizing session is not sent; it resolves the local 401 problem, never a throw. */
const expectRefusedWrite = async (session: Awaited<ReturnType<typeof load>>, method = 'POST') => {
  const calls = fetcher.mock.calls.length;
  const response = await session.integrationFetch('/api/drafts', { method });
  expect(response.status).toBe(401);
  expect(await response.json()).toMatchObject({ code: session.SessionRequiredCode });
  expect(fetcher).toHaveBeenCalledTimes(calls);
};

describe('Ocharo shared sign-in', () => {
  it('reads the Identity session and keeps its token only in memory', async () => {
    const storeWrite = vi.spyOn(Storage.prototype, 'setItem');
    fetcher.mockResolvedValueOnce(success(signedIn));
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(fetcher.mock.calls[0]?.[0]).toBe('/api/identity/session');
    expect(session.integrationSession.value).toEqual({
      active: true,
      canEdit: true,
      canApprove: false,
      expiresAt: '2026-11-04T08:00:00Z',
    });
    expect(session.integrationSession.value).not.toHaveProperty('csrfToken');
    expect(session.integrationAccess.value).toBe(true);
    expect(storeWrite).not.toHaveBeenCalled();
  });

  it.each([
    ['edit and approve', ['marketing:edit', 'marketing:approve'], true, true],
    ['edit only', ['marketing:edit'], true, false],
    ['approve without edit', ['marketing:approve'], false, false],
    ['another app’s permissions', ['editorial:edit', 'editorial:approve', 'catalogue:edit'], false, false],
    ['no permissions', [], false, false],
  ])(
    'derives canEdit and canApprove from the permissions under the app namespace: %s',
    async (_case, permissions, edit, approve) => {
      fetcher.mockResolvedValueOnce(success({ ...signedIn, permissions }));
      const session = await load();
      await session.initializeIntegration();
      expect(session.integrationAccess.value).toBe(true);
      expect(session.integrationSession.value).toMatchObject({ active: true, canEdit: edit, canApprove: approve });
    },
  );

  it('reads the namespace the app configured, and nobody edits before it is configured', async () => {
    const permissions = ['marketing:edit', 'marketing:approve', 'editorial:edit'];
    fetcher.mockImplementation(async () => success({ ...signedIn, permissions }));
    const editorial = await load('editorial');
    await editorial.refreshIntegration();
    expect(editorial.integrationSession.value).toMatchObject({ canEdit: true, canApprove: false });

    vi.resetModules();
    const unconfigured = await load(null);
    await unconfigured.refreshIntegration();
    expect(unconfigured.integrationSession.value).toMatchObject({ active: true, canEdit: false, canApprove: false });
  });

  it('refuses a blank permission namespace', async () => {
    const session = await load(null);
    expect(() => session.configureIntegration({ permissionNamespace: '  ' })).toThrow();
  });

  it('automatically redirects an anonymous initial visit to sign-in once, returning to the current page', async () => {
    fetcher.mockResolvedValueOnce(success(signedOut));
    const assign = assignSpy();
    window.history.replaceState({}, '', '/studio/stories?status=draft');
    const session = await load();
    expect(await session.initializeIntegration()).toBe(false);
    expect(assign).toHaveBeenCalledExactlyOnceWith(
      '/api/identity/sign-in?returnUrl=%2Fstudio%2Fstories%3Fstatus%3Ddraft',
    );
    expect(session.integrationAccess.value).toBe(false);
  });

  it('opens the app without redirecting again when the last sign-in callback failed', async () => {
    fetcher.mockResolvedValueOnce(success(signedOut));
    const assign = assignSpy();
    window.history.replaceState({}, '', '/posts?signInError=callback_failed');
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(assign).not.toHaveBeenCalled();
    expect(session.integrationAccess.value).toBe(false);
    expect(session.hasSignInError()).toBe(true);
    expect(session.signInUrl()).toBe('/api/identity/sign-in?returnUrl=%2Fposts');
  });

  it.each([
    new Response(null, { status: 503 }),
    success({ ...signedIn, csrfToken: '' }),
    success({ ...signedIn, person: null }),
    success({ ...signedIn, permissions: 'marketing:edit' }),
    success({}),
    new Response('not json', { headers: { 'Content-Type': 'application/json' } }),
  ])('blocks safely when the session cannot be established', async (response) => {
    fetcher.mockResolvedValueOnce(response);
    const assign = assignSpy();
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(session.integrationSession.value).toBeNull();
    expect(session.integrationAccess.value).toBe(false);
    expect(session.integrationError.value).not.toBe('');
    expect(assign).not.toHaveBeenCalled();
    expect(fetcher).toHaveBeenCalledTimes(1);
    await expectRefusedWrite(session);
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it('blocks safely when the API does not answer the session read', async () => {
    fetcher.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    const session = await load();
    expect(await session.initializeIntegration()).toBe(true);
    expect(session.integrationAccess.value).toBe(false);
    expect(session.integrationError.value).toContain('unavailable');
    expect(session.integrationChecking.value).toBe(false);
  });

  it('hands a refused write to an API client as a quiet unauthorized failure, not a transport one', async () => {
    fetcher.mockResolvedValueOnce(success(signedOut));
    window.history.replaceState({}, '', '/posts?signInError=callback_failed');
    const session = await load();
    await session.initializeIntegration();
    const client = createApiClient({ retry: false, fetch: session.integrationFetch });
    const result = await client.post('/api/drafts', { body: {}, response: 'empty' });
    expect(result).toMatchObject({ ok: false, failure: { code: 'http', status: 401, type: 'unauthorized' } });
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it.each(['POST', 'PUT', 'PATCH', 'DELETE'])(
    'attaches CSRF and same-origin cookies to %s without changing the payload',
    async (method) => {
      fetcher.mockResolvedValueOnce(success(signedIn)).mockResolvedValueOnce(new Response(null, { status: 204 }));
      const session = await load();
      await session.initializeIntegration();
      await session.integrationFetch('/api/drafts', {
        method,
        body: 'payload',
        headers: { 'Content-Type': 'text/plain', 'X-Csrf-Token': 'stale' },
      });
      const options = fetcher.mock.calls[1]?.[1];
      expect(new Headers(options?.headers).get('X-Csrf-Token')).toBe('memory-only-token');
      expect(new Headers(options?.headers).get('Content-Type')).toBe('text/plain');
      expect(options?.credentials).toBe('same-origin');
      expect(options?.redirect).toBe('error');
      expect(options?.body).toBe('payload');
      expect(options?.method).toBe(method);
    },
  );

  it('preserves Request headers and never adds CSRF to reads or another origin', async () => {
    fetcher.mockResolvedValueOnce(success(signedIn));
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
    expect(new Headers(fetcher.mock.calls[1]?.[1]?.headers).get('X-Csrf-Token')).toBe('memory-only-token');
    await session.integrationFetch('/api/drafts', { headers: { 'X-Csrf-Token': 'stale' } });
    expect(new Headers(fetcher.mock.calls[2]?.[1]?.headers).has('X-Csrf-Token')).toBe(false);
    const external = { method: 'POST', body: 'external' };
    await session.integrationFetch('https://elsewhere.example/upload', external);
    expect(fetcher.mock.calls[3]?.[1]).toBe(external);
  });

  it('expires without navigation, clears permissions, and blocks later writes', async () => {
    fetcher.mockResolvedValueOnce(success(signedIn)).mockResolvedValueOnce(new Response(null, { status: 401 }));
    const assign = assignSpy();
    const session = await load();
    await session.initializeIntegration();
    const response = await session.integrationFetch('/api/drafts', { method: 'POST' });
    expect(response.status).toBe(401);
    expect(session.integrationSession.value).toMatchObject({ active: false, canEdit: false, canApprove: false });
    expect(session.integrationError.value).toContain('Sign in again');
    await expectRefusedWrite(session, 'DELETE');
    expect(fetcher).toHaveBeenCalledTimes(2);
    expect(assign).not.toHaveBeenCalled();
  });

  it('announces one expiry per signed-in session with the page to return to', async () => {
    fetcher
      .mockResolvedValueOnce(success(signedIn))
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
      .mockResolvedValueOnce(success(signedIn))
      .mockResolvedValueOnce(success(signedOut))
      .mockResolvedValueOnce(success(signedOut));
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
    fetcher.mockReset().mockResolvedValueOnce(success(signedOut));
    assignSpy();
    const fresh = await load();
    const anonymous = vi.fn();
    fresh.subscribeIntegrationExpired(anonymous);
    expect(await fresh.initializeIntegration()).toBe(false);
    expect(anonymous).not.toHaveBeenCalled();
  });

  it('does not resurrect a session from an older status read after a 401', async () => {
    let resolveStatus!: (response: Response) => void;
    fetcher
      .mockResolvedValueOnce(success(signedIn))
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
    resolveStatus(success(signedIn));
    await refresh;
    expect(session.integrationSession.value?.active).toBe(false);
  });

  it.each([
    ['person', { person: { ...signedIn.person, sub: 'operator-2' } }],
    ['organization', { organization: { ...signedIn.organization, id: 'org-2' } }],
  ])('requires a reload before another %s can authorize preserved drafts', async (_case, change) => {
    fetcher
      .mockResolvedValueOnce(success(signedIn))
      .mockResolvedValueOnce(success({ ...signedIn, ...change, csrfToken: 'other-token' }));
    const session = await load();
    await session.initializeIntegration();
    await session.refreshIntegration();
    expect(session.integrationIdentityChanged.value).toBe(true);
    expect(session.integrationAccess.value).toBe(false);
    expect(session.integrationError.value).toContain('Reload this app');
    await expectRefusedWrite(session);
    expect(fetcher).toHaveBeenCalledTimes(2);
  });

  it('names the signed-in person with the profile Identity sends, trimmed, and a picture only over HTTPS', async () => {
    fetcher
      .mockResolvedValueOnce(
        success({
          ...signedIn,
          person: {
            sub: 'operator-1',
            name: '  Maftuna Rakhimova ',
            email: 'maftuna@ocharo.example.test',
            phoneNumber: '+998901234567',
            picture: 'https://identity.example.test/people/operator-1.png',
          },
        }),
      )
      .mockResolvedValueOnce(
        success({ ...signedIn, person: { ...signedIn.person, picture: 'http://identity.example.test/p.png' } }),
      );
    const session = await load();
    await session.initializeIntegration();
    expect(session.integrationAccount.value).toEqual({
      userId: 'operator-1',
      name: 'Maftuna Rakhimova',
      email: 'maftuna@ocharo.example.test',
      phoneNumber: '+998901234567',
      picture: 'https://identity.example.test/people/operator-1.png',
    });
    await session.refreshIntegration();
    expect(session.integrationAccount.value).toMatchObject({ picture: null });
  });

  it.each([
    ['absent', {}],
    ['null', { name: null, email: null, phoneNumber: null, picture: null }],
    ['blank or not text', { name: '   ', email: 42, phoneNumber: false, picture: ['x'] }],
  ])('keeps the session and reads no profile fields when they are %s', async (_case, profile) => {
    fetcher.mockResolvedValueOnce(success({ ...signedIn, person: { sub: 'operator-1', ...profile } }));
    const session = await load();
    await session.initializeIntegration();
    expect(session.integrationAccess.value).toBe(true);
    expect(session.integrationError.value).toBe('');
    expect(session.integrationAccount.value).toEqual({
      userId: 'operator-1',
      name: null,
      email: null,
      phoneNumber: null,
      picture: null,
    });
  });

  it.each([
    ['a forbidden write', Response.json({ code: 'edit_permission_required' }, { status: 403 })],
    ['a stale CSRF token', Response.json({ code: 'AntiforgeryValidationFailed' }, { status: 400 })],
  ])('reads the session again after %s', async (_case, refused) => {
    fetcher
      .mockResolvedValueOnce(success(signedIn))
      .mockResolvedValueOnce(refused)
      .mockResolvedValueOnce(success({ ...signedIn, permissions: [], csrfToken: 'fresh-token' }));
    const session = await load();
    await session.initializeIntegration();
    await session.integrationFetch('/api/drafts', { method: 'POST' });
    await vi.waitFor(() => expect(session.integrationSession.value?.canEdit).toBe(false));
    expect(fetcher.mock.calls.map((call) => call[0])).toEqual([
      '/api/identity/session',
      '/api/drafts',
      '/api/identity/session',
    ]);
  });

  it('does not read the session again after other refusals', async () => {
    fetcher
      .mockResolvedValueOnce(success(signedIn))
      .mockResolvedValueOnce(Response.json({ code: 'validation' }, { status: 400 }))
      .mockResolvedValueOnce(Response.json({}, { status: 404 }));
    const session = await load();
    await session.initializeIntegration();
    await session.integrationFetch('/api/drafts', { method: 'POST' });
    await session.integrationFetch('/api/drafts/missing');
    expect(fetcher).toHaveBeenCalledTimes(3);
  });

  it('signs out with the CSRF token, then sends the browser to Identity’s end-session page', async () => {
    fetcher.mockResolvedValueOnce(success(signedIn)).mockResolvedValueOnce(success({ endSessionUrl: EndSessionUrl }));
    const assign = assignSpy();
    window.history.replaceState({}, '', '/posts?view=drafts');
    const session = await load();
    const expired = vi.fn();
    session.subscribeIntegrationExpired(expired);
    await session.initializeIntegration();

    expect(await session.signOutIntegration()).toEqual({ ok: true });
    const [url, options] = fetcher.mock.calls[1]!;
    expect(url).toBe('/api/identity/sign-out');
    expect(options).toMatchObject({ method: 'POST', credentials: 'same-origin', redirect: 'error' });
    expect(new Headers(options?.headers).get('X-Csrf-Token')).toBe('memory-only-token');
    expect(assign).toHaveBeenCalledExactlyOnceWith(EndSessionUrl);
    expect(session.integrationAccess.value).toBe(false);
    expect(session.integrationAccount.value).toBeNull();
    expect(session.integrationError.value).toBe('');
    expect(session.integrationReturnPath.value).toBe('/posts?view=drafts');
    expect(expired).not.toHaveBeenCalled();
    await expectRefusedWrite(session);
  });

  it.each([
    ['none', {}, '/'],
    ['a foreign host over HTTP', { endSessionUrl: 'http://identity.example.test/end' }, '/'],
    ['a protocol-relative address', { endSessionUrl: '//evil.example/end' }, '/'],
    ['a script address', { endSessionUrl: 'javascript:alert(1)' }, '/'],
    ['a local path', { endSessionUrl: '/signed-out' }, '/signed-out'],
  ])('sends the browser only to a safe end-session address (%s)', async (_case, body, destination) => {
    fetcher.mockResolvedValueOnce(success(signedIn)).mockResolvedValueOnce(success(body));
    const assign = assignSpy();
    const session = await load();
    await session.initializeIntegration();
    expect(await session.signOutIntegration()).toEqual({ ok: true });
    expect(assign).toHaveBeenCalledExactlyOnceWith(destination);
  });

  it('falls back to the Studio root after a sign-out that names no address', async () => {
    fetcher.mockResolvedValueOnce(success(signedIn)).mockResolvedValueOnce(success({}));
    const assign = assignSpy();
    window.history.replaceState({}, '', '/studio/stories');
    const session = await load();
    await session.initializeIntegration();
    await session.signOutIntegration();
    expect(assign).toHaveBeenCalledExactlyOnceWith('/studio/');
  });

  it('does not resurrect a session from a status read that started before sign-out', async () => {
    let resolveStatus!: (response: Response) => void;
    fetcher
      .mockResolvedValueOnce(success(signedIn))
      .mockImplementationOnce(
        () =>
          new Promise((resolve) => {
            resolveStatus = resolve;
          }),
      )
      .mockResolvedValueOnce(success({ endSessionUrl: EndSessionUrl }));
    assignSpy();
    const session = await load();
    await session.initializeIntegration();
    const refresh = session.refreshIntegration();
    await session.signOutIntegration();
    resolveStatus(success(signedIn));
    await refresh;
    expect(session.integrationAccess.value).toBe(false);
  });

  it.each([
    [400, 'This tab could not confirm its Ocharo sign-in. Reload the app, then sign out.'],
    [403, 'This tab could not confirm its Ocharo sign-in. Reload the app, then sign out.'],
    [503, 'Ocharo could not end this sign-in. Your work stays here; try again shortly.'],
    [500, 'Could not sign out. Try again.'],
  ])('keeps the session and returns display-safe feedback when sign-out answers %i', async (status, message) => {
    fetcher
      .mockResolvedValueOnce(success(signedIn))
      .mockResolvedValueOnce(Response.json({ code: 'sign_out_unavailable', detail: 'raw' }, { status }));
    const assign = assignSpy();
    const session = await load();
    await session.initializeIntegration();
    expect(await session.signOutIntegration()).toEqual({ ok: false, message });
    expect(session.integrationAccess.value).toBe(true);
    expect(session.integrationSigningOut.value).toBe(false);
    expect(assign).not.toHaveBeenCalled();
  });

  it('treats a sign-out that finds the sign-in already ended as signed out, through the expiry flow', async () => {
    fetcher.mockResolvedValueOnce(success(signedIn)).mockResolvedValueOnce(new Response(null, { status: 401 }));
    const assign = assignSpy();
    const session = await load();
    const expired = vi.fn();
    session.subscribeIntegrationExpired(expired);
    await session.initializeIntegration();
    expect(await session.signOutIntegration()).toEqual({ ok: true });
    expect(session.integrationAccess.value).toBe(false);
    expect(expired).toHaveBeenCalledTimes(1);
    expect(assign).not.toHaveBeenCalled();
  });

  it('offers no sign-out outside an active Ocharo session', async () => {
    fetcher.mockResolvedValueOnce(success(signedOut));
    const session = await load();
    await session.refreshIntegration();
    expect(await session.signOutIntegration()).toEqual({ ok: false, message: 'Could not sign out. Try again.' });
    expect(fetcher.mock.calls.map((call) => call[0])).not.toContain('/api/identity/sign-out');
  });

  it.each(['//foreign.example', '/api/session', '/API/identity', '/a%2fb', '/a\\b', '/path\nnext'])(
    'rejects unsafe return path %s',
    async (path) => {
      const session = await load();
      expect(session.signInUrl(path)).toBe('/api/identity/sign-in?returnUrl=%2F');
    },
  );

  it('falls back to the Studio root for an unsafe return path inside the Studio', async () => {
    window.history.replaceState({}, '', '/studio/stories');
    const session = await load();
    expect(session.signInUrl('//foreign.example')).toBe('/api/identity/sign-in?returnUrl=%2Fstudio%2F');
  });

  it('carries a local return path, minus the mark of a failed sign-in', async () => {
    const session = await load();
    expect(session.signInUrl('/posts/draft-1?tab=copy')).toBe(
      '/api/identity/sign-in?returnUrl=%2Fposts%2Fdraft-1%3Ftab%3Dcopy',
    );
    expect(session.signInUrl('/posts?signInError=callback_failed&view=drafts')).toBe(
      '/api/identity/sign-in?returnUrl=%2Fposts%3Fview%3Ddrafts',
    );
  });
});
