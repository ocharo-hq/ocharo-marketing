<script setup lang="ts">
import { onMounted, onUnmounted } from "vue";
import {
  integrationSession as session,
  integrationError as error,
  refreshIntegration,
} from "./session";
const props = defineProps<{ platformUrl: string }>();
let timer: ReturnType<typeof setInterval> | undefined;
onMounted(() => {
  timer = setInterval(() => void refreshIntegration(), 30_000);
  window.addEventListener("focus", refreshIntegration);
});
onUnmounted(() => {
  if (timer) clearInterval(timer);
  window.removeEventListener("focus", refreshIntegration);
});
</script>
<template>
  <section
    aria-label="Platform connection"
    class="connect-desk"
    :class="{ disconnected: !session?.active }"
  >
    <div>
      <strong>{{
        session?.active
          ? session.mode === "local"
            ? "Local workspace"
            : "Platform connected"
          : "Connect this desk"
      }}</strong>
      <p v-if="error" role="alert">{{ error }}</p>
      <p v-else-if="session && !session.configured">
        This instance needs its Platform URL and organization binding.
      </p>
      <p v-else-if="!session?.active">
        Open Connected apps in Platform, choose this desk, then return here.
        Unsaved edits stay open.
      </p>
      <p v-else>
        {{
          session.canApprove
            ? "Editing and approval access"
            : session.canEdit
              ? "Editing access"
              : "Read access"
        }}
      </p>
    </div>
    <a :href="props.platformUrl" target="_blank" rel="noopener noreferrer"
      >Open Platform ↗</a
    >
    <button type="button" @click="refreshIntegration">Check connection</button>
  </section>
</template>
<style scoped>
.connect-desk {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
  border: 1px solid #cad8cf;
  background: #f3f8f3;
  color: #233d31;
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
  border-color: #d7b584;
  background: #fff8ed;
  color: #654b28;
}
</style>
