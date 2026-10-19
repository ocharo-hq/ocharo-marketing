<script setup lang="ts">
import { useUiLocale } from "./UseUiLocale";
import { computed, onMounted, onUnmounted } from "vue";
import {
  integrationSession as session,
  integrationError as error,
  refreshIntegration,
  integrationMode,
  integrationChecking,
  integrationIdentityChanged,
  integrationReturnPath,
  ssoLoginUrl,
} from "./session";
const { t: ui } = useUiLocale();

const props = defineProps<{ platformUrl: string }>();
// Returns to the page where the session expired, captured before any later navigation.
const loginUrl = computed(() =>
  ssoLoginUrl(integrationReturnPath.value ?? undefined),
);
function refreshOnFocus() {
  if (integrationMode.value !== "unknown") void refreshIntegration();
}
let timer: ReturnType<typeof setInterval> | undefined;
onMounted(() => {
  timer = setInterval(() => {
    if (session.value?.active) void refreshIntegration();
  }, 30_000);
  window.addEventListener("focus", refreshOnFocus);
});
onUnmounted(() => {
  if (timer) clearInterval(timer);
  window.removeEventListener("focus", refreshOnFocus);
});
</script>
<template>
  <section
    :aria-label="ui('Platform connection')"
    class="connect-desk"
    :class="{ disconnected: !session?.active }"
  >
    <div>
      <strong>{{
        session?.active
          ? session.mode === "sso"
            ? ui("Signed in to Ocharo")
            : session.mode === "local"
              ? ui("Local workspace")
              : ui("Platform connected")
          : integrationMode === "sso"
            ? ui("Reconnect to Ocharo")
            : integrationMode === "legacy"
              ? ui("Connect this desk")
              : ui("Check your Ocharo connection")
      }}</strong>
      <p v-if="error" role="alert">{{ ui(error) }}</p>
      <p v-else-if="integrationMode === 'sso' && !session?.active">
        {{ ui("Use your Ocharo account across all apps.") }}
      </p>
      <p
        v-else-if="
          integrationMode === 'legacy' && session && !session.configured
        "
      >
        {{
          ui("This instance needs its Platform URL and organization binding.")
        }}
      </p>
      <p v-else-if="integrationMode === 'legacy' && !session?.active">
        {{
          ui(
            "Open Connected apps in Platform, choose this desk, then return here. Unsaved edits stay open.",
          )
        }}
      </p>
      <p v-else-if="session?.active">
        {{
          session.canApprove
            ? ui("Editing and approval access")
            : session.canEdit
              ? ui("Editing access")
              : ui("Read access")
        }}
      </p>
      <p
        v-if="
          integrationMode === 'sso' &&
          !session?.active &&
          !integrationIdentityChanged
        "
      >
        {{ ui("Reconnection opens a new tab. Your unsaved edits stay here.") }}
      </p>
    </div>
    <a
      v-if="integrationIdentityChanged"
      href=""
      target="_blank"
      rel="noopener noreferrer"
      >{{ ui("Open app in a new tab") }}</a
    >
    <a
      v-else-if="integrationMode === 'sso' && !session?.active"
      :href="loginUrl"
      target="_blank"
      rel="noopener noreferrer"
      >{{ ui("Reconnect") }}</a
    >
    <a
      v-else
      :href="props.platformUrl"
      target="_blank"
      rel="noopener noreferrer"
      >{{
        integrationMode === "sso"
          ? ui("Open Ocharo Desk")
          : ui("Open Platform ↗")
      }}</a
    >
    <button
      type="button"
      :disabled="integrationChecking"
      :aria-busy="integrationChecking"
      @click="refreshIntegration"
    >
      {{ ui("Check connection") }}
    </button>
  </section>
</template>
<style scoped>
.connect-desk {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
  border: 1px solid var(--color-border, #cad8cf);
  background: var(--color-card, #f3f8f3);
  color: var(--color-foreground, #233d31);
  border-radius: 14px;
  padding: 1rem 1.25rem;
  margin-bottom: 1.5rem;
  font-size: 0.8rem;
}
.connect-desk div {
  flex: 1;
  min-width: 200px;
}
.connect-desk p {
  margin: 0.25rem 0 0;
  max-width: 70ch;
}
.connect-desk a,
.connect-desk button {
  text-decoration: underline;
  text-underline-offset: 3px;
  cursor: pointer;
}
.disconnected {
  border-color: var(--color-border, #d7b584);
}
</style>
