import { getCurrentScope, onScopeDispose, ref } from "vue";

/** The placeholders the version row shows; English sources the frame translates. */
export const ServiceVersionState = {
  NotChecked: "Not checked",
  Checking: "Checking…",
  Unavailable: "Unavailable",
} as const;

/** How long one status read may take. */
export const ServiceVersionTimeoutMs = 5_000;

/** Reads the API's informational version from its public status; this never carries a session or a ticket. */
export async function readServiceVersion(signal: AbortSignal): Promise<string> {
  try {
    const response = await fetch("/api/system/status", {
      credentials: "omit",
      cache: "no-store",
      signal: AbortSignal.any([signal, AbortSignal.timeout(ServiceVersionTimeoutMs)]),
    });
    if (!response.ok) return ServiceVersionState.Unavailable;
    const data: unknown = await response.json();
    return typeof data === "object" && data !== null && "version" in data && typeof data.version === "string"
      ? data.version
      : ServiceVersionState.Unavailable;
  } catch {
    return ServiceVersionState.Unavailable;
  }
}

/**
 * The API's version for the account menu, read only when `refresh` asks (the menu opening) and cancelled with the
 * owning scope. A later read wins over an earlier one still in flight.
 */
export function useServiceVersion() {
  const version = ref<string>(ServiceVersionState.NotChecked);
  const controller = new AbortController();
  let generation = 0;
  if (getCurrentScope()) onScopeDispose(() => controller.abort());
  async function refresh(): Promise<void> {
    const current = ++generation;
    version.value = ServiceVersionState.Checking;
    const value = await readServiceVersion(controller.signal);
    if (!controller.signal.aborted && current === generation) version.value = value;
  }
  return { version, refresh };
}
