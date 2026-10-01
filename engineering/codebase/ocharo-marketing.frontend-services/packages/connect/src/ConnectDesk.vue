<script setup lang="ts">
import { useUiLocale } from "./UseUiLocale";
import { onMounted, onUnmounted } from "vue";
import {
  integrationSession as session,
  integrationError as error,
  refreshIntegration,
} from "./session";
const { t: ui } = useUiLocale();

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
    :aria-label="ui('Platform connection')"
    class="connect-desk"
    :class="{ disconnected: !session?.active }"
  >
    <div>
      <strong>{{
        session?.active
          ? session.mode === "local"
            ? ui("Local workspace")
            : ui("Platform connected")
          : ui("Connect this desk")
      }}</strong>
      <p v-if="error" role="alert">{{ ui(error) }}</p>
      <p v-else-if="session && !session.configured">
        {{
          ui("This instance needs its Platform URL and organization binding.")
        }}
      </p>
      <p v-else-if="!session?.active">
        {{
          ui(
            "Open Connected apps in Platform, choose this desk, then return here. Unsaved edits stay open.",
          )
        }}
      </p>
      <p v-else>
        {{
          session.canApprove
            ? ui("Editing and approval access")
            : session.canEdit
              ? ui("Editing access")
              : ui("Read access")
        }}
      </p>
    </div>
    <a :href="props.platformUrl" target="_blank" rel="noopener noreferrer">{{
      ui("Open Platform ↗")
    }}</a>
    <button type="button" @click="refreshIntegration">
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
