import { ref } from "vue";
export interface IntegrationSession {
  mode: string;
  active: boolean;
  configured: boolean;
  canEdit?: boolean;
  canApprove?: boolean;
  expiresAt?: string;
}
export const integrationSession = ref<IntegrationSession | null>(null);
export const integrationError = ref("");
export async function refreshIntegration() {
  try {
    const response = await fetch("/api/integrations/session", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!response.ok) throw new Error("Connection status is unavailable.");
    integrationSession.value = (await response.json()) as IntegrationSession;
    if (integrationSession.value?.active) integrationError.value = "";
  } catch {
    integrationError.value =
      "Connection status is unavailable. Your unsaved work stays here.";
  }
}
export async function initializeIntegration() {
  const fragment = new URLSearchParams(window.location.hash.slice(1));
  const ticket = fragment.get("platformToken") ?? fragment.get("ocharo-ticket");
  if (ticket) {
    window.history.replaceState(
      window.history.state,
      "",
      window.location.pathname + window.location.search,
    );
    try {
      const response = await fetch("/api/integrations/connect", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ ticket }),
      });
      if (!response.ok) throw new Error();
    } catch {
      integrationError.value =
        "This connection expired or belongs to another organization. Reconnect from Platform.";
    }
  }
  await refreshIntegration();
}
export const integrationFetch: typeof fetch = async (input, init) => {
  const response = await globalThis.fetch(input, init);
  if (response.status === 401) {
    if (integrationSession.value)
      integrationSession.value = { ...integrationSession.value, active: false };
    integrationError.value =
      "Your connection expired. Reconnect from Platform; your unsaved work stays here.";
  }
  return response;
};
