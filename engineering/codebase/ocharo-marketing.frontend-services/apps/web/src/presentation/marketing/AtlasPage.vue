<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import SelectPickerTrigger from './compatibility/PickerTrigger.vue';
import { computed, ref } from 'vue';
import { ArrowRight, BookOpen, Download } from 'lucide-vue-next';

import { downloadBlob } from '@wow-two-beta/fe/foundation/files';
import { Icon } from '@wow-two-beta/fe/foundation/icons';
import { Button } from '@wow-two-beta/fe/presentation/actions';
import {
  SelectPicker,
  SelectPickerContent,
  SelectPickerItem,
  SelectPickerValue,
} from '@wow-two-beta/fe/presentation/forms';

import { useMarketingWorkspace } from '../../application/marketing';
import { MarketingExtensions, type AssetModel } from '../../domain/marketing';
import CardPreview from './CardPreview.vue';
const { t: ui } = useUiLocale();

/** Compares the exact card presets and exports selected completed artwork as a journal. */
defineOptions({ name: 'AtlasPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const productId = ref(workspace.value?.products[0]?.id ?? '');
const selected = ref<string[]>([]);
/** The selected product holds garment and title constant during comparisons. */
const product = computed(() => workspace.value?.products.find((item) => item.id === productId.value));
/** Completed card outputs are the only candidates for the exact-image PDF. */
const outputs = computed(() => {
  const ids = new Set(
    workspace.value?.generationJobs
      .filter((job) => job.kind === 'card' && job.status === 'completed')
      .flatMap((job) => job.outputAssetIds) ?? [],
  );
  return workspace.value?.assets.filter((asset) => ids.has(asset.id)) ?? [];
});

/** Names finished artwork without exposing generated job identifiers. */
function outputLabel(asset: AssetModel) {
  const job = workspace.value?.generationJobs.find((item) => item.outputAssetIds.includes(asset.id));
  const product = workspace.value?.products.find((item) => item.id === job?.productId)?.name ?? 'Tayyor karta';
  const preset = workspace.value?.presets.find((item) => asset.fileName.startsWith(item.id));
  return `${product}${preset ? ' · ' + MarketingExtensions.presetName(preset.id, preset.name) : ''}`;
}
/** Toggles a completed card in the comparison PDF selection. */
function toggle(id: string) {
  selected.value = selected.value.includes(id) ? selected.value.filter((item) => item !== id) : [...selected.value, id];
}

/** Downloads a journal composed from exact selected output bytes. */
async function exportAtlas() {
  const result = await desk.exportAtlas(selected.value);
  if (result.ok) {
    downloadBlob(result.value, 'ocharo-card-atlas.pdf');
    desk.notice.value = 'Atlas yuklab olindi.';
  }
}
</script>

<template>
  <div class="space-y-7">
    <div class="flex flex-wrap items-start justify-between gap-5">
      <div>
        <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Visual atlas') }}</p>
        <h1 class="mt-2 font-editorial text-3xl">{{ ui('One product, different expressions.') }}</h1>
        <p class="mt-3 max-w-xl text-sm leading-7 text-muted-foreground">
          {{ ui('Compare templates here. Apply the chosen variant to an everyday post.') }}
        </p>
      </div>
      <div class="w-full max-w-56">
        <SelectPicker
          v-model="productId"
          :options="workspace?.products.map((item) => ({ itemKey: item.id, value: item.id, label: item.name }))"
          ><SelectPickerTrigger :aria-label="ui('Atlas product')"><SelectPickerValue /></SelectPickerTrigger
          ><SelectPickerContent
            ><SelectPickerItem
              v-for="item in workspace?.products"
              :key="item.id"
              :item-key="item.id"
              :label="item.name" /></SelectPickerContent
        ></SelectPicker>
      </div>
    </div>
    <div class="grid items-start gap-5 md:grid-cols-3">
      <article
        v-for="preset in workspace?.presets"
        :key="preset.id"
        class="rounded-2xl border border-border bg-card p-5"
      >
        <div class="mb-4 flex items-center justify-between">
          <span class="text-xs font-semibold tracking-wider text-primary">{{ preset.family }}</span
          ><span class="rounded-full bg-muted px-2 py-1 text-xs text-muted-foreground">{{
            preset.requiresFabric ? ui('Fabric photo required') : ui('Front view')
          }}</span>
        </div>
        <CardPreview
          v-if="!preset.requiresFabric"
          :composition="{ productId, presetId: preset.id, title: product?.name ?? '', locale: 'uz' }"
          :show-issues="false"
        />
        <div v-else class="flex aspect-[4/5] flex-col items-center justify-center rounded-xl bg-muted p-6 text-center">
          <Icon :icon="BookOpen" :size="30" class="mb-5 text-primary" />
          <p class="font-editorial text-2xl">{{ ui('Half fabric + product') }}</p>
          <p class="mt-4 text-xs leading-6 text-muted-foreground">
            {{ ui('Upload a matching fabric photo and place the product over it.') }}
          </p>
          <div class="mt-6 h-1 w-28 rounded-full bg-primary-soft"><div class="h-1 w-14 rounded-full bg-primary" /></div>
        </div>
        <h2 class="mt-5 font-editorial text-xl">{{ ui(MarketingExtensions.presetName(preset.id, preset.name)) }}</h2>
        <p class="mt-2 min-h-12 text-xs leading-6 text-muted-foreground">
          {{ ui(MarketingExtensions.presetDescription(preset.id, preset.description)) }}
        </p>
        <Button
          variant="outline"
          :is-full-width="true"
          class="mt-4"
          @click="desk.navigate(`/posts/new?product=${productId}&preset=${preset.id}`)"
          >{{ ui('Apply to post') }} <Icon :icon="ArrowRight" :size="15" class="ml-2"
        /></Button>
      </article>
    </div>
    <section class="rounded-2xl border border-border bg-card p-6">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 class="font-editorial text-2xl">{{ ui('Journal from prepared cards') }}</h2>
          <p class="mt-2 text-xs leading-6 text-muted-foreground">
            {{ ui('The PDF uses the exact selected PNG images. Captions stay outside the cards.') }}
          </p>
        </div>
        <Button
          :is-disabled="!selected.length || desk.busy.value !== null"
          :is-loading="desk.busy.value === 'atlas'"
          @click="exportAtlas"
          ><Icon :icon="Download" :size="16" class="mr-2" />{{ ui('Atlas PDF') }}</Button
        >
      </div>
      <div v-if="outputs.length" class="mt-6 grid grid-cols-2 gap-4 md:grid-cols-4">
        <button
          v-for="asset in outputs"
          :key="asset.id"
          :class="[
            'overflow-hidden rounded-xl border p-2 text-left',
            selected.includes(asset.id) ? 'border-primary bg-primary-soft ring-1 ring-primary' : 'border-border',
          ]"
          :aria-pressed="selected.includes(asset.id)"
          @click="toggle(asset.id)"
        >
          <img :src="asset.url" :alt="outputLabel(asset)" class="aspect-[4/5] w-full rounded-lg object-contain" />
          <p class="mt-2 truncate text-xs">{{ ui(outputLabel(asset)) }}</p>
        </button>
      </div>
      <div v-else class="mt-6 rounded-xl border border-dashed border-border p-8 text-center">
        <p class="text-sm font-semibold">{{ ui('Prepare the card image first') }}</p>
        <p class="mt-2 text-xs text-muted-foreground">
          {{ ui('Cards prepared in the post composer will appear here.') }}
        </p>
        <Button class="mt-4" variant="ghost" size="sm" @click="desk.navigate('/posts/new')">{{
          ui('Prepare card')
        }}</Button>
      </div>
    </section>
    <section class="grid gap-4 md:grid-cols-4">
      <div
        v-for="family in [
          { id: 'T03', name: 'Detallar', note: 'Haqiqiy mato va ish yaqin fotosi.' },
          { id: 'T04', name: 'Kiyilgan ko‘rinish', note: 'To‘liq kiyilgan foto va qisqa sarlavha.' },
          { id: 'T05', name: 'A yoki B', note: 'Ikki haqiqiy rang yoki dizayn varianti.' },
          { id: 'T06', name: 'Mahsulot faktlari', note: 'O‘lcham, mato va xarid ma’lumotlari.' },
        ]"
        :key="family.id"
        class="rounded-xl border border-dashed border-border p-5"
      >
        <span class="text-xs font-semibold text-muted-foreground">{{ family.id }} {{ ui('· Research') }}</span>
        <h3 class="mt-3 text-sm font-semibold">{{ ui(family.name) }}</h3>
        <p class="mt-2 text-xs leading-6 text-muted-foreground">{{ ui(family.note) }}</p>
      </div>
    </section>
  </div>
</template>
