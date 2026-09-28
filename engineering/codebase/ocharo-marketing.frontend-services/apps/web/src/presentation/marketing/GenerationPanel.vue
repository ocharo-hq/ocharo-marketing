<script setup lang="ts">
import SelectPickerTrigger from './compatibility/PickerTrigger.vue';
import { computed, ref } from 'vue';
import { Sparkles } from 'lucide-vue-next';

import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import Button from './compatibility/ActionButton.vue';
import {
  SelectPicker,
  SelectPickerContent,
  SelectPickerItem,
  SelectPickerValue,
  TextAreaInput,
  TextInput,
} from '@wow-two-beta/ui-vue/presentation/forms';

import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions } from '../../domain/marketing';
import StatusIndicator from './StatusIndicator.vue';

/** Requests actual provider image jobs with explicitly selected product references. */
defineOptions({ name: 'GenerationPanel' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const provider = ref('openai');
const productId = ref(workspace.value?.products[0]?.id ?? '');
const prompt = ref('');
const model = ref('');
const referenceAssetIds = ref<string[]>([]);
/** The selected source product determines available reference imagery. */
const product = computed(() => workspace.value?.products.find((item) => item.id === productId.value));
/** References are existing assets, never inferred external photographs. */
const references = computed(
  () => workspace.value?.assets.filter((item) => product.value?.assetIds.includes(item.id)) ?? [],
);
/** The connection describes configuration without claiming provider verification. */
const connection = computed(() => workspace.value?.connections.find((item) => item.id === provider.value));
/** The history contains only persisted non-card generation jobs. */
const history = computed(() =>
  [...(workspace.value?.generationJobs ?? [])]
    .filter((item) => item.kind !== 'card')
    .sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
);

/** Toggles an explicit existing reference for the provider request. */
function toggleReference(id: string) {
  referenceAssetIds.value = referenceAssetIds.value.includes(id)
    ? referenceAssetIds.value.filter((item) => item !== id)
    : [...referenceAssetIds.value, id];
}

/** Selects the recorded front asset as the generation reference. */
function useFront() {
  referenceAssetIds.value = product.value ? [product.value.heroAssetId] : [];
}

/** Submits a configured provider request without fabricating unavailable results. */
async function generate() {
  if (!prompt.value.trim()) {
    desk.error.value = 'Tasvir uchun topshiriq yozing.';
    return;
  }
  const validReferences = referenceAssetIds.value.filter((id) => references.value.some((asset) => asset.id === id));
  const result = await desk.generate({
    provider: provider.value,
    prompt: prompt.value,
    productId: productId.value || undefined,
    referenceAssetIds: validReferences,
    model: model.value.trim() || undefined,
  });
  if (result.ok) desk.notice.value = 'Tasvir so‘rovi navbatga qo‘shildi.';
}
</script>

<template>
  <section class="grid items-start gap-6 lg:grid-cols-[1.1fr_1fr]">
    <div class="space-y-5 rounded-2xl border border-border bg-card p-6">
      <div class="flex items-center gap-3">
        <span class="rounded-xl bg-primary-soft p-3 text-primary"><Icon :icon="Sparkles" :size="21" /></span>
        <div>
          <h2 class="font-editorial text-2xl">Yangi tasvir</h2>
          <p class="mt-1 text-xs text-muted-foreground">Mavjud mahsulotdan yangi ko‘rinish yoki fon taklifi.</p>
        </div>
      </div>
      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="mb-2 block text-xs font-semibold">Xizmat</label
          ><SelectPicker
            v-model="provider"
            :options="[
              { itemKey: 'openai', value: 'openai', label: 'OpenAI' },
              { itemKey: 'gemini', value: 'gemini', label: 'Gemini' },
            ]"
            ><SelectPickerTrigger aria-label="Tasvir yaratish xizmati"><SelectPickerValue /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem item-key="openai" label="OpenAI" /><SelectPickerItem
                item-key="gemini"
                label="Gemini" /></SelectPickerContent
          ></SelectPicker>
        </div>
        <div>
          <label class="mb-2 block text-xs font-semibold">Mahsulot</label
          ><SelectPicker
            v-model="productId"
            :options="workspace?.products.map((item) => ({ itemKey: item.id, value: item.id, label: item.name }))"
            ><SelectPickerTrigger aria-label="Tasvir yaratish mahsuloti"><SelectPickerValue /></SelectPickerTrigger
            ><SelectPickerContent
              ><SelectPickerItem
                v-for="item in workspace?.products"
                :key="item.id"
                :item-key="item.id"
                :label="item.name" /></SelectPickerContent
          ></SelectPicker>
        </div>
      </div>
      <div v-if="connection" class="rounded-xl bg-muted p-4">
        <div class="flex items-center gap-2">
          <StatusIndicator :status="connection.status" /><span class="text-xs font-semibold">{{
            connection.name
          }}</span>
        </div>
        <p class="mt-2 text-xs leading-6 text-muted-foreground">{{ connection.setupHint }}</p>
      </div>
      <div>
        <div class="mb-3 flex items-center justify-between">
          <label class="text-xs font-semibold">Manba tasviri</label
          ><button class="text-xs font-semibold text-primary" @click="useFront">Old ko‘rinishni tanlash</button>
        </div>
        <div class="grid grid-cols-3 gap-3">
          <button
            v-for="asset in references"
            :key="asset.id"
            :class="[
              'relative flex aspect-square items-center justify-center rounded-xl border bg-muted p-3',
              referenceAssetIds.includes(asset.id) ? 'border-primary ring-1 ring-primary' : 'border-border',
            ]"
            :aria-pressed="referenceAssetIds.includes(asset.id)"
            @click="toggleReference(asset.id)"
          >
            <img :src="asset.url" :alt="asset.fileName" class="h-full w-full object-contain" /><span
              v-if="referenceAssetIds.includes(asset.id)"
              class="absolute right-2 top-2 size-3 rounded-full bg-primary"
            />
          </button>
        </div>
        <p class="mt-2 text-xs text-muted-foreground">Tanlangan tasvirlar so‘rov bilan xizmatga yuboriladi.</p>
      </div>
      <label class="block text-xs font-semibold"
        >Topshiriq<TextAreaInput
          v-model="prompt"
          class="mt-2"
          :rows="5"
          aria-label="Tasvir uchun topshiriq"
          placeholder="Masalan: mahsulotni o‘zgartirmasdan, sokin studiya fonida ko‘rsating…"
      /></label>
      <details>
        <summary class="cursor-pointer text-xs text-muted-foreground">Model sozlamasi</summary>
        <TextInput
          v-model="model"
          class="mt-3"
          aria-label="Tasvir yaratish modeli"
          placeholder="Xizmatdagi model nomi · ixtiyoriy"
        />
      </details>
      <Button
        :is-loading="desk.busy.value === 'generate'"
        :is-disabled="!prompt.trim() || desk.busy.value !== null"
        @click="generate"
        ><Icon :icon="Sparkles" :size="16" class="mr-2" />Tasvir yaratish</Button
      >
      <p class="text-xs leading-6 text-muted-foreground">
        Yangi tasvir mahsulot shakli va matosiga mosligi bo‘yicha tekshiriladi. Tayyor post matni alohida qoladi.
      </p>
    </div>
    <div class="space-y-4">
      <h2 class="font-editorial text-xl">Natijalar tarixi</h2>
      <div
        v-if="!history.length"
        class="flex min-h-64 flex-col items-center justify-center rounded-2xl border border-dashed border-border bg-card p-6 text-center"
      >
        <Icon :icon="Sparkles" :size="28" class="mb-4 text-muted-foreground" />
        <h3 class="text-sm font-semibold">Yaratilgan tasvirlar hali yo‘q</h3>
        <p class="mt-2 max-w-xs text-xs leading-6 text-muted-foreground">
          Xizmatni sozlang va aniq manba bilan topshiriq yuboring.
        </p>
      </div>
      <article v-for="job in history" :key="job.id" class="rounded-2xl border border-border bg-card p-5">
        <div class="flex items-center justify-between">
          <span class="text-xs font-semibold">{{ job.provider }}</span
          ><StatusIndicator :status="job.status" />
        </div>
        <p class="mt-3 line-clamp-3 text-xs leading-6 text-muted-foreground">{{ job.prompt }}</p>
        <p v-if="job.message" class="mt-2 text-xs text-warning">{{ job.message }}</p>
        <div v-if="job.outputAssetIds.length" class="mt-4 grid grid-cols-2 gap-2">
          <a
            v-for="id in job.outputAssetIds"
            :key="id"
            :href="workspace?.assets.find((asset) => asset.id === id)?.url"
            target="_blank"
            rel="noopener noreferrer"
            class="rounded-xl bg-muted p-2"
            ><img
              :src="workspace?.assets.find((asset) => asset.id === id)?.url"
              alt="Yaratilgan tasvir"
              class="aspect-square w-full object-contain"
          /></a>
        </div>
        <p class="mt-3 text-xs text-muted-foreground">{{ MarketingExtensions.formatDate(job.createdAt) }}</p>
      </article>
    </div>
  </section>
</template>
