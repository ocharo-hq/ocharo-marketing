<script setup lang="ts">
import SelectPickerTrigger from './compatibility/PickerTrigger.vue';
import { computed, reactive, ref, watch } from 'vue';
import { ArrowLeft, CalendarPlus, Check, Download, RotateCcw, Sparkles } from 'lucide-vue-next';

import { downloadBlob } from '@wow-two-beta/ui-vue/foundation/files';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import Button from './compatibility/ActionButton.vue';
import {
  DateTimeInput,
  NumberInput,
  SelectPicker,
  SelectPickerContent,
  SelectPickerItem,
  SelectPickerValue,
  TextAreaInput,
  TextInput,
  type DateTimeInputProps,
} from '@wow-two-beta/ui-vue/presentation/forms';
import {
  Modal,
  ModalContent,
  ModalDescription,
  ModalFooter,
  ModalHeader,
  ModalTitle,
} from '@wow-two-beta/ui-vue/presentation/overlays';
import { useNavigationBlocker } from '@wow-two-beta/ui-vue/router';

import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type DraftModel, type GenerationJobModel } from '../../domain/marketing';
import CardPreview from './CardPreview.vue';
import StatusIndicator from './StatusIndicator.vue';

/** Prepares one bounded card and its separately reviewed caption. */
defineOptions({ name: 'PostComposerPage' });
const props = defineProps<{ draftId?: string; productId?: string; presetId?: string }>();
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const original = workspace.value?.drafts.find((item) => item.id === props.draftId);
const selectedProduct =
  workspace.value?.products.find((item) => item.id === (original?.productId ?? props.productId)) ??
  workspace.value?.products[0];
const form = reactive({
  title: original?.composition?.title ?? original?.title ?? selectedProduct?.name ?? '',
  productId: selectedProduct?.id ?? '',
  presetId: original?.presetId ?? props.presetId ?? 't02-bone',
  locale: original?.locale ?? 'uz',
  caption: original?.caption ?? '',
  channels: original?.channels ?? ['instagram'],
  mediaIds: [...(original?.mediaIds ?? [])],
});
const currentId = ref(original?.id);
const revision = ref(original?.revision ?? 0);
const approvedRevision = ref(original?.approvedRevision ?? null);
const saved = ref('');
const subtitle = ref(original?.composition?.subtitle ?? '');
const fabricAssetId = ref(original?.composition?.fabricAssetId ?? '');
const scale = ref<number | null>(original?.composition?.scale ?? 1);
const offsetX = ref<number | null>(original?.composition?.offsetX ?? 0);
const offsetY = ref<number | null>(original?.composition?.offsetY ?? 0);
const renderJobId = ref<string | null>(null);
const renderedSignature = ref('');
const scheduleOpen = ref(false);
const scheduleTime = ref<DateTimeInputProps['modelValue']>(null);
const scheduleChannel = ref('instagram');
const suggestion = ref<string | null>(null);
const previousCaption = ref<string | null>(null);
/** Reviewed image wording is offered explicitly, never inserted or approved automatically. */
function useReviewedCopy() {
  const copy = desk.reviewedCopy?.value;
  if (!copy) return;
  previousCaption.value = form.caption;
  form.caption = copy.text;
  desk.reviewedCopy.value = null;
}

let restoring = false;

/** The selected product supplies facts without changing approval. */
const product = computed(() => workspace.value?.products.find((item) => item.id === form.productId));
/** The selected preset determines its input requirement. */
const preset = computed(() => workspace.value?.presets.find((item) => item.id === form.presetId));
/** The exact composition sent to both proof and authoritative rendering. */
const composition = computed(() => ({
  productId: form.productId,
  presetId: form.presetId,
  title: form.title,
  subtitle: subtitle.value,
  locale: form.locale,
  fabricAssetId: fabricAssetId.value || undefined,
  scale: scale.value ?? 1,
  offsetX: offsetX.value ?? 0,
  offsetY: offsetY.value ?? 0,
}));
/** The artwork signature detects changes after generating an image. */
const compositionSignature = computed(() => JSON.stringify(composition.value));
/** The editor tracks material changes against its last successful save. */
const dirty = computed(() => saved.value !== JSON.stringify({ form, composition: composition.value }));
saved.value = JSON.stringify({ form, composition: composition.value });
/** The current image can be reviewed as its actual original bytes. */
const selectedOutput = computed(() => workspace.value?.assets.find((asset) => asset.id === form.mediaIds[0]));
/** The render job is refreshed without replacing the editor. */
const renderJob = computed(() => workspace.value?.generationJobs.find((item) => item.id === renderJobId.value));
/** Final package actions require the current reviewed revision. */
const approved = computed(() => currentId.value && approvedRevision.value === revision.value && !dirty.value);
/** Matching fabric choices are explicit library items. */
const fabricAssets = computed(() => workspace.value?.assets.filter((item) => item.role === 'fabric') ?? []);
/** The available card output must belong to the unchanged requested composition. */
const canUseOutput = computed(
  () =>
    renderJob.value?.status === 'completed' &&
    renderedSignature.value === compositionSignature.value &&
    renderJob.value.outputAssetIds.length > 0,
);
/** Unsaved work participates in the SDK navigation blocker. */
const blocker = useNavigationBlocker(dirty);

/** Invalidates old artwork when a displayed card field changes. */
watch(
  compositionSignature,
  () => {
    if (!restoring) form.mediaIds = [];
  },
  { flush: 'sync' },
);

/** Selects a product and proposes its source title without claiming wording approval. */
function chooseProduct(id: string) {
  form.productId = id;
  form.title = workspace.value?.products.find((item) => item.id === id)?.name ?? '';
}

/** Captures the saved server revision while preserving editor-only composition controls. */
function applySaved(post: DraftModel, submittedSnapshot: string) {
  const changed = JSON.stringify({ form, composition: composition.value }) !== submittedSnapshot;
  currentId.value = post.id;
  revision.value = post.revision;
  approvedRevision.value = post.approvedRevision;
  if (changed) {
    saved.value = submittedSnapshot;
    return;
  }
  restoring = true;
  currentId.value = post.id;
  revision.value = post.revision;
  approvedRevision.value = post.approvedRevision;
  Object.assign(form, {
    title: post.title,
    productId: post.productId,
    presetId: post.presetId,
    locale: post.locale,
    caption: post.caption,
    channels: [...post.channels],
    mediaIds: [...post.mediaIds],
  });
  saved.value = JSON.stringify({ form, composition: composition.value });
  restoring = false;
}

/** Saves the current values with their explicit expected revision. */
async function save() {
  if (!form.title.trim() || !form.productId) {
    desk.error.value = 'Mahsulot va sarlavhani kiriting.';
    return null;
  }
  const submittedSnapshot = JSON.stringify({ form, composition: composition.value });
  const result = await desk.saveDraft(
    {
      ...form,
      composition: {
        title: form.title,
        subtitle: subtitle.value,
        fabricAssetId: fabricAssetId.value || null,
        scale: scale.value ?? 1,
        offsetX: offsetX.value ?? 0,
        offsetY: offsetY.value ?? 0,
      },
      channels: [...form.channels],
      mediaIds: [...form.mediaIds],
      expectedRevision: currentId.value ? revision.value : undefined,
    },
    currentId.value,
  );
  if (!result.ok) return null;
  applySaved(result.value, submittedSnapshot);
  desk.notice.value = 'Post saqlandi.';
  return result.value;
}

/** Generates the exact configured artwork as a recorded job. */
async function render() {
  const signature = compositionSignature.value;
  const result = await desk.renderCard({ ...composition.value });
  if (result.ok) {
    renderJobId.value = result.value.id;
    renderedSignature.value = signature;
    desk.notice.value = 'Tasvir tayyorlash navbatga qo‘shildi.';
  }
}

/** Uses a completed output only for its unchanged composition. */
function useOutput(job: GenerationJobModel | undefined) {
  if (!job || !canUseOutput.value) return;
  form.mediaIds = [...job.outputAssetIds];
  desk.notice.value = 'Tayyor tasvir postga qo‘shildi.';
}

/** Approves the saved copy and selected artwork revision. */
async function approve() {
  if (!form.mediaIds.length) {
    desk.error.value = 'Tasvirni tayyorlab, postga qo‘shing.';
    return;
  }
  if (dirty.value || !currentId.value) {
    if (!(await save())) return;
  }
  if (!currentId.value) return;
  if (dirty.value) {
    desk.error.value = 'Yangi o‘zgarishlar saqlanmagan. Avval ularni saqlang.';
    return;
  }
  const submittedSnapshot = JSON.stringify({ form, composition: composition.value });
  const result = await desk.approveDraft(currentId.value, revision.value);
  if (result.ok) {
    applySaved(result.value, submittedSnapshot);
    desk.notice.value = 'Shu post varianti tekshirildi.';
  }
}

/** Downloads the immutable package for the reviewed revision. */
async function exportPost() {
  if (!currentId.value || !approved.value) return;
  const result = await desk.exportDraft(currentId.value);
  if (result.ok) {
    downloadBlob(result.value, `ocharo-post-${currentId.value}.zip`);
    desk.notice.value = 'Post to‘plami yuklab olindi.';
  }
}

/** Plans the approved revision with an explicit Tashkent wall-clock time. */
async function schedule() {
  if (!currentId.value || !scheduleTime.value) {
    desk.error.value = 'Sana va vaqtni tanlang.';
    return;
  }
  const instant = MarketingExtensions.toTashkentInstant(scheduleTime.value.toString().slice(0, 16));
  if (!instant) {
    desk.error.value = 'Sana va vaqtni tekshiring.';
    return;
  }
  const result = await desk.scheduleDraft(currentId.value, revision.value, scheduleChannel.value, instant);
  if (result.ok) {
    scheduleOpen.value = false;
    desk.notice.value = result.value.message ?? 'Post rejaga qo‘shildi.';
  }
}

/**
 * Builds a candidate caption only from known source facts: the saved template for the post's language, filled from
 * the catalogue product (copy, sizes, availability, price); falls back to the local Uzbek facts when offline.
 */
async function useFacts() {
  if (!product.value) return;
  const result = await desk.renderCaption(product.value.id, form.locale);
  previousCaption.value = form.caption;
  form.caption = result.ok ? result.value.caption : MarketingExtensions.buildCaption(product.value);
}

/** Requests a provider-backed suggestion without overwriting the current caption. */
async function suggestText() {
  const source = product.value ? MarketingExtensions.buildCaption(product.value) : '';
  const result = await desk.assist(`${source}\n\n${form.caption}`, 'caption', form.locale);
  if (result.ok) suggestion.value = result.value.text;
}

/** Applies a reviewed suggestion and retains an undo value. */
function applySuggestion() {
  if (!suggestion.value) return;
  previousCaption.value = form.caption;
  form.caption = suggestion.value;
  suggestion.value = null;
}

/** Restores the caption preceding the last assisted replacement. */
function undoCaption() {
  if (previousCaption.value === null) return;
  form.caption = previousCaption.value;
  previousCaption.value = null;
}

/** Toggles an intended channel without implying an account connection. */
function toggleChannel(channel: string) {
  form.channels = form.channels.includes(channel)
    ? form.channels.filter((item) => item !== channel)
    : [...form.channels, channel];
}
</script>

<template>
  <div class="space-y-6">
    <div
      v-if="desk.reviewedCopy?.value"
      class="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-primary/20 bg-primary-soft p-4"
    >
      <div>
        <p class="text-sm font-semibold">Tekshirilgan tasvir tahlili matni</p>
        <p class="mt-2 whitespace-pre-wrap break-words text-xs leading-6">{{ desk.reviewedCopy.value.text }}</p>
        <p class="mt-2 text-[11px] text-muted-foreground">
          Manba tahlili · {{ desk.reviewedCopy.value.revision }}-talqin. Qo‘shilgach postni alohida saqlang va
          tekshiring.
        </p>
      </div>
      <div class="flex flex-wrap gap-2">
        <Button size="sm" @click="useReviewedCopy">Post matniga qo‘shish</Button
        ><Button size="sm" variant="ghost" @click="desk.reviewedCopy.value = null">Yopish</Button>
      </div>
    </div>

    <div class="flex flex-wrap items-center justify-between gap-4">
      <div>
        <button
          class="mb-3 inline-flex items-center gap-2 text-xs text-muted-foreground"
          @click="desk.navigate('/posts')"
        >
          <Icon :icon="ArrowLeft" :size="14" />Postlarga qaytish
        </button>
        <h1 class="font-editorial text-3xl">{{ currentId ? 'Postni tahrirlash' : 'Yangi post' }}</h1>
      </div>
      <div class="flex items-center gap-3">
        <StatusIndicator v-if="currentId" :status="approved ? 'approved' : 'draft'" /><span
          v-if="dirty"
          class="text-xs text-warning"
          >Saqlanmagan</span
        ><Button :is-loading="desk.busy.value === 'save'" :is-disabled="desk.busy.value !== null" @click="save"
          >Saqlash</Button
        >
      </div>
    </div>
    <div v-if="props.draftId && !original" class="rounded-xl border border-destructive bg-destructive-soft p-6">
      <h2 class="text-base font-semibold">Post topilmadi</h2>
      <p class="mt-2 text-sm">Postlar ro‘yxatidan mavjud variantni oching.</p>
    </div>
    <div v-else class="grid items-start gap-5 xl:grid-cols-[250px_minmax(280px,1fr)_330px]">
      <aside class="space-y-5 rounded-2xl border border-border bg-card p-5">
        <div>
          <h2 class="text-sm font-semibold">1. Mahsulot</h2>
          <p class="mt-2 text-xs leading-5 text-muted-foreground">Old ko‘rinishdan boshlang.</p>
        </div>
        <div class="space-y-2">
          <button
            v-for="item in workspace?.products"
            :key="item.id"
            :class="[
              'flex w-full items-center gap-3 rounded-xl border p-2 text-left',
              form.productId === item.id ? 'border-primary bg-primary-soft' : 'border-border hover:bg-muted',
            ]"
            @click="chooseProduct(item.id)"
          >
            <img
              :src="MarketingExtensions.productAsset(workspace, item) ?? ''"
              :alt="item.name"
              class="h-14 w-12 rounded-md bg-muted object-contain"
            />
            <div>
              <p class="text-xs font-semibold">{{ item.name }}</p>
              <p class="mt-1 text-xs text-muted-foreground">{{ item.category }}</p>
            </div>
          </button>
        </div>
        <div class="border-t border-border pt-5">
          <h2 class="mb-3 text-sm font-semibold">2. Shablon</h2>
          <SelectPicker
            v-model="form.presetId"
            :options="
              workspace?.presets.map((item) => ({
                itemKey: item.id,
                value: item.id,
                label: MarketingExtensions.presetName(item.id, item.name),
              }))
            "
            ><SelectPickerTrigger aria-label="Shablon tanlash"
              ><SelectPickerValue placeholder="Shablon tanlang" /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem
                v-for="item in workspace?.presets"
                :key="item.id"
                :item-key="item.id"
                :label="MarketingExtensions.presetName(item.id, item.name)" /></SelectPickerContent
          ></SelectPicker>
          <p class="mt-3 text-xs leading-5 text-muted-foreground">
            {{ preset ? MarketingExtensions.presetDescription(preset.id, preset.description) : '' }}
          </p>
          <button class="mt-3 text-xs font-semibold text-primary" @click="desk.navigate('/atlas')">
            Atlasda taqqoslash →
          </button>
        </div>
        <div v-if="preset?.requiresFabric" class="border-t border-border pt-4">
          <label class="mb-2 block text-xs font-semibold">Mato fotosi</label
          ><SelectPicker
            v-model="fabricAssetId"
            :options="fabricAssets.map((asset) => ({ itemKey: asset.id, value: asset.id, label: asset.fileName }))"
            ><SelectPickerTrigger aria-label="Mos mato fotosi"
              ><SelectPickerValue placeholder="Mato tanlang" /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem
                v-for="asset in fabricAssets"
                :key="asset.id"
                :item-key="asset.id"
                :label="asset.fileName" /></SelectPickerContent
          ></SelectPicker>
          <p v-if="!fabricAssets.length" class="mt-2 text-xs leading-5 text-warning">
            Mos mato fotosini Mahsulotlar bo‘limiga yuklang.
          </p>
        </div>
        <details class="border-t border-border pt-4">
          <summary class="cursor-pointer text-xs font-semibold">Tasvir joylashuvi</summary>
          <p class="mt-3 text-xs leading-5 text-muted-foreground">
            Masshtab: 75–105%. Siljish karta enining ±12% yoki balandligining ±6% chegarasida; 0.01 = 1%.
          </p>
          <div class="mt-4 space-y-3">
            <label class="block text-xs"
              >Masshtab<NumberInput
                v-model="scale"
                :min="0.75"
                :max="1.05"
                :step="0.01"
                aria-label="Masshtab"
                class="mt-2" /></label
            ><label class="block text-xs"
              >Gorizontal siljish<NumberInput
                v-model="offsetX"
                :min="-0.12"
                :max="0.12"
                :step="0.01"
                aria-label="Gorizontal siljish"
                class="mt-2" /></label
            ><label class="block text-xs"
              >Vertikal siljish<NumberInput
                v-model="offsetY"
                :min="-0.06"
                :max="0.06"
                :step="0.01"
                aria-label="Vertikal siljish"
                class="mt-2" /></label
            ><Button
              variant="ghost"
              size="sm"
              @click="
                scale = 1;
                offsetX = 0;
                offsetY = 0;
              "
              >Joylashuvni tiklash</Button
            >
          </div>
        </details>
      </aside>
      <section class="rounded-2xl border border-border bg-muted p-5">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-xs font-semibold">Post ko‘rinishi</h2>
          <span class="text-xs text-muted-foreground">1080 × 1350</span>
        </div>
        <div class="mx-auto max-w-90"><CardPreview :composition="composition" /></div>
        <div class="mt-5 flex flex-wrap justify-center gap-3">
          <Button
            :is-loading="desk.busy.value === 'render'"
            :is-disabled="desk.busy.value !== null || !form.title || (preset?.requiresFabric && !fabricAssetId)"
            @click="render"
            ><Icon :icon="Sparkles" :size="16" class="mr-2" />Tasvirni tayyorlash</Button
          >
        </div>
        <div v-if="renderJob" class="mt-4 rounded-xl bg-card p-4">
          <div class="flex items-center justify-between">
            <StatusIndicator :status="renderJob.status" /><Button
              v-if="canUseOutput"
              size="sm"
              variant="outline"
              @click="useOutput(renderJob)"
              >Postga qo‘shish</Button
            >
          </div>
          <p v-if="renderJob.message" class="mt-2 text-xs leading-5 text-muted-foreground">{{ renderJob.message }}</p>
          <p v-if="renderJob.status === 'completed' && !canUseOutput" class="mt-2 text-xs text-warning">
            Ko‘rinish o‘zgargan bo‘lsa, tasvirni qayta tayyorlang.
          </p>
        </div>
      </section>
      <aside class="space-y-5 rounded-2xl border border-border bg-card p-5">
        <h2 class="text-sm font-semibold">3. Matn va tekshirish</h2>
        <label class="block text-xs font-semibold"
          >Post tili
          <div class="mt-2 flex gap-2">
            <button
              v-for="locale in ['uz', 'ru', 'en']"
              :key="locale"
              :class="[
                'rounded-md border px-3 py-2 text-xs',
                form.locale === locale ? 'border-primary bg-primary-soft text-primary' : 'border-border',
              ]"
              @click="form.locale = locale"
            >
              {{ locale.toUpperCase() }}
            </button>
          </div></label
        ><label class="block text-xs font-semibold"
          >Sarlavha<TextInput v-model="form.title" class="mt-2" :maxlength="80" aria-label="Post sarlavhasi" /></label
        ><label class="block text-xs font-semibold"
          >Qo‘shimcha satr<TextInput
            v-model="subtitle"
            class="mt-2"
            :maxlength="90"
            placeholder="Ixtiyoriy"
            aria-label="Qo‘shimcha satr" /></label
        ><label class="block text-xs font-semibold"
          >Post matni<TextAreaInput
            v-model="form.caption"
            class="mt-2"
            :rows="7"
            aria-label="Post matni"
            placeholder="Mahsulot haqida qisqa va aniq yozing…"
        /></label>
        <div class="flex flex-wrap gap-2">
          <Button variant="outline" size="sm" @click="useFacts">Faktlardan matn</Button
          ><Button
            variant="ghost"
            size="sm"
            :is-loading="desk.busy.value === 'assist'"
            :is-disabled="desk.busy.value !== null"
            @click="suggestText"
            >AI taklifi</Button
          ><Button
            v-if="previousCaption !== null"
            variant="ghost"
            size="sm"
            aria-label="Oldingi matnni tiklash"
            @click="undoCaption"
            ><Icon :icon="RotateCcw" :size="15"
          /></Button>
        </div>
        <div v-if="suggestion" class="rounded-xl border border-primary bg-primary-soft p-4">
          <p class="mb-2 text-xs font-semibold">Matn taklifi</p>
          <p class="whitespace-pre-wrap text-xs leading-6">{{ suggestion }}</p>
          <div class="mt-3 flex gap-2">
            <Button size="sm" @click="applySuggestion">Qo‘llash</Button
            ><Button size="sm" variant="ghost" @click="suggestion = null">Bekor qilish</Button>
          </div>
        </div>
        <div>
          <p class="mb-2 text-xs font-semibold">Kanallar</p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="channel in ['instagram', 'telegram', 'whatsapp', 'x']"
              :key="channel"
              :class="[
                'rounded-full border px-3 py-1.5 text-xs capitalize',
                form.channels.includes(channel)
                  ? 'border-primary bg-primary-soft text-primary'
                  : 'border-border text-muted-foreground',
              ]"
              :aria-pressed="form.channels.includes(channel)"
              @click="toggleChannel(channel)"
            >
              {{ channel === 'x' ? 'X' : channel }}
            </button>
          </div>
          <p class="mt-2 text-xs leading-5 text-muted-foreground">Tanlash kanalga ulanganlikni anglatmaydi.</p>
        </div>
        <div class="border-t border-border pt-4">
          <p class="text-xs font-semibold">Mahsulot ma’lumotlari</p>
          <p class="mt-2 text-xs leading-5 text-muted-foreground">
            {{ product?.material }} · {{ product?.sizes.join(', ') }}
          </p>
          <StatusIndicator class="mt-2" :status="product?.reviewStatus ?? 'pending'" />
          <ul v-if="product?.issues.length" class="mt-3 space-y-1 text-xs leading-5 text-warning">
            <li v-for="issue in product.issues.slice(0, 3)" :key="issue">{{ issue }}</li>
          </ul>
        </div>
        <div v-if="selectedOutput" class="rounded-xl border border-border p-3">
          <p class="mb-2 text-xs font-semibold">Postga qo‘shilgan tasvir</p>
          <img
            :src="selectedOutput.url"
            alt="Postga qo‘shilgan tayyor tasvir"
            class="max-h-56 w-full rounded-lg object-contain"
          />
          <p class="mt-2 text-xs text-muted-foreground">Tekshirish va eksport aynan shu tasvirni ishlatadi.</p>
        </div>
        <p v-else class="rounded-lg bg-warning-soft p-3 text-xs leading-5 text-warning">
          Tasvirni tayyorlang va natijani postga qo‘shing.
        </p>
        <div class="space-y-2 border-t border-border pt-4">
          <Button
            :is-full-width="true"
            :is-disabled="!form.mediaIds.length || desk.busy.value !== null || !!approved"
            :is-loading="desk.busy.value === 'approve'"
            @click="approve"
            ><Icon :icon="Check" :size="16" class="mr-2" />{{
              approved ? 'Shu variant tekshirildi' : 'Tekshirildi deb belgilash'
            }}</Button
          >
          <div class="grid grid-cols-2 gap-2">
            <Button variant="outline" :is-disabled="!approved || desk.busy.value !== null" @click="exportPost"
              ><Icon :icon="Download" :size="15" class="mr-2" />Eksport</Button
            ><Button variant="outline" :is-disabled="!approved || desk.busy.value !== null" @click="scheduleOpen = true"
              ><Icon :icon="CalendarPlus" :size="15" class="mr-2" />Rejaga</Button
            >
          </div>
        </div>
      </aside>
    </div>
    <Modal
      :open="blocker.state === 'blocked'"
      @update:open="
        (open) => {
          if (!open) blocker.reset();
        }
      "
      ><ModalContent class="max-w-md"
        ><ModalHeader
          ><ModalTitle>Saqlanmagan o‘zgarishlar</ModalTitle
          ><ModalDescription
            >Matn va tanlangan variantni saqlab qoling yoki sahifadan chiqing.</ModalDescription
          ></ModalHeader
        ><ModalFooter
          ><Button variant="outline" @click="blocker.reset">Tahrirlashni davom ettirish</Button
          ><Button @click="blocker.proceed">Saqlamasdan chiqish</Button></ModalFooter
        ></ModalContent
      ></Modal
    >
    <Modal v-model:open="scheduleOpen"
      ><ModalContent class="max-w-md"
        ><ModalHeader
          ><ModalTitle>Postni rejalashtirish</ModalTitle
          ><ModalDescription
            >Vaqt Toshkent bo‘yicha. Ulanmagan kanalda post reja sifatida saqlanadi.</ModalDescription
          ></ModalHeader
        >
        <div class="my-5 space-y-4">
          <DateTimeInput
            v-model="scheduleTime"
            placeholder="Sana va vaqtni tanlang"
            aria-label="Sana va vaqt"
          /><SelectPicker
            v-model="scheduleChannel"
            :options="['instagram', 'telegram', 'whatsapp', 'x'].map((id) => ({ itemKey: id, value: id, label: id }))"
            ><SelectPickerTrigger aria-label="Rejalashtirish kanali"><SelectPickerValue /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem
                v-for="channel in ['instagram', 'telegram', 'whatsapp', 'x']"
                :key="channel"
                :item-key="channel"
                :label="channel" /></SelectPickerContent
          ></SelectPicker>
        </div>
        <ModalFooter
          ><Button variant="outline" @click="scheduleOpen = false">Bekor qilish</Button
          ><Button
            :is-loading="desk.busy.value === 'schedule'"
            :is-disabled="!scheduleTime || desk.busy.value !== null"
            @click="schedule"
            >Rejaga qo‘shish</Button
          ></ModalFooter
        ></ModalContent
      ></Modal
    >
  </div>
</template>
