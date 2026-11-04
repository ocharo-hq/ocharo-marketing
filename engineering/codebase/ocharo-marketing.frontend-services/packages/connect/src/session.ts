import { computed, ref } from "vue";
/**
 * The Ocharo session of a signed-in app, as Ocharo's Identity BFF serves it at `GET /api/identity/session`. Sign-in
 * happens at Identity; the API keeps the tokens and the browser only a secure session cookie. Every unsafe request
 * echoes the session's CSRF token in `X-Csrf-Token`.
 */
export interface IntegrationSession {
  active: boolean;
  canEdit: boolean;
  canApprove: boolean;
  expiresAt?: string;
}
/**
 * Who is signed in, from Identity's ID token: the person's id and the profile fields Identity sends, each `null` when
 * it sends none (a blank, or a field its scopes leave out).
 */
export interface IntegrationAccount {
  readonly userId: string;
  readonly name: string | null;
  readonly email: string | null;
  readonly phoneNumber: string | null;
  /** An absolute HTTPS link to the person's picture. */
  readonly picture: string | null;
}
interface DecodedSession {
  readonly authenticated: boolean;
  readonly identity: string | null;
  readonly account: IntegrationAccount | null;
  readonly permissions: readonly string[];
  readonly csrfToken: string | null;
  readonly expiresAt: string | null;
}
export const integrationSession = ref<IntegrationSession | null>(null);
export const integrationError = ref("");
export const integrationChecking = ref(false);
export const integrationIdentityChanged = ref(false);
export const integrationAccess = computed(
  () => integrationSession.value?.active === true,
);
/** The app path a sign-in returns to, captured when the signed-in session expired or was signed out. */
export const integrationReturnPath = ref<string | null>(null);
/** The signed-in Ocharo account, while a session is active; `null` otherwise. */
export const integrationAccount = ref<IntegrationAccount | null>(null);
/** True while `signOutIntegration` waits for the server. */
export const integrationSigningOut = ref(false);
/** Describes one expiry of a signed-in session, in the shape of the SDK's `subscribeExpired` event. */
export interface IntegrationExpiry {
  readonly returnUrl: string;
}
const expiryListeners = new Set<(expiry: IntegrationExpiry) => void>();
/** The query parameter the BFF adds to the return address when a sign-in callback failed. */
const SignInErrorParameter = "signInError";
let permissionNamespace: string | null = null;
let csrfToken: string | null = null;
let identity: string | null = null;
let generation = 0;
let refreshing: Promise<void> | undefined;
const unavailable =
  "Connection status is unavailable. Your unsaved work stays here.";
const expired =
  "Your sign-in expired. Sign in again; your unsaved work stays here.";
const accountChanged =
  "Your Ocharo account changed. Reload this app before continuing.";
const signOutFailed = "Could not sign out. Try again.";
const signOutNeedsReload =
  "This tab could not confirm its Ocharo sign-in. Reload the app, then sign out.";
const signOutUnavailable =
  "Ocharo could not end this sign-in. Your work stays here; try again shortly.";

/**
 * Names the permissions this app reads from the session: Identity grants `{permissionNamespace}:edit` and
 * `{permissionNamespace}:approve` (`marketing`, `editorial`). Called once at startup, before the first session read;
 * without it nobody reads as allowed to edit.
 */
export function configureIntegration(options: {
  readonly permissionNamespace: string;
}): void {
  const namespace = options.permissionNamespace.trim();
  if (!namespace) throw new Error("A permission namespace is required.");
  permissionNamespace = namespace;
}
function record(value: unknown): Record<string, unknown> | null {
  return value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}
function filled(value: unknown): value is string {
  return typeof value === "string" && value.length > 0;
}
/** Keeps a profile field only as nonblank text; anything else reads as absent, never as a failed session. */
function displayText(value: unknown): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text.length > 0 ? text : null;
}
/** Accepts a picture only as an absolute HTTPS link. */
function httpsUrl(value: unknown): string | null {
  const link = displayText(value);
  if (!link) return null;
  try {
    return new URL(link).protocol === "https:" ? link : null;
  } catch {
    return null;
  }
}
/** Identity's end-session address (absolute HTTPS) or a local path; anything else is dropped. */
function safeDestination(value: unknown): string | null {
  const link = displayText(value);
  if (!link) return null;
  return link.startsWith("/") && !link.startsWith("//") ? link : httpsUrl(link);
}
function decodeSession(value: unknown): DecodedSession {
  const data = record(record(value)?.data);
  if (!data || typeof data.isAuthenticated !== "boolean")
    throw new Error(unavailable);
  if (!data.isAuthenticated)
    return {
      authenticated: false,
      identity: null,
      account: null,
      permissions: [],
      csrfToken: null,
      expiresAt: null,
    };
  const person = record(data.person);
  if (
    !person ||
    !filled(person.sub) ||
    !filled(data.csrfToken) ||
    !Array.isArray(data.permissions)
  )
    throw new Error(unavailable);
  const organization = record(data.organization);
  return {
    authenticated: true,
    identity: `${filled(organization?.id) ? organization.id : ""}:${person.sub}`,
    account: {
      userId: person.sub,
      name: displayText(person.name),
      email: displayText(person.email),
      phoneNumber: displayText(person.phoneNumber),
      picture: httpsUrl(person.picture),
    },
    permissions: data.permissions.filter(
      (permission): permission is string => typeof permission === "string",
    ),
    csrfToken: data.csrfToken,
    expiresAt: displayText(data.expiresAt),
  };
}
function allows(permissions: readonly string[], action: "edit" | "approve") {
  return (
    permissionNamespace !== null &&
    permissions.includes(`${permissionNamespace}:${action}`)
  );
}
function expire(message: string) {
  generation++;
  csrfToken = null;
  integrationAccount.value = null;
  if (integrationSession.value)
    integrationSession.value = {
      ...integrationSession.value,
      active: false,
      canEdit: false,
      canApprove: false,
    };
  integrationError.value = message;
}
/** Runs once per signed-in session: the transition from active to signed out, never a later 401. */
function announceExpiry(wasActive: boolean) {
  if (!wasActive || integrationSession.value?.active) return;
  const returnUrl = window.location.pathname + window.location.search;
  integrationReturnPath.value = returnUrl;
  for (const listener of [...expiryListeners]) listener({ returnUrl });
}
/**
 * Calls `listener` once each time a signed-in session expires — its first 401 or a status read that finds it
 * signed out. The app stays on its page so unsaved edits survive; the sign-in link returns to `returnUrl`.
 */
export function subscribeIntegrationExpired(
  listener: (expiry: IntegrationExpiry) => void,
): () => void {
  expiryListeners.add(listener);
  return () => {
    expiryListeners.delete(listener);
  };
}
async function readSession() {
  const version = generation;
  let wasActive = false;
  integrationChecking.value = true;
  try {
    const response = await fetch("/api/identity/session", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error(unavailable);
    const session = decodeSession(await response.json());
    if (version !== generation) return;
    if (identity && session.identity && identity !== session.identity) {
      integrationIdentityChanged.value = true;
      expire(accountChanged);
      return;
    }
    if (integrationIdentityChanged.value) return;
    identity ??= session.identity;
    csrfToken = session.csrfToken;
    integrationAccount.value = session.account;
    wasActive = integrationSession.value?.active === true;
    const canEdit =
      session.authenticated && allows(session.permissions, "edit");
    integrationSession.value = {
      active: session.authenticated,
      canEdit,
      canApprove: canEdit && allows(session.permissions, "approve"),
      ...(session.expiresAt ? { expiresAt: session.expiresAt } : {}),
    };
    integrationError.value = "";
    announceExpiry(wasActive);
  } catch {
    if (version === generation) expire(unavailable);
  } finally {
    integrationChecking.value = false;
  }
}
export function refreshIntegration(): Promise<void> {
  refreshing ??= readSession().finally(() => {
    refreshing = undefined;
  });
  return refreshing;
}
/** Whether `search` carries the BFF's mark of a failed sign-in callback. */
export function hasSignInError(search = window.location.search): boolean {
  return new URLSearchParams(search).has(SignInErrorParameter);
}
/** Where the app lives: the Studio under `/studio/`, otherwise the site root. */
function appRoot(): string {
  return window.location.pathname.startsWith("/studio") ? "/studio/" : "/";
}
/** Only local app paths can be handed to the server's redirect flow. */
export function signInUrl(
  returnPath = window.location.pathname + window.location.search,
): string {
  const valid =
    /^\/(?!\/)/.test(returnPath) &&
    !/[\\%\u0000- \u007f]/.test(returnPath) &&
    !/^\/api(?:[/?#]|$)/i.test(returnPath);
  let target = appRoot();
  if (valid) {
    // A sign-in that failed moments ago is not carried into the next one.
    const url = new URL(returnPath, window.location.origin);
    url.searchParams.delete(SignInErrorParameter);
    target = url.pathname + url.search + url.hash;
  }
  return `/api/identity/sign-in?returnUrl=${encodeURIComponent(target)}`;
}
/**
 * Returns false when the browser is leaving for its initial central sign-in. A browser the API answered as signed out
 * goes to sign in and returns here, unless the last sign-in failed moments ago or the API did not answer; the app
 * opens then and offers sign-in instead.
 */
export async function initializeIntegration(): Promise<boolean> {
  await refreshIntegration();
  if (
    integrationSession.value &&
    !integrationSession.value.active &&
    !integrationError.value &&
    !hasSignInError()
  ) {
    window.location.assign(signInUrl());
    return false;
  }
  return true;
}
/** Cookie credentials and the anti-forgery token stay on this app's origin. */
export const integrationFetch: typeof fetch = async (input, init) => {
  const request = input instanceof Request ? input : undefined;
  const url = new URL(request?.url ?? String(input), window.location.href);
  if (url.origin !== window.location.origin)
    return globalThis.fetch(input, init);
  const method = (init?.method ?? request?.method ?? "GET").toUpperCase();
  const headers = new Headers(init?.headers ?? request?.headers);
  headers.delete("X-Csrf-Token");
  const protectedWrite = !["GET", "HEAD", "OPTIONS"].includes(method);
  if (protectedWrite) {
    if (
      !integrationSession.value?.active ||
      !csrfToken ||
      integrationIdentityChanged.value
    )
      throw new Error("Sign in again. Your edits are preserved.");
    headers.set("X-Csrf-Token", csrfToken);
  }
  const response = await globalThis.fetch(input, {
    ...init,
    headers,
    credentials: "same-origin",
    ...(protectedWrite ? { redirect: "error" } : {}),
  });
  if (response.status === 401) {
    const wasActive = integrationSession.value?.active === true;
    expire(expired);
    announceExpiry(wasActive);
  } else if (
    url.pathname.startsWith("/api/") &&
    !url.pathname.startsWith("/api/identity/") &&
    (response.status === 403 || (await antiforgeryFailed(response)))
  )
    // The session's permissions or token changed since the last read.
    void refreshIntegration();
  return response;
};
/** Whether `response` is the BFF's 400 for a missing or stale CSRF token. */
async function antiforgeryFailed(response: Response): Promise<boolean> {
  if (response.status !== 400) return false;
  try {
    return (
      record(await response.clone().json())?.code ===
      "AntiforgeryValidationFailed"
    );
  } catch {
    return false;
  }
}
/** The outcome of `signOutIntegration`: `message` is a display-safe English source for the operator. */
export type IntegrationSignOut =
  | { readonly ok: true }
  | { readonly ok: false; readonly message: string };
/**
 * Ends this app's session with `POST /api/identity/sign-out` and the in-memory CSRF token, then sends the browser to
 * Identity's end-session page, which ends the Ocharo sign-in for every app and returns here. A 401 means the sign-in
 * had already ended (the expiry flow reports it). A 400, 403 or 503 leaves the session as it was.
 */
export async function signOutIntegration(): Promise<IntegrationSignOut> {
  if (integrationSigningOut.value) return { ok: false, message: "" };
  if (!integrationSession.value?.active)
    return { ok: false, message: signOutFailed };
  integrationSigningOut.value = true;
  try {
    const response = await integrationFetch("/api/identity/sign-out", {
      method: "POST",
      cache: "no-store",
    });
    if (response.ok) {
      const endSessionUrl = safeDestination(
        record(record(await response.json().catch(() => null))?.data)
          ?.endSessionUrl,
      );
      generation++;
      csrfToken = null;
      integrationAccount.value = null;
      integrationReturnPath.value =
        window.location.pathname + window.location.search;
      if (integrationSession.value)
        integrationSession.value = {
          ...integrationSession.value,
          active: false,
          canEdit: false,
          canApprove: false,
        };
      integrationError.value = "";
      window.location.assign(endSessionUrl ?? appRoot());
      return { ok: true };
    }
    if (response.status === 401) return { ok: true };
    return {
      ok: false,
      message:
        response.status === 400 || response.status === 403
          ? signOutNeedsReload
          : response.status === 503
            ? signOutUnavailable
            : signOutFailed,
    };
  } catch {
    return { ok: false, message: signOutFailed };
  } finally {
    integrationSigningOut.value = false;
  }
}
