<script lang="ts">
import type { CardCompositionModel } from '../../domain/marketing';

/** Defines the exact backend composition preview surface. */
export interface CardPreviewProps {
  /** The immutable request represented by the preview. */
  composition: CardCompositionModel;

  /** The accessible preview name. */
  label?: string;

  /** Whether to show advisory review notes. */
  showIssues?: boolean;
}
</script>

<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onScopeDispose, ref, useTemplateRef, watch } from 'vue';

import { useResizeObserver } from '@wow-two-beta/ui-vue/foundation/observers';

import { useMarketingWorkspace } from '../../application/marketing';
const { t: ui } = useUiLocale();

/** Displays self-contained backend artwork without granting script or origin access. */
defineOptions({ name: 'CardPreview' });
const props = withDefaults(defineProps<CardPreviewProps>(), { label: 'Post ko‘rinishi', showIssues: true });
const desk = useMarketingWorkspace();
const frame = useTemplateRef<HTMLElement>('frame');
const width = ref(300);
const html = ref('');
const issues = ref<string[]>([]);
const error = ref<string | null>(null);
const loading = ref(true);
let timer: ReturnType<typeof setTimeout> | undefined;
let controller: AbortController | undefined;
let requestRevision = 0;

/** The scaled dimensions preserve the export composition. */
const frameStyle = computed(() => ({ height: `${width.value * 1.25}px` }));
/** The frame scales the original 1080-pixel artwork. */
const artworkStyle = computed(() => ({ transform: `scale(${width.value / 1080})` }));

useResizeObserver(frame, (entry) => {
  width.value = entry.contentRect.width || 300;
});

/** Replaces the proof only when the latest composition finishes. */
watch(
  () => JSON.stringify(props.composition),
  () => {
    clearTimeout(timer);
    controller?.abort();
    requestRevision += 1;
    const revision = requestRevision;
    loading.value = true;
    error.value = null;
    timer = setTimeout(async () => {
      if (!props.composition.productId) {
        loading.value = false;
        return;
      }
      controller = new AbortController();
      const result = await desk.previewCard({ ...props.composition }, controller.signal);
      if (revision !== requestRevision || controller.signal.aborted) return;
      loading.value = false;
      if (result.ok) {
        html.value = result.value.html;
        issues.value = result.value.issues;
      } else if (result.failure.code !== 'cancelled') {
        html.value = '';
        error.value = desk.failureMessage(result.failure);
      }
    }, 200);
  },
  { immediate: true },
);

onScopeDispose(() => {
  clearTimeout(timer);
  controller?.abort();
  requestRevision += 1;
});
</script>

<template>
  <div class="min-w-0">
    <div ref="frame" class="relative overflow-hidden rounded-xl bg-muted" :style="frameStyle" :aria-busy="loading">
      <iframe
        v-if="html"
        :srcdoc="html"
        sandbox=""
        :title="ui(label)"
        width="1080"
        height="1350"
        class="pointer-events-none absolute left-0 top-0 origin-top-left border-0"
        :style="artworkStyle"
      />
      <div v-if="!html" class="absolute inset-0 flex flex-col items-center justify-center gap-3 p-5 text-center">
        <span v-if="loading" class="size-6 animate-spin rounded-full border-2 border-border border-t-primary" />
        <p class="text-sm text-muted-foreground">
          {{ error ?? (loading ? ui('Preparing preview…') : ui('Choose a product')) }}
        </p>
      </div>
      <span
        v-if="loading && html"
        class="absolute bottom-3 right-3 rounded-full bg-card px-3 py-1 text-xs text-muted-foreground"
      >
        {{ ui('Refreshing…') }}
      </span>
    </div>
    <ul v-if="showIssues && issues.length" class="mt-3 space-y-1 text-xs leading-relaxed text-warning">
      <li v-for="issue in issues" :key="issue">{{ ui(issue) }}</li>
    </ul>
    <p v-if="error && html" class="mt-2 text-sm text-destructive">{{ ui(error) }}</p>
  </div>
</template>
