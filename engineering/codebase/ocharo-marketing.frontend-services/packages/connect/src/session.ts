import { computed, ref } from "vue";
export interface IntegrationSession {
  mode: string;
  active: boolean;
  configured: boolean;
  canEdit?: boolean;
  canApprove?: boolean;
  expiresAt?: string;
}
interface SsoSession {
  enabled: boolean;
  authenticated: boolean;
  userId?: string;
  organizationId?: string;
  canEdit?: boolean;
  canApprove?: boolean;
  csrfToken?: string;
  accountUrl?: unknown;
  name?: unknown;
  email?: unknown;
}
/**
 * Who is signed in through Ocharo Desk: the operator's current Desk display name and email, each `null` when Desk
 * sends none (an older Desk, or a blank profile field). `accountUrl` is Desk's own origin, where the operator
 * manages the account.
 */
export interface IntegrationAccount {
  readonly userId: string;
  readonly name: string | null;
  readonly email: string | null;
  readonly accountUrl: string | null;
}
export const integrationSession = ref<IntegrationSession | null>(null);
export const integrationError = ref("");
export const integrationMode = ref<"unknown" | "legacy" | "sso">("unknown");
export const integrationChecking = ref(false);
export const integrationIdentityChanged = ref(false);
export const integrationAccess = computed(
  () =>
    integrationMode.value === "legacy" ||
    integrationSession.value?.active === true,
);
/** The app path a reconnect returns to, captured when the signed-in session expired or was signed out. */
export const integrationReturnPath = ref<string | null>(null);
/** The signed-in Ocharo account, while an SSO session is active; `null` otherwise. */
export const integrationAccount = ref<IntegrationAccount | null>(null);
/** True while `signOutIntegration` waits for the server. */
export const integrationSigningOut = ref(false);
/** Describes one expiry of a signed-in session, in the shape of the SDK's `subscribeExpired` event. */
export interface IntegrationExpiry {
  readonly returnUrl: string;
}
const expiryListeners = new Set<(expiry: IntegrationExpiry) => void>();
let csrfToken: string | null = null;
let identity: string | null = null;
let generation = 0;
let refreshing: Promise<void> | undefined;
const unavailable =
  "Connection status is unavailable. Your unsaved work stays here.";
const expired =
  "Your connection expired. Reconnect from Platform; your unsaved work stays here.";
const signOutFailed = "Could not sign out. Try again.";
const signOutNeedsReload =
  "This tab could not confirm its Ocharo sign-in. Reload the app, then sign out.";
const signOutUnavailable =
  "Ocharo Desk could not end this sign-in. Your work stays here; try again shortly.";

function record(value: unknown): Record<string, unknown> | null {
  return value !== null && typeof value === "object" && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}
function decodeSso(value: unknown): SsoSession {
  const data = record(record(value)?.data);
  if (
    !data ||
    typeof data.enabled !== "boolean" ||
    typeof data.authenticated !== "boolean"
  )
    throw new Error(unavailable);
  if (
    data.enabled &&
    data.authenticated &&
    (![data.userId, data.organizationId, data.csrfToken].every(
      (item) => typeof item === "string" && item.length > 0,
    ) ||
      typeof data.canEdit !== "boolean" ||
      typeof data.canApprove !== "boolean")
  )
    throw new Error(unavailable);
  return data as unknown as SsoSession;
}
/** Keeps a display name or email only as nonblank text; anything else reads as absent, never as a failed session. */
function displayText(value: unknown): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text.length > 0 ? text : null;
}
/** Accepts Desk's account address only as a plain HTTPS origin, the shape the server sends. */
function safeAccountUrl(value: unknown): string | null {
  try {
    const url = new URL(String(value));
    return url.protocol === "https:" &&
      !url.username &&
      !url.password &&
      !url.search &&
      !url.hash &&
      url.pathname === "/"
      ? url.href
      : null;
  } catch {
    return null;
  }
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
 * signed out. The app stays on its page so unsaved edits survive; the reconnect link returns to `returnUrl`.
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
    const response = await fetch("/api/sso/session", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error(unavailable);
    const session = decodeSso(await response.json());
    if (version !== generation) return;
    if (session.enabled) {
      integrationMode.value = "sso";
      const nextIdentity = session.authenticated
        ? `${session.organizationId}:${session.userId}`
        : null;
      if (identity && nextIdentity && identity !== nextIdentity) {
        integrationIdentityChanged.value = true;
        expire(
          "Your Ocharo account changed. Reload this app before continuing.",
        );
        return;
      }
      if (integrationIdentityChanged.value) return;
      identity ??= nextIdentity;
      csrfToken = session.authenticated ? session.csrfToken! : null;
      integrationAccount.value = session.authenticated
        ? {
            userId: session.userId!,
            name: displayText(session.name),
            email: displayText(session.email),
            accountUrl: safeAccountUrl(session.accountUrl),
          }
        : null;
      wasActive = integrationSession.value?.active === true;
      integrationSession.value = {
        mode: "sso",
        configured: true,
        active: session.authenticated,
        canEdit: session.authenticated && session.canEdit,
        canApprove: session.authenticated && session.canApprove,
      };
    } else {
      // Only an explicit disabled response enables the legacy connection flow.
      const legacy = await fetch("/api/integrations/session", {
        credentials: "same-origin",
        cache: "no-store",
      });
      if (!legacy.ok) throw new Error(unavailable);
      const value = record(await legacy.json());
      if (
        !value ||
        typeof value.mode !== "string" ||
        typeof value.active !== "boolean" ||
        typeof value.configured !== "boolean"
      )
        throw new Error(unavailable);
      if (version !== generation) return;
      if (identity) {
        integrationIdentityChanged.value = true;
        expire(
          "Your Ocharo account changed. Reload this app before continuing.",
        );
        return;
      }
      csrfToken = null;
      integrationAccount.value = null;
      integrationMode.value = "legacy";
      wasActive = integrationSession.value?.active === true;
      integrationSession.value = value as unknown as IntegrationSession;
    }
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
/** Only local app paths can be handed to the server's redirect flow. */
export function ssoLoginUrl(
  returnPath = window.location.pathname + window.location.search,
): string {
  const valid =
    /^\/(?!\/)/.test(returnPath) &&
    !/[\\%\u0000-\u0020\u007f]/.test(returnPath) &&
    !/^\/api(?:[/?#]|$)/i.test(returnPath);
  const fallback = window.location.pathname.startsWith("/studio")
    ? "/studio/"
    : "/";
  return `/api/sso/login?returnUrl=${encodeURIComponent(valid ? returnPath : fallback)}`;
}
/** Returns false when the browser is leaving for its initial central sign-in. */
export async function initializeIntegration(): Promise<boolean> {
  const fragment = new URLSearchParams(window.location.hash.slice(1));
  const ticket = fragment.get("platformToken") ?? fragment.get("ocharo-ticket");
  if (ticket)
    window.history.replaceState(
      window.history.state,
      "",
      window.location.pathname + window.location.search,
    );
  await refreshIntegration();
  if (
    integrationMode.value === "sso" &&
    integrationSession.value &&
    !integrationSession.value.active &&
    !integrationError.value
  ) {
    window.location.assign(ssoLoginUrl());
    return false;
  }
  if (ticket && integrationMode.value === "legacy") {
    try {
      const response = await fetch("/api/integrations/connect", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ticket }),
      });
      if (!response.ok) throw new Error();
      await refreshIntegration();
    } catch {
      integrationError.value =
        "This connection expired or belongs to another organization. Reconnect from Platform.";
    }
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
  headers.delete("X-CSRF-Token");
  const protectedWrite =
    !["GET", "HEAD", "OPTIONS"].includes(method) &&
    integrationMode.value !== "legacy";
  if (protectedWrite) {
    if (
      integrationMode.value !== "sso" ||
      !integrationSession.value?.active ||
      !csrfToken ||
      integrationIdentityChanged.value
    )
      throw new Error("Sign in again. Your edits are preserved.");
    headers.set("X-CSRF-Token", csrfToken);
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
  }
  return response;
};
/** The outcome of `signOutIntegration`: `message` is a display-safe English source for the operator. */
export type IntegrationSignOut =
  | { readonly ok: true }
  | { readonly ok: false; readonly message: string };
/**
 * Ends this app's Desk grant with `POST /api/sso/logout` and the in-memory CSRF token. On 204 the session reads signed
 * out without an expiry notice; the page, its fieldset-disabled editors and the reconnect link stay. A 401 means the
 * sign-in had already ended (the expiry flow reports it). A 400 or 503 leaves the session as it was.
 */
export async function signOutIntegration(): Promise<IntegrationSignOut> {
  if (integrationSigningOut.value) return { ok: false, message: "" };
  if (integrationMode.value !== "sso" || !integrationSession.value?.active)
    return { ok: false, message: signOutFailed };
  integrationSigningOut.value = true;
  try {
    const response = await integrationFetch("/api/sso/logout", {
      method: "POST",
      cache: "no-store",
    });
    if (response.ok) {
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
