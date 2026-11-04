<script setup lang="ts">
import { useUiLocale } from "./UseUiLocale";
import { computed, onMounted, onUnmounted } from "vue";
import {
  integrationSession as session,
  integrationError as error,
  refreshIntegration,
  integrationChecking,
  integrationIdentityChanged,
  integrationReturnPath,
  hasSignInError,
  signInUrl,
} from "./session";
const { t: ui } = useUiLocale();

// Signs in again at the page where the session expired, captured before any later navigation.
const loginUrl = computed(() =>
  signInUrl(integrationReturnPath.value ?? undefined),
);
const signedOut = computed(
  () => session.value !== null && !session.value.active,
);
const readOnly = computed(
  () => session.value?.active === true && !session.value.canEdit,
);
function refreshOnFocus() {
  if (session.value) void refreshIntegration();
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
    v-if="!session || signedOut || readOnly"
    :aria-label="ui('Ocharo connection')"
    class="session-connection"
    :class="{ disconnected: !session?.active }"
  >
    <div>
      <strong>{{
        !session
          ? ui("Check your Ocharo connection")
          : signedOut
            ? ui("Reconnect to Ocharo")
            : ui("Signed in to Ocharo")
      }}</strong>
      <p v-if="error" role="alert">{{ ui(error) }}</p>
      <p v-else-if="signedOut && hasSignInError()">
        {{ ui("Sign-in did not complete. Try again.") }}
      </p>
      <p v-else-if="signedOut">
        {{ ui("Use your Ocharo account across all apps.") }}
      </p>
      <p v-else-if="readOnly">{{ ui("You have read-only access.") }}</p>
      <p v-if="signedOut && !integrationIdentityChanged">
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
      v-else-if="signedOut"
      :href="loginUrl"
      target="_blank"
      rel="noopener noreferrer"
      >{{ ui("Sign in to Ocharo") }}</a
    >
    <button
      v-if="!readOnly"
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
.session-connection {
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
.session-connection div {
  flex: 1;
  min-width: 200px;
}
.session-connection p {
  margin: 0.25rem 0 0;
  max-width: 70ch;
}
.session-connection a,
.session-connection button {
  text-decoration: underline;
  text-underline-offset: 3px;
  cursor: pointer;
}
.disconnected {
  border-color: var(--color-border, #d7b584);
}
</style>
