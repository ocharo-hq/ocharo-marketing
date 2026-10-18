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
import { requestFailureMessage } from '@ocharomarketing/connect/RequestFeedback';
import { useErrorToast } from '../../application/shared/UseFeedback';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onScopeDispose, ref, useTemplateRef, watch } from 'vue';

import { useResizeObserver } from '@wow-two-beta/fe/foundation/observers';

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
const retry = ref(0);
useErrorToast(error);
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
  () => [JSON.stringify(props.composition), retry.value],
  () => {
    clearTimeout(timer);
    controller?.abort();
    requestRevision += 1;
    const revision = requestRevision;
    loading.value = true;
    error.value = null;
    timer = setTimeout(async () => {
      if (!props.composition.productId) {
        html.value = '';
        issues.value = [];
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
        error.value = requestFailureMessage(result.failure);
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
      <div v-if="loading" class="absolute inset-0 animate-pulse bg-muted/95 p-5" :aria-label="ui('Preview loading')">
        <div class="h-2/3 rounded-xl bg-background/60" />
        <div class="mt-5 h-3 w-3/4 rounded bg-background/60" />
        <div class="mt-3 h-3 w-1/2 rounded bg-background/60" />
      </div>
      <div v-else-if="!html" class="absolute inset-0 flex flex-col items-center justify-center gap-3 p-5 text-center">
        <p class="text-sm text-muted-foreground">{{ error ? ui('Preview unavailable') : ui('Choose a product') }}</p>
        <Button v-if="error" variant="outline" @click="retry++">{{ ui('Try again') }}</Button>
      </div>
    </div>
    <ul v-if="showIssues && issues.length" class="mt-3 space-y-1 text-xs leading-relaxed text-warning">
      <li v-for="issue in issues" :key="issue">{{ ui(issue) }}</li>
    </ul>
    <Button v-if="error && html && !loading" class="mt-3" variant="outline" @click="retry++">{{
      ui('Retry preview')
    }}</Button>
  </div>
</template>
