<script setup lang="ts">
import { onMounted, onScopeDispose, ref } from "vue";
import { SplashScreen } from "@wow-two-beta/fe/presentation/feedback";
import { useUiLocale } from "./UseUiLocale";

/**
 * Covers an Ocharo app's first load: the Ocharo wordmark above the boot bar on the theme background. It renders what
 * the static twin in the app's `index.html` draws (styled by its `public/boot-splash.css`, whose wordmark classes it
 * shares), replaces that twin as the app mounts, and lifts once the bar is full. An app mounts only after its first
 * route and its session are ready, so mounting is the last boot stage.
 */
defineOptions({ name: "BootSplash" });
const props = withDefaults(
  defineProps<{
    /** The app's base path, which serves the wordmark files from its `public/`. */
    base?: string;
  }>(),
  { base: "/" },
);
/** The boot stages the bar reports: the static twin holds at `Scripts`; a mounted app is `Ready`. */
const BootProgress = { Scripts: 35, Ready: 100 } as const;
/** Lets the bar's width transition finish before the splash fades. */
const SettleMs = 300;
const { t } = useUiLocale();
const progress = ref<number>(BootProgress.Scripts);
const isOpen = ref(true);
let frame: number | undefined;
let timer: ReturnType<typeof setTimeout> | undefined;
onMounted(() => {
  // One frame at the twin's stage first, so the bar grows from where the static page left it.
  frame = requestAnimationFrame(() => {
    progress.value = BootProgress.Ready;
    timer = setTimeout(() => {
      isOpen.value = false;
    }, SettleMs);
  });
});
onScopeDispose(() => {
  if (frame !== undefined) cancelAnimationFrame(frame);
  clearTimeout(timer);
});
</script>

<template>
  <SplashScreen
    :is-open="isOpen"
    :value="progress"
    is-overlay
    :label="t('Loading…')"
  >
    <template #logo>
      <img
        class="boot-splash-wordmark boot-splash-wordmark-ink"
        :src="`${props.base}ocharo-wordmark-ink.png`"
        alt="Ocharo"
        width="119"
        height="40"
      />
      <img
        class="boot-splash-wordmark boot-splash-wordmark-reverse"
        :src="`${props.base}ocharo-wordmark-reverse.png`"
        alt="Ocharo"
        width="119"
        height="40"
      />
    </template>
  </SplashScreen>
</template>
