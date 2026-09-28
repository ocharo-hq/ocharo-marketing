<script setup lang="ts">
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
import Button from './compatibility/ActionButton.vue';
import StatusIndicator from './StatusIndicator.vue';
import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type SourceAnalysisModel } from '../../domain/marketing';
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
function upsert(record: SourceAnalysisModel) {
  records.value = [record, ...records.value.filter((item) => item.id !== record.id)];
}
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
      upsert(result.value);
      choose(result.value);
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
      upsert(result.value);
      if (selectedId.value === record.id && reviewText.value === submitted)
        reviewText.value = result.value.reviewedText ?? submitted;
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
        <h2 class="font-editorial text-2xl">Tasvirdan post g‘oyasi</h2>
      </div>
      <p class="mt-3 text-sm leading-7 text-muted-foreground">
        1–3 manba tasvirni tanlang. Ko‘rinadigan belgilar va matn g‘oyasi taklif qilinadi. Mato tarkibi, o‘lcham va narx
        rasm bilan tasdiqlanmaydi.
      </p>
      <fieldset :disabled="analysisBusy" class="mt-5 space-y-5">
        <legend class="sr-only">Tasvir tahlili so‘rovi</legend>
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
        <p v-if="!images.length" class="text-xs text-muted-foreground">Avval manba tasvirini kutubxonaga qo‘shing.</p>
        <div>
          <label class="mb-2 block text-xs font-semibold">Til</label
          ><SelectPicker
            v-model="locale"
            :options="[
              { itemKey: 'uz', value: 'uz', label: 'O‘zbekcha' },
              { itemKey: 'ru', value: 'ru', label: 'Русский' },
              { itemKey: 'en', value: 'en', label: 'English' },
            ]"
            ><SelectPickerTrigger aria-label="Tahlil tili"><SelectPickerValue /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem item-key="uz" label="O‘zbekcha" /><SelectPickerItem
                item-key="ru"
                label="Русский" /><SelectPickerItem item-key="en" label="English" /></SelectPickerContent
          ></SelectPicker>
        </div>
        <div>
          <label for="analysis-prompt" class="mb-2 block text-xs font-semibold">Nimani e’tiborga olish kerak?</label
          ><TextAreaInput
            id="analysis-prompt"
            v-model="prompt"
            :rows="3"
            placeholder="Masalan: rang va bichimni qisqa post matniga aylantirish."
          />
        </div>
      </fieldset>
      <p class="mt-4 rounded-lg bg-muted p-3 text-xs leading-6 text-muted-foreground">
        {{
          providerReady
            ? 'OpenAI · Bu tugma pulli tasvir tahlilini boshlashi mumkin. Natija avtomatik qabul qilinmaydi.'
            : 'OpenAI hozir sozlanmagan. Tahlil mavjud emas. Holatni yangilash faqat saqlangan sozlamalarni o‘qiydi.'
        }}
      </p>
      <Button
        class="mt-4"
        :is-disabled="
          !providerReady || selectedAssets.length < 1 || selectedAssets.length > 3 || desk.busy.value !== null
        "
        :is-loading="analysisBusy"
        @click="analyze"
        >Tasvirlarni tahlil qilish</Button
      >
      <Button variant="ghost" size="sm" class="mt-3" :is-disabled="analysisBusy || reviewBusy" @click="desk.refresh()"
        >Xizmat holatini yangilash</Button
      >
      <p class="mt-3 text-[11px] leading-5 text-muted-foreground">
        O‘zgarmagan so‘rov qayta bosilsa yangi pulli tahlil yaratilmaydi. Manba yoki izohni o‘zgartirish yangi niyatni
        boshlaydi.
      </p>
    </section>
    <section class="rounded-2xl border border-border bg-card p-5 sm:p-6">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <h2 class="font-editorial text-2xl">Taklifni ko‘rib chiqish</h2>
        <Button size="sm" variant="ghost" :is-disabled="loading || analysisBusy || reviewBusy" @click="load"
          ><Icon :icon="RefreshCw" :size="14" class="mr-1" />Saqlangan tarix</Button
        >
      </div>
      <p v-if="localError" role="alert" class="mt-4 text-xs leading-6 text-destructive">{{ localError }}</p>
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
          {{ record.sources[0]?.fileName ?? 'Tahlil' }} · {{ MarketingExtensions.statusLabel(record.status) }}
        </button>
      </div>
      <div v-if="selected" class="mt-5 space-y-4">
        <div class="flex flex-wrap items-center gap-3">
          <StatusIndicator :status="selected.status" />
          <p class="text-[11px] text-muted-foreground">
            {{ selected.provider }} · {{ selected.model ?? 'Model mavjud emas' }} · {{ selected.revision }}-talqin
          </p>
        </div>
        <p class="text-xs leading-6 text-muted-foreground">{{ selected.message }}</p>
        <details class="rounded-lg bg-muted p-3 text-xs">
          <summary class="cursor-pointer font-semibold">Manbalar · {{ selected.sources.length }}</summary>
          <div v-for="source in selected.sources" :key="source.assetId" class="mt-3">
            <p class="break-all font-semibold">{{ source.fileName }}</p>
            <p class="mt-1 leading-6 text-muted-foreground">{{ source.provenance }}</p>
            <p class="break-all text-[10px] text-muted-foreground">SHA-256: {{ source.sha256 }}</p>
          </div>
        </details>
        <p v-if="!sourcesCurrent" class="text-xs leading-6 text-warning">
          Manba izi o‘zgargan yoki tasvir mavjud emas. Bu taklifni hozir qabul qilib bo‘lmaydi.
        </p>
        <div v-if="selected.suggestedText">
          <h3 class="text-xs font-semibold">Asl AI taklifi</h3>
          <p class="mt-2 whitespace-pre-wrap break-words rounded-lg bg-muted p-4 text-sm leading-7">
            {{ selected.suggestedText }}
          </p>
        </div>
        <div v-if="['ready', 'reviewed'].includes(selected.status)">
          <label for="review-text" class="mb-2 block text-xs font-semibold">Siz tekshirgan matn</label
          ><TextAreaInput id="review-text" v-model="reviewText" :rows="7" />
          <p class="mt-2 text-[11px] leading-6 text-muted-foreground">
            Mahsulotga mosligini tekshiring. Bu matn katalogni o‘zgartirmaydi.
          </p>
          <div class="mt-4 flex flex-wrap gap-3">
            <Button :is-disabled="!canReview || desk.busy.value !== null" :is-loading="reviewBusy" @click="saveReview"
              >Tekshirilgan matnni saqlash</Button
            ><Button variant="outline" :is-disabled="!canReuse || reviewBusy" @click="reuse"
              >Postda ishlatish <Icon :icon="ArrowUpRight" :size="14" class="ml-1"
            /></Button>
          </div>
          <p v-if="dirty" class="mt-3 text-xs text-warning">
            Matn o‘zgargan. Postda ishlatishdan oldin tekshirilgan matnni saqlang.
          </p>
        </div>
        <p v-if="selected.status === 'pending'" class="text-xs leading-6 text-warning">
          Bu so‘rov boshlangan. Holati noma’lum bo‘lsa pulli so‘rov avtomatik takrorlanmaydi.
        </p>
      </div>
      <p v-else class="mt-8 text-sm leading-7 text-muted-foreground">
        Hali saqlangan tahlil yo‘q. Tanlangan tasvirdan boshlang.
      </p>
    </section>
  </div>
</template>
