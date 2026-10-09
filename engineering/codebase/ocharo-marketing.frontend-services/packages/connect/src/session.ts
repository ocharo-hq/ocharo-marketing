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
let csrfToken: string | null = null;
let identity: string | null = null;
let generation = 0;
let refreshing: Promise<void> | undefined;
const unavailable =
  "Connection status is unavailable. Your unsaved work stays here.";
const expired =
  "Your connection expired. Reconnect from Platform; your unsaved work stays here.";

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
function expire(message: string) {
  generation++;
  csrfToken = null;
  if (integrationSession.value)
    integrationSession.value = {
      ...integrationSession.value,
      active: false,
      canEdit: false,
      canApprove: false,
    };
  integrationError.value = message;
}
async function readSession() {
  const version = generation;
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
      integrationMode.value = "legacy";
      integrationSession.value = value as unknown as IntegrationSession;
    }
    integrationError.value = "";
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
  if (response.status === 401) expire(expired);
  return response;
};
