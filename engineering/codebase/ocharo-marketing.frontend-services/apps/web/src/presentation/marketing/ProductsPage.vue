<script setup lang="ts">
import FilePicker from './compatibility/MediaPicker.vue';
import SelectPickerTrigger from './compatibility/PickerTrigger.vue';
import { computed, ref } from 'vue';
import { ArrowUpRight, FileSearch, ImagePlus, Search, Upload } from 'lucide-vue-next';

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
import { MarketingExtensions, type ScanModel } from '../../domain/marketing';
import GenerationPanel from './GenerationPanel.vue';
import SourceImageAnalysisPanel from './SourceImageAnalysisPanel.vue';
import StatusIndicator from './StatusIndicator.vue';

/** Reuses actual product records and media with explicit scan and upload actions. */
defineOptions({ name: 'ProductsPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const tab = ref('products');
const search = ref('');
const scanText = ref('');
const scanResult = ref<ScanModel | null>(null);
const file = ref<File | null>(null);
const provenance = ref('');
const role = ref('reference');
/** The imported product list respects the visible search. */
const products = computed(
  () =>
    workspace.value?.products.filter((item) =>
      `${item.name} ${item.category}`.toLocaleLowerCase().includes(search.value.toLocaleLowerCase()),
    ) ?? [],
);
/** The media library contains persisted originals and generated outputs. */
const assets = computed(() => workspace.value?.assets ?? []);

/** Retains the selected file until a successful upload. */
function chooseFile(files: FileList | null) {
  file.value = files?.item(0) ?? null;
}

/** Uploads an original with its explicit role and source note. */
async function upload() {
  if (!file.value || !provenance.value.trim()) {
    desk.error.value = 'Fayl va manba izohini kiriting.';
    return;
  }
  const result = await desk.upload(file.value, provenance.value, role.value);
  if (result.ok) {
    file.value = null;
    provenance.value = '';
    desk.notice.value = 'Tasvir kutubxonaga qo‘shildi.';
  }
}

/** Scans pasted source wording without silently accepting suggested facts. */
async function scan() {
  const result = await desk.scan(scanText.value);
  if (result.ok) scanResult.value = result.value;
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <p class="text-xs uppercase tracking-widest text-primary">Manbalar</p>
      <h1 class="mt-2 font-editorial text-3xl">Mahsulotlar va tasvirlar</h1>
      <p class="mt-3 text-sm text-muted-foreground">
        Mavjud mahsulotdan boshlang. Rasmlar va manba matnini shu yerda tayyorlang.
      </p>
      <Button class="mt-4" size="sm" :is-disabled="desk.busy.value !== null" @click="desk.syncCatalogue()"
        >Ocharo Katalogdan yangilash</Button
      >
    </div>
    <div class="flex flex-wrap gap-1 rounded-xl bg-muted p-1">
      <button
        v-for="item in [
          { id: 'products', label: 'Mahsulotlar' },
          { id: 'media', label: 'Tasvirlar' },
          { id: 'scan', label: 'Matnni tekshirish' },
          { id: 'analysis', label: 'Tasvirni tahlil qilish' },
          { id: 'generate', label: 'AI tasvir' },
        ]"
        :key="item.id"
        :class="[
          'rounded-lg px-4 py-2.5 text-xs font-semibold',
          tab === item.id ? 'bg-card shadow-sm' : 'text-muted-foreground',
        ]"
        @click="tab = item.id"
      >
        {{ item.label }}
      </button>
    </div>
    <section v-if="tab === 'products'" class="space-y-5">
      <div class="flex max-w-sm items-center gap-2">
        <Icon :icon="Search" :size="17" class="text-muted-foreground" /><TextInput
          v-model="search"
          aria-label="Mahsulotni topish"
          placeholder="Mahsulotni topish…"
        />
      </div>
      <div class="grid gap-5 sm:grid-cols-2 xl:grid-cols-3">
        <article
          v-for="item in products"
          :key="item.id"
          class="overflow-hidden rounded-2xl border border-border bg-card"
        >
          <div class="flex aspect-[4/3] items-center justify-center bg-muted p-6">
            <img
              :src="MarketingExtensions.productAsset(workspace, item) ?? ''"
              :alt="item.name"
              class="h-full w-full object-contain"
            />
          </div>
          <div class="p-5">
            <div class="flex items-center justify-between gap-3">
              <h2 class="font-editorial text-xl">{{ item.name }}</h2>
              <StatusIndicator :status="item.reviewStatus" />
            </div>
            <p class="mt-3 text-sm text-muted-foreground">{{ item.material }}</p>
            <p class="mt-2 text-xs text-muted-foreground">{{ item.sizes.join(' · ') }}</p>
            <p class="mt-4 text-sm font-semibold">
              {{ MarketingExtensions.formatPrice(item.price, item.currency)
              }}<span v-if="item.price !== null" class="ml-2 text-xs font-normal text-muted-foreground">1 dona</span>
            </p>
            <Button
              variant="outline"
              class="mt-5"
              :is-full-width="true"
              @click="desk.navigate(`/posts/new?product=${item.id}`)"
              >Post yaratish <Icon :icon="ArrowUpRight" :size="15" class="ml-2"
            /></Button>
            <details v-if="item.issues.length" class="mt-4 text-xs">
              <summary class="cursor-pointer text-warning">Tekshirish kerak · {{ item.issues.length }}</summary>
              <ul class="mt-2 space-y-2 leading-5 text-muted-foreground">
                <li v-for="issue in item.issues" :key="issue">{{ issue }}</li>
              </ul>
            </details>
          </div>
        </article>
      </div>
      <p v-if="!products.length" class="rounded-xl bg-card p-6 text-sm text-muted-foreground">
        Mos mahsulot topilmadi.
      </p>
    </section>
    <section v-if="tab === 'media'" class="grid items-start gap-6 lg:grid-cols-[1fr_320px]">
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <article
          v-for="asset in assets"
          :key="asset.id"
          class="overflow-hidden rounded-xl border border-border bg-card"
        >
          <a
            :href="asset.url"
            target="_blank"
            rel="noopener noreferrer"
            class="flex aspect-square items-center justify-center bg-muted p-4"
            ><img :src="asset.url" :alt="asset.fileName" class="h-full w-full object-contain"
          /></a>
          <div class="p-4">
            <p class="truncate text-xs font-semibold" :title="asset.fileName">{{ asset.fileName }}</p>
            <p class="mt-2 text-xs text-muted-foreground">
              {{ asset.role }} · {{ Math.round(asset.byteLength / 1024) }} KB
            </p>
            <details class="mt-2 text-xs text-muted-foreground">
              <summary class="cursor-pointer">Manba</summary>
              <p class="mt-2 leading-5">{{ asset.provenance }}</p>
            </details>
          </div>
        </article>
      </div>
      <aside class="space-y-4 rounded-2xl border border-border bg-card p-5">
        <Icon :icon="ImagePlus" :size="24" class="text-primary" />
        <h2 class="font-editorial text-xl">Tasvir qo‘shish</h2>
        <p class="text-xs leading-6 text-muted-foreground">Asl foto, mos mato yoki ish detallari uchun.</p>
        <FilePicker
          label="Fayl tanlash"
          :preview="file?.name ?? ''"
          accept="image/png,image/jpeg,image/webp"
          @files-change="chooseFile"
        /><label class="block text-xs font-semibold"
          >Manba izohi<TextInput
            v-model="provenance"
            class="mt-2"
            aria-label="Tasvir manbasi"
            placeholder="Rasm qayerdan olingan?" /></label
        ><SelectPicker
          v-model="role"
          :options="[
            { itemKey: 'reference', value: 'reference', label: 'Asl foto' },
            { itemKey: 'fabric', value: 'fabric', label: 'Mos mato' },
            { itemKey: 'detail', value: 'detail', label: 'Mahsulot detali' },
            { itemKey: 'front', value: 'front', label: 'Mahsulot old ko‘rinishi' },
          ]"
          ><SelectPickerTrigger aria-label="Tasvir turi"><SelectPickerValue /></SelectPickerTrigger
          ><SelectPickerContent
            ><SelectPickerItem item-key="reference" label="Asl foto" /><SelectPickerItem
              item-key="fabric"
              label="Mos mato" /><SelectPickerItem item-key="detail" label="Mahsulot detali" /><SelectPickerItem
              item-key="front"
              label="Mahsulot old ko‘rinishi" /></SelectPickerContent></SelectPicker
        ><Button
          :is-disabled="!file || !provenance.trim() || desk.busy.value !== null"
          :is-loading="desk.busy.value === 'upload'"
          @click="upload"
          ><Icon :icon="Upload" :size="16" class="mr-2" />Kutubxonaga qo‘shish</Button
        >
      </aside>
    </section>
    <section v-if="tab === 'scan'" class="grid gap-6 lg:grid-cols-2">
      <div class="space-y-5 rounded-2xl border border-border bg-card p-6">
        <Icon :icon="FileSearch" :size="26" class="text-primary" />
        <h2 class="font-editorial text-2xl">Manba matnini tekshiring</h2>
        <p class="text-sm leading-7 text-muted-foreground">
          Mahsulot haqidagi matnni joylang. Topilgan ma’lumot va yetishmayotgan qismlarni ko‘ring.
        </p>
        <TextAreaInput
          v-model="scanText"
          :rows="9"
          aria-label="Tekshirish uchun manba matni"
          placeholder="Material: …
O‘lcham: …
Narx: …"
        /><Button
          :is-disabled="!scanText.trim() || desk.busy.value !== null"
          :is-loading="desk.busy.value === 'scan'"
          @click="scan"
          >Matnni tekshirish</Button
        >
      </div>
      <div v-if="scanResult" class="space-y-4 rounded-2xl border border-border bg-card p-6">
        <h2 class="font-editorial text-2xl">Tekshirish natijasi</h2>
        <p class="text-sm leading-7">{{ scanResult.summary }}</p>
        <div v-if="scanResult.suggestions.length">
          <h3 class="mb-2 text-xs font-semibold text-primary">Takliflar</h3>
          <ul class="space-y-2 text-sm leading-6">
            <li v-for="item in scanResult.suggestions" :key="item">{{ item }}</li>
          </ul>
        </div>
        <div v-if="scanResult.issues.length" class="rounded-xl bg-warning-soft p-4">
          <h3 class="mb-2 text-xs font-semibold text-warning">Tekshiring</h3>
          <ul class="space-y-2 text-xs leading-6">
            <li v-for="item in scanResult.issues" :key="item">{{ item }}</li>
          </ul>
        </div>
        <p class="text-xs leading-6 text-muted-foreground">
          Takliflar mahsulot ma’lumotlarini o‘z-o‘zidan o‘zgartirmaydi.
        </p>
      </div>
      <div
        v-else
        class="flex min-h-64 items-center justify-center rounded-2xl border border-dashed border-border p-8 text-center text-sm text-muted-foreground"
      >
        Natija shu yerda ko‘rinadi.
      </div>
    </section>
    <SourceImageAnalysisPanel v-if="tab === 'analysis'" />
    <GenerationPanel v-if="tab === 'generate'" />
  </div>
</template>
