<script setup lang="ts">
import { onScopeDispose, ref } from "vue";
import { integrationFetch } from "./session";
defineOptions({ name: "BuildVersion" });
defineProps<{ appVersion: string; serviceLabel: string }>();
const serviceVersion = ref(""),
  loading = ref(false),
  failed = ref(false);
let controller: AbortController | undefined;
async function inspect(event: Event): Promise<void> {
  if (!(event.target as HTMLDetailsElement).open || loading.value) return;
  controller?.abort();
  controller = new AbortController();
  const current = controller;
  loading.value = true;
  failed.value = false;
  try {
    const response = await integrationFetch("/api/system/status", {
      signal: current.signal,
      cache: "no-store",
    });
    const data: unknown = response.ok ? await response.json() : null;
    if (current.signal.aborted) return;
    if (
      !data ||
      typeof data !== "object" ||
      !("version" in data) ||
      typeof data.version !== "string"
    )
      throw new Error();
    serviceVersion.value = data.version;
  } catch {
    if (!current.signal.aborted) {
      serviceVersion.value = "";
      failed.value = true;
    }
  } finally {
    if (!current.signal.aborted) loading.value = false;
  }
}
onScopeDispose(() => controller?.abort());
</script>
<template>
  <details class="relative text-xs text-muted-foreground" @toggle="inspect">
    <summary
      class="cursor-pointer list-none rounded px-1 py-0.5 focus-visible:outline"
      aria-label="About app versions"
      title="About app versions"
    >
      {{ appVersion }}
    </summary>
    <div
      class="absolute left-0 top-full z-50 mt-2 w-64 rounded-lg border border-border bg-card p-3 text-foreground shadow-lg"
    >
      <p class="font-semibold">{{ serviceLabel }} versions</p>
      <p class="mt-2 break-all">App: {{ appVersion }}</p>
      <p v-if="loading" role="status">Reading service version…</p>
      <p v-else-if="failed">
        Service version unavailable. Check your connection.
      </p>
      <p v-else class="mt-1 break-all">
        Service: {{ serviceVersion || "Unavailable" }}
      </p>
    </div>
  </details>
</template>
