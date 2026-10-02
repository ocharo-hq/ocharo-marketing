<script setup lang="ts">
import { useErrorToast } from '../../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { FileSearch, ArrowUpRight, RefreshCw } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Guid } from '@wow-two-beta/ui-vue/foundation/identifiers';
import {
  CheckboxInput,
  TextAreaInput,
  SelectPicker,
  SelectPickerContent,
  SelectPickerItem,
  SelectPickerValue,
} from '@wow-two-beta/ui-vue/presentation/forms';
import SelectPickerTrigger from './compatibility/PickerTrigger.vue';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import StatusIndicator from './StatusIndicator.vue';
import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type SourceAnalysisModel } from '../../domain/marketing';
const { t: ui } = useUiLocale();

/** Keeps AI image wording source-linked until an operator reviews and reuses it. */
defineOptions({ name: 'SourceImageAnalysisPanel' });
const desk = useMarketingWorkspace();
const selectedAssets = ref<string[]>([]);
const locale = ref<'uz' | 'ru' | 'en'>('uz');
const prompt = ref('');
const records = ref<SourceAnalysisModel[]>([]);
const selectedId = ref<string | null>(null);
const reviewText = ref('');
const localError = ref<string | null>(null);
useErrorToast(localError);
const loading = ref(false);
const analysisBusy = ref(false);
const reviewBusy = ref(false);
const operationId = ref<string | null>(null);
let controller: AbortController | null = null;
let loadVersion = 0;
const images = computed(
  () => desk.workspace.value?.assets.filter((item) => item.contentType.startsWith('image/')) ?? [],
);
const providerReady = computed(
  () => desk.workspace.value?.connections.find((item) => item.id === 'openai')?.status === 'configured',
);
const selected = computed(() => records.value.find((item) => item.id === selectedId.value));
const sourcesCurrent = computed(
  () =>
    selected.value?.sources.every((source) =>
      desk.workspace.value?.assets.some((asset) => asset.id === source.assetId && asset.sha256 === source.sha256),
    ) ?? false,
);
const dirty = computed(
  () => reviewText.value !== (selected.value?.reviewedText ?? selected.value?.suggestedText ?? ''),
);
const canReview = computed(
  () =>
    selected.value !== undefined &&
    ['ready', 'reviewed'].includes(selected.value.status) &&
    reviewText.value.trim().length > 0 &&
    sourcesCurrent.value,
);
const canReuse = computed(
  () =>
    selected.value?.status === 'reviewed' &&
    selected.value.reviewedText !== null &&
    !dirty.value &&
    sourcesCurrent.value,
);
watch(
  () => JSON.stringify({ assets: selectedAssets.value, locale: locale.value, prompt: prompt.value }),
  () => {
    operationId.value = null;
  },
  { flush: 'sync' },
);
/** An unchanged request retains its ID even if the HTTP response was lost. */
/** Selecting a record explicitly replaces only the local review editor. */
function choose(record: SourceAnalysisModel) {
  selectedId.value = record.id;
  reviewText.value = record.reviewedText ?? record.suggestedText ?? '';
  localError.value = null;
}
/** A cache read never invokes an image analysis provider. */
async function load() {
  controller?.abort();
  controller = new AbortController();
  const version = ++loadVersion;
  loading.value = true;
  const result = await desk.getSourceAnalyses(controller.signal);
  if (version !== loadVersion) return;
  if (result.ok) {
    records.value = result.value;
    if (!selectedId.value && result.value[0]) choose(result.value[0]);
  } else if (result.failure.code !== 'cancelled') localError.value = desk.failureMessage(result.failure);
  loading.value = false;
}
void load();
onBeforeUnmount(() => {
  loadVersion++;
  controller?.abort();
});
/** Image selection is bounded to the broker's supported three sources. */
function toggle(id: string, value: boolean) {
  selectedAssets.value = value ? [...selectedAssets.value, id] : selectedAssets.value.filter((item) => item !== id);
}
/** The only action that can make a paid provider analysis request. */
async function analyze() {
  if (!providerReady.value || analysisBusy.value || selectedAssets.value.length < 1 || selectedAssets.value.length > 3)
    return;
  if (prompt.value.length > 2000) {
    localError.value = 'Izoh 2000 belgidan oshmasin.';
    return;
  }
  const total = images.value
    .filter((item) => selectedAssets.value.includes(item.id))
    .reduce((sum, item) => sum + item.byteLength, 0);
  if (total > 12582912) {
    localError.value = 'Tanlangan tasvirlar jami 12 MiB dan oshmasin.';
    return;
  }
  operationId.value ??= Guid.createV4();
  analysisBusy.value = true;
  localError.value = null;
  try {
    const result = await desk.analyzeSources({
      operationId: operationId.value,
      assetIds: [...selectedAssets.value],
      provider: 'openai',
      locale: locale.value,
      prompt: prompt.value || undefined,
    });
    if (result.ok) {
      await load();
      const current = records.value.find(item => item.id === result.value.id);
      if (current) choose(current);
    } else localError.value = desk.failureMessage(result.failure);
  } finally {
    analysisBusy.value = false;
  }
}
/** A delayed review response updates evidence without erasing newer local edits. */
async function saveReview() {
  const record = selected.value;
  if (!record || !canReview.value || reviewBusy.value) return;
  const submitted = reviewText.value;
  reviewBusy.value = true;
  localError.value = null;
  try {
    const result = await desk.reviewSourceAnalysis(record.id, record.revision, submitted);
    if (result.ok) {
      await load();
      const current = records.value.find(item => item.id === record.id);
      if (current && selectedId.value === record.id && reviewText.value === submitted)
        reviewText.value = current.reviewedText ?? submitted;
    } else localError.value = desk.failureMessage(result.failure);
  } finally {
    reviewBusy.value = false;
  }
}
/** Reuse offers saved wording in the post editor without approving or saving a post. */
function reuse() {
  const record = selected.value;
  if (!record || !canReuse.value || !record.reviewedText) return;
  desk.reviewedCopy.value = { analysisId: record.id, revision: record.revision, text: record.reviewedText };
  desk.navigate('/posts/new');
}
</script>
<template>
  <div class="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
    <section class="rounded-2xl border border-border bg-card p-5 sm:p-6">
      <div class="flex items-center gap-3">
        <Icon :icon="FileSearch" :size="20" class="text-primary" />
        <h2 class="font-editorial text-2xl">{{ ui('Post idea from an image') }}</h2>
      </div>
      <p class="mt-3 text-sm leading-7 text-muted-foreground">
        {{
          ui(
            'Choose 1–3 source images. Visible details and caption ideas are suggested. Images do not verify fabric composition, sizes or prices.',
          )
        }}
      </p>
      <fieldset :disabled="analysisBusy" class="mt-5 space-y-5">
        <legend class="sr-only">{{ ui('Image analysis request') }}</legend>
        <div class="grid max-h-80 grid-cols-2 gap-3 overflow-y-auto sm:grid-cols-3">
          <label
            v-for="asset in images"
            :key="asset.id"
            class="min-w-0 cursor-pointer rounded-xl border border-border p-3"
            ><img :src="asset.url" :alt="asset.fileName" class="h-24 w-full bg-muted object-contain" /><span
              class="mt-2 flex items-start gap-2"
              ><CheckboxInput
                :model-value="selectedAssets.includes(asset.id)"
                :disabled="analysisBusy || (!selectedAssets.includes(asset.id) && selectedAssets.length >= 3)"
                @update:model-value="(value) => toggle(asset.id, value)"
              /><span class="break-all text-[10px] leading-5">{{ asset.fileName }}</span></span
            ></label
          >
        </div>
        <p v-if="!images.length" class="text-xs text-muted-foreground">
          {{ ui('Add the source image to the library first.') }}
        </p>
        <div>
          <label class="mb-2 block text-xs font-semibold">{{ ui('Language') }}</label
          ><SelectPicker
            v-model="locale"
            :options="[
              { itemKey: 'uz', value: 'uz', label: 'O‘zbekcha' },
              { itemKey: 'ru', value: 'ru', label: 'Русский' },
              { itemKey: 'en', value: 'en', label: 'English' },
            ]"
            ><SelectPickerTrigger :aria-label="ui('Analysis language')"><SelectPickerValue /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem item-key="uz" label="O‘zbekcha" /><SelectPickerItem
                item-key="ru"
                label="Русский" /><SelectPickerItem item-key="en" :label="ui('English')" /></SelectPickerContent
          ></SelectPicker>
        </div>
        <div>
          <label for="analysis-prompt" class="mb-2 block text-xs font-semibold">{{ ui('What needs attention?') }}</label
          ><TextAreaInput
            id="analysis-prompt"
            v-model="prompt"
            :rows="3"
            :placeholder="ui('For example: turn colour and cut into a short post caption.')"
          />
        </div>
      </fieldset>
      <p class="mt-4 rounded-lg bg-muted p-3 text-xs leading-6 text-muted-foreground">
        {{
          providerReady
            ? ui('OpenAI · This button may start paid image analysis. Results are not accepted automatically.')
            : ui('OpenAI is not configured. Analysis is unavailable. Refreshing status reads saved settings only.')
        }}
      </p>
      <Button
        class="mt-4"
        :is-disabled="
          !providerReady || selectedAssets.length < 1 || selectedAssets.length > 3 || desk.busy.value !== null
        "
        :is-loading="analysisBusy"
        @click="analyze"
        >{{ ui('Analyse images') }}</Button
      >
      <Button
        variant="ghost"
        size="sm"
        class="mt-3"
        :is-disabled="analysisBusy || reviewBusy"
        @click="desk.refresh()"
        >{{ ui('Refresh service status') }}</Button
      >
      <p class="mt-3 text-[11px] leading-5 text-muted-foreground">
        {{
          ui(
            'Repeating an unchanged request does not create a new paid analysis. Changing the source or prompt starts a new request.',
          )
        }}
      </p>
    </section>
    <section class="rounded-2xl border border-border bg-card p-5 sm:p-6">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h2 class="font-editorial text-2xl">{{ ui('Review suggestion') }}</h2>
        <Button size="sm" variant="ghost" :is-disabled="loading || analysisBusy || reviewBusy" @click="load"
          ><Icon :icon="RefreshCw" :size="14" class="mr-1" />{{ ui('Saved history') }}</Button
        >
      </div>

      <div v-if="records.length" class="mt-4 flex max-h-40 flex-wrap gap-2 overflow-y-auto">
        <button
          v-for="record in records"
          :key="record.id"
          :aria-pressed="selectedId === record.id"
          :disabled="reviewBusy"
          :class="[
            'rounded-lg border px-3 py-2 text-left text-[11px]',
            selectedId === record.id ? 'border-primary bg-primary-soft' : 'border-border',
          ]"
          @click="choose(record)"
        >
          {{ record.sources[0]?.fileName ?? ui('Analysis') }} · {{ ui(MarketingExtensions.statusLabel(record.status)) }}
        </button>
      </div>
      <div v-if="selected" class="mt-5 space-y-4">
        <div class="flex flex-wrap items-center gap-3">
          <StatusIndicator :status="selected.status" />
          <p class="text-[11px] text-muted-foreground">
            {{ selected.provider }} · {{ selected.model ?? ui('Model unavailable') }} · {{ selected.revision
            }}{{ ui(' revision') }}
          </p>
        </div>
        <p class="text-xs leading-6 text-muted-foreground">{{ ui('Needs attention') }}</p>
        <details class="rounded-lg bg-muted p-3 text-xs">
          <summary class="cursor-pointer font-semibold">{{ ui('Sources ·') }} {{ selected.sources.length }}</summary>
          <div v-for="source in selected.sources" :key="source.assetId" class="mt-3">
            <p class="break-all font-semibold">{{ source.fileName }}</p>
            <p class="mt-1 leading-6 text-muted-foreground">{{ source.provenance }}</p>
            <p class="break-all text-[10px] text-muted-foreground">SHA-256: {{ source.sha256 }}</p>
          </div>
        </details>
        <p v-if="!sourcesCurrent" class="text-xs leading-6 text-warning">
          {{ ui('The source has changed or the image is unavailable. This suggestion cannot be accepted now.') }}
        </p>
        <div v-if="selected.suggestedText">
          <h3 class="text-xs font-semibold">{{ ui('Original AI suggestion') }}</h3>
          <p class="mt-2 whitespace-pre-wrap break-words rounded-lg bg-muted p-4 text-sm leading-7">
            {{ selected.suggestedText }}
          </p>
        </div>
        <div v-if="['ready', 'reviewed'].includes(selected.status)">
          <label for="review-text" class="mb-2 block text-xs font-semibold">{{ ui('Your reviewed text') }}</label
          ><TextAreaInput id="review-text" v-model="reviewText" :rows="7" />
          <p class="mt-2 text-[11px] leading-6 text-muted-foreground">
            {{ ui('Check that it matches the product. This text does not change the catalogue.') }}
          </p>
          <div class="mt-4 flex flex-wrap gap-3">
            <Button
              :is-disabled="!canReview || desk.busy.value !== null"
              :is-loading="reviewBusy"
              @click="saveReview"
              >{{ ui('Save reviewed text') }}</Button
            ><Button variant="outline" :is-disabled="!canReuse || reviewBusy" @click="reuse"
              >{{ ui('Use in post') }} <Icon :icon="ArrowUpRight" :size="14" class="ml-1"
            /></Button>
          </div>
          <p v-if="dirty" class="mt-3 text-xs text-warning">
            {{ ui('The text changed. Save the reviewed text before using it in a post.') }}
          </p>
        </div>
        <p v-if="selected.status === 'pending'" class="text-xs leading-6 text-warning">
          {{ ui('This request has started. A paid request with an unknown outcome is not retried automatically.') }}
        </p>
      </div>
      <p v-else class="mt-8 text-sm leading-7 text-muted-foreground">
        {{ ui('No saved analysis yet. Start with the selected image.') }}
      </p>
    </section>
  </div>
</template>
