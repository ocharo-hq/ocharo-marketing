<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { BookOpen, CalendarClock, Plus, RefreshCw, Sparkles, Trash2, Zap } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import Button from './compatibility/ActionButton.vue';
import { useMarketingWorkspace } from '../../application/marketing';
import type {
  AutomationRunModel,
  AutomationSettingsModel,
  BlogStoryModel,
  CatalogueSyncReportModel,
  SlotFillReportModel,
} from '../../domain/marketing';

/** Catalogue sync, bulk post creation, posting rhythm and blog-story posts in one place. */
defineOptions({ name: 'AutomationPage' });
const desk = useMarketingWorkspace();
const workspace = desk.workspace;
const settings = ref<AutomationSettingsModel | null>(null);
const run = ref<AutomationRunModel | null>(null);
const syncReport = ref<CatalogueSyncReportModel | null>(null);
const fillReport = ref<SlotFillReportModel | null>(null);
const stories = ref<BlogStoryModel[] | null>(null);
const storiesError = ref<string | null>(null);
const selected = ref<Set<string>>(new Set());
const bulkLocale = ref('uz');
const bulkPreset = ref('t02-bone');
const bulkChannels = ref<string[]>(['instagram', 'telegram']);
const bulkTemplate = ref('');
const storyProduct = ref('');
const storyChannels = ref<string[]>(['telegram']);
const days = ref(14);
const weekdays = [
  { value: 1, label: 'Du' },
  { value: 2, label: 'Se' },
  { value: 3, label: 'Ch' },
  { value: 4, label: 'Pa' },
  { value: 5, label: 'Ju' },
  { value: 6, label: 'Sh' },
  { value: 7, label: 'Ya' },
];
const channelLabels: Record<string, string> = {
  instagram: 'Instagram',
  telegram: 'Telegram',
  x: 'X',
  whatsapp: 'WhatsApp',
};
const products = computed(() => workspace.value?.products ?? []);
const presets = computed(() => workspace.value?.presets ?? []);
const approvedWaiting = computed(
  () =>
    workspace.value?.drafts.filter(
      (d) =>
        d.approvedRevision === d.revision &&
        !workspace.value?.schedules.some((s) => s.draftId === d.id && s.status !== 'cancelled'),
    ).length ?? 0,
);
const upcoming = computed(() =>
  (workspace.value?.schedules ?? [])
    .filter((s) => s.status === 'planned' || s.status === 'queued')
    .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt))
    .slice(0, 12),
);
function localTime(value: string) {
  return new Date(value).toLocaleString('uz-UZ', {
    timeZone: 'Asia/Tashkent',
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}
function price(value: number | null | undefined) {
  return value === null || value === undefined
    ? '—'
    : `${new Intl.NumberFormat('ru-RU').format(value).replace(/[\u00a0\u202f]/g, ' ')} so‘m`;
}
function toggle(list: string[], value: string) {
  return list.includes(value) ? list.filter((v) => v !== value) : [...list, value];
}
function toggleProduct(id: string) {
  const next = new Set(selected.value);
  if (next.has(id)) next.delete(id);
  else next.add(id);
  selected.value = next;
}
function assetUrl(id: string) {
  return `/api/marketing/assets/${encodeURIComponent(id)}`;
}
watch(bulkLocale, (locale) => {
  bulkTemplate.value = settings.value?.captionTemplates[locale] ?? '';
});
async function loadSettings() {
  const result = await desk.getAutomation();
  if (result.ok) {
    settings.value = structuredClone(result.value);
    bulkLocale.value = result.value.defaultLocale;
    bulkPreset.value = result.value.defaultPresetId;
    bulkChannels.value = [...result.value.defaultChannels];
    bulkTemplate.value = result.value.captionTemplates[result.value.defaultLocale] ?? '';
  } else desk.error.value = desk.failureMessage(result.failure);
}
async function loadRun() {
  const result = await desk.getAutomationStatus();
  if (result.ok) run.value = result.value;
}
async function loadStories() {
  storiesError.value = null;
  const result = await desk.getBlogStories();
  if (result.ok) stories.value = result.value;
  else storiesError.value = desk.failureMessage(result.failure);
}
async function sync() {
  const result = await desk.syncCatalogue();
  if (result.ok) {
    syncReport.value = result.value;
    desk.notice.value = `Katalog: ${result.value.received} ta mahsulot, ${result.value.created} yangi, ${result.value.updated} yangilandi.`;
  }
}
async function saveSettings() {
  if (!settings.value) return;
  const result = await desk.saveAutomation(settings.value);
  if (result.ok) {
    settings.value = structuredClone(result.value);
    desk.notice.value = 'Avtomatika sozlamalari saqlandi.';
  }
}
async function fill() {
  const result = await desk.fillSlots(days.value);
  if (result.ok) {
    fillReport.value = result.value;
    desk.notice.value = `${result.value.planned.length} ta reja yaratildi. Yuborish hali ham alohida tasdiq talab qiladi.`;
  }
}
async function createDrafts() {
  const result = await desk.createDrafts({
    productIds: [...selected.value],
    locale: bulkLocale.value,
    presetId: bulkPreset.value,
    channels: bulkChannels.value,
    template: bulkTemplate.value.trim() ? bulkTemplate.value : null,
  });
  if (result.ok) {
    desk.notice.value = `${result.value.length} ta post qoralamasi yaratildi. Postlar bo‘limida tekshirib, tasdiqlang.`;
    selected.value = new Set();
  }
}
async function draftStory(story: BlogStoryModel) {
  const productId = storyProduct.value || products.value[0]?.id;
  if (!productId) return;
  const result = await desk.draftFromStory({
    link: story.link,
    productId,
    channels: storyChannels.value,
    locale: 'uz',
    presetId: bulkPreset.value,
  });
  if (result.ok) {
    desk.notice.value = 'Blog hikoyasi uchun post qoralamasi yaratildi.';
    await loadStories();
  }
}
onMounted(async () => {
  await loadSettings();
  await loadRun();
  await loadStories();
});
</script>

<template>
  <div class="space-y-6">
    <div>
      <p class="text-xs uppercase tracking-widest text-primary">Avtomatika</p>
      <h1 class="mt-2 font-editorial text-3xl">Postlar oqimi</h1>
      <p class="mt-3 max-w-3xl text-sm text-muted-foreground">
        Katalogdan mahsulotlarni oling, bir bosishda post qoralamalarini yarating va tasdiqlanganlarini haftalik
        jadvalga joylang. Jadval faqat reja tuzadi: tarmoqqa yuborish uchun tasdiqlangan hisob va alohida navbat
        tasdig‘i kerak.
      </p>
    </div>

    <section v-if="settings" class="rounded-2xl border border-border bg-card p-6" aria-labelledby="unattended-heading">
      <h2 id="unattended-heading" class="flex items-center gap-2 font-editorial text-2xl">
        <Icon :icon="Zap" :size="18" />Avtomatik rejim
      </h2>
      <p class="mt-1 text-sm text-muted-foreground">
        Server har 5 daqiqada tekshiradi. Katalogni yangilaydi va tasdiqlangan postlarni bo‘sh vaqtlarga rejalashtiradi
        — hech narsani o‘zi yubormaydi.
      </p>
      <div class="mt-4 flex flex-wrap items-end gap-4 text-sm">
        <label class="block text-xs font-semibold"
          >Katalogni har necha soatda yangilash (0 — o‘chiq)
          <input
            v-model.number="settings.autoSyncHours"
            type="number"
            min="0"
            max="168"
            class="mt-1 block w-28 rounded-lg border border-border bg-background px-3 py-2 text-sm"
          />
        </label>
        <label class="block text-xs font-semibold"
          >Necha kun oldinga rejalashtirish (0 — o‘chiq)
          <input
            v-model.number="settings.autoFillDays"
            type="number"
            min="0"
            max="60"
            class="mt-1 block w-28 rounded-lg border border-border bg-background px-3 py-2 text-sm"
          />
        </label>
        <Button size="sm" :is-disabled="desk.busy.value !== null" @click="saveSettings">Saqlash</Button>
      </div>
      <p v-if="run" class="mt-3 text-xs text-muted-foreground">
        Oxirgi katalog yangilanishi: {{ run.lastSyncAt ? localTime(run.lastSyncAt) : '—' }} · oxirgi rejalashtirish:
        {{ run.lastFillAt ? `${localTime(run.lastFillAt)} (${run.lastFillPlanned} ta)` : '—' }}
        <span v-if="run.lastError" class="block text-warning"
          >Oxirgi xato ({{ run.lastErrorAt ? localTime(run.lastErrorAt) : '' }}): {{ run.lastError }}</span
        >
      </p>
    </section>

    <section class="rounded-2xl border border-border bg-card p-6" aria-labelledby="catalogue-heading">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 id="catalogue-heading" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="RefreshCw" :size="18" />Ocharo Katalog
          </h2>
          <p class="mt-1 text-sm text-muted-foreground">
            {{ products.length }} ta mahsulot ish stolida. Katalogdagi faol mahsulotlar, narxlar va uch tildagi matn shu
            yerga keladi.
          </p>
        </div>
        <Button :is-disabled="desk.busy.value !== null" @click="sync"
          ><Icon :icon="RefreshCw" :size="15" class="mr-2" />Katalogdan yangilash</Button
        >
      </div>
      <label v-if="settings" class="mt-4 flex items-center gap-2 text-sm">
        <input v-model="settings.draftNewCatalogueProducts" type="checkbox" @change="saveSettings" />
        Katalogda yangi mahsulot paydo bo‘lsa, avtomatik post qoralamasi yaratilsin
      </label>
      <div v-if="syncReport" class="mt-4 rounded-xl bg-muted p-4 text-sm">
        {{ syncReport.received }} olindi · {{ syncReport.created }} yangi · {{ syncReport.updated }} yangilandi ·
        {{ syncReport.imagesStored }} rasm saqlandi
        <span v-if="syncReport.draftIds.length"> · {{ syncReport.draftIds.length }} qoralama yaratildi</span>
        <ul v-if="syncReport.warnings.length" class="mt-2 list-disc pl-5 text-xs text-muted-foreground">
          <li v-for="w in syncReport.warnings" :key="w">{{ w }}</li>
        </ul>
      </div>
    </section>

    <section class="rounded-2xl border border-border bg-card p-6" aria-labelledby="bulk-heading">
      <h2 id="bulk-heading" class="flex items-center gap-2 font-editorial text-2xl">
        <Icon :icon="Sparkles" :size="18" />Post yaratish
      </h2>
      <p class="mt-1 text-sm text-muted-foreground">
        Mahsulotlarni tanlang — har biri uchun shablon asosida qoralama yaratiladi (tasdiqlash sizda).
      </p>
      <ul class="mt-4 grid grid-cols-[repeat(auto-fill,minmax(130px,1fr))] gap-3">
        <li v-for="p in products" :key="p.id">
          <button
            :class="[
              'w-full rounded-xl border p-2 text-left transition',
              selected.has(p.id) ? 'border-primary bg-primary-soft' : 'border-border hover:bg-muted',
            ]"
            :aria-pressed="selected.has(p.id)"
            @click="toggleProduct(p.id)"
          >
            <img
              :src="assetUrl(p.heroAssetId)"
              :alt="p.name"
              class="aspect-[3/4] w-full rounded-lg bg-background object-contain p-2"
              loading="lazy"
            />
            <p class="mt-2 truncate text-xs font-semibold">{{ p.name }}</p>
            <p class="truncate text-[10px] text-muted-foreground">{{ p.id }} · {{ price(p.price) }}</p>
          </button>
        </li>
      </ul>
      <div class="mt-5 grid gap-4 lg:grid-cols-[1fr_1fr_1.4fr]">
        <div class="space-y-3">
          <label class="block text-xs font-semibold"
            >Til
            <select
              v-model="bulkLocale"
              class="mt-1 w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
            >
              <option value="uz">O‘zbekcha</option>
              <option value="ru">Ruscha</option>
              <option value="en">Inglizcha</option>
            </select>
          </label>
          <label class="block text-xs font-semibold"
            >Kartochka
            <select
              v-model="bulkPreset"
              class="mt-1 w-full rounded-lg border border-border bg-background px-3 py-2 text-sm"
            >
              <option v-for="preset in presets" :key="preset.id" :value="preset.id">{{ preset.name }}</option>
            </select>
          </label>
        </div>
        <fieldset class="space-y-2 text-sm">
          <legend class="text-xs font-semibold">Kanallar</legend>
          <label v-for="c in ['instagram', 'telegram', 'x', 'whatsapp']" :key="c" class="flex items-center gap-2">
            <input
              type="checkbox"
              :checked="bulkChannels.includes(c)"
              @change="bulkChannels = toggle(bulkChannels, c)"
            />{{ channelLabels[c] }}
          </label>
        </fieldset>
        <label class="block text-xs font-semibold"
          >Matn shabloni
          <textarea
            v-model="bulkTemplate"
            rows="7"
            class="mt-1 w-full rounded-lg border border-border bg-background px-3 py-2 font-mono text-xs"
          ></textarea>
          <span class="mt-1 block font-normal text-muted-foreground"
            >{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — bo‘sh qiymatli qator tushib
            qoladi.</span
          >
        </label>
      </div>
      <div class="mt-4 flex justify-end">
        <Button
          :is-disabled="selected.size === 0 || bulkChannels.length === 0 || desk.busy.value !== null"
          @click="createDrafts"
        >
          {{ selected.size }} ta qoralama yaratish
        </Button>
      </div>
    </section>

    <section v-if="settings" class="rounded-2xl border border-border bg-card p-6" aria-labelledby="slots-heading">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 id="slots-heading" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="CalendarClock" :size="18" />Joylash jadvali
          </h2>
          <p class="mt-1 text-sm text-muted-foreground">
            Toshkent vaqti. {{ approvedWaiting }} ta tasdiqlangan post rejasiz turibdi.
          </p>
        </div>
        <div class="flex items-center gap-2">
          <label class="text-xs"
            >Kun
            <input
              v-model.number="days"
              type="number"
              min="1"
              max="60"
              class="ml-1 w-16 rounded-lg border border-border bg-background px-2 py-1.5 text-sm"
            />
          </label>
          <Button :is-disabled="desk.busy.value !== null" @click="fill"
            ><Icon :icon="Zap" :size="15" class="mr-2" />Bo‘sh vaqtlarga joylash</Button
          >
        </div>
      </div>
      <table class="mt-4 w-full text-sm">
        <thead class="text-left text-[11px] uppercase tracking-wider text-muted-foreground">
          <tr>
            <th class="py-2">Kanal</th>
            <th>Kunlar</th>
            <th>Vaqt</th>
            <th>Faol</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(slot, index) in settings.slots" :key="index" class="border-t border-border">
            <td class="py-2 pr-2">
              <select
                v-model="slot.channel"
                class="rounded-lg border border-border bg-background px-2 py-1.5"
                aria-label="Kanal"
              >
                <option value="telegram">Telegram</option>
                <option value="instagram">Instagram</option>
                <option value="x">X</option>
              </select>
            </td>
            <td class="pr-2">
              <div class="flex flex-wrap gap-1">
                <button
                  v-for="d in weekdays"
                  :key="d.value"
                  :class="[
                    'rounded-md px-2 py-1 text-xs',
                    slot.days.includes(d.value)
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted text-muted-foreground',
                  ]"
                  :aria-pressed="slot.days.includes(d.value)"
                  @click="
                    slot.days = slot.days.includes(d.value)
                      ? slot.days.filter((x) => x !== d.value)
                      : [...slot.days, d.value]
                  "
                >
                  {{ d.label }}
                </button>
              </div>
            </td>
            <td class="pr-2">
              <input
                v-model="slot.time"
                type="time"
                class="rounded-lg border border-border bg-background px-2 py-1.5"
                aria-label="Vaqt"
              />
            </td>
            <td><input v-model="slot.enabled" type="checkbox" aria-label="Faol" /></td>
            <td class="text-right">
              <button
                class="rounded p-1 text-muted-foreground hover:text-destructive"
                aria-label="Slotni o‘chirish"
                @click="settings.slots.splice(index, 1)"
              >
                <Icon :icon="Trash2" :size="14" />
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <div class="mt-3 flex gap-2">
        <Button
          variant="ghost"
          size="sm"
          @click="settings.slots.push({ id: '', channel: 'telegram', days: [1, 3, 5], time: '19:00', enabled: true })"
        >
          <Icon :icon="Plus" :size="14" class="mr-1" />Slot
        </Button>
        <Button size="sm" :is-disabled="desk.busy.value !== null" @click="saveSettings">Jadvalni saqlash</Button>
      </div>
      <div v-if="fillReport" class="mt-4 rounded-xl bg-muted p-4 text-sm">
        <p class="font-semibold">{{ fillReport.planned.length }} ta yangi reja</p>
        <ul class="mt-2 space-y-1 text-xs">
          <li v-for="p in fillReport.planned" :key="p.id">
            {{ localTime(p.scheduledAt) }} · {{ channelLabels[p.channel] ?? p.channel }} · {{ p.draftTitle }}
          </li>
        </ul>
        <ul v-if="fillReport.skipped.length" class="mt-2 list-disc pl-5 text-xs text-muted-foreground">
          <li v-for="s in fillReport.skipped" :key="s">{{ s }}</li>
        </ul>
      </div>
      <div v-if="upcoming.length" class="mt-4">
        <p class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Yaqin rejalar</p>
        <ul class="mt-2 grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
          <li v-for="s in upcoming" :key="s.id" class="rounded-lg border border-border px-3 py-2 text-xs">
            <span class="font-semibold">{{ localTime(s.scheduledAt) }}</span> ·
            {{ channelLabels[s.channel] ?? s.channel }}<br />
            <span class="text-muted-foreground">{{ s.draftTitle ?? s.draftId }} · {{ s.status }}</span>
          </li>
        </ul>
      </div>
    </section>

    <section class="rounded-2xl border border-border bg-card p-6" aria-labelledby="blog-heading">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 id="blog-heading" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="BookOpen" :size="18" />Blog hikoyalari
          </h2>
          <p class="mt-1 text-sm text-muted-foreground">
            Ocharo Blogdagi nashr qilingan hikoyani e’lon qiluvchi post qoralamasi.
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-2 text-sm">
          <select
            v-model="storyProduct"
            class="rounded-lg border border-border bg-background px-2 py-1.5"
            aria-label="Rasm uchun mahsulot"
          >
            <option value="">Rasm: birinchi mahsulot</option>
            <option v-for="p in products" :key="p.id" :value="p.id">Rasm: {{ p.name }} · {{ p.id }}</option>
          </select>
          <label v-for="c in ['telegram', 'instagram', 'x']" :key="c" class="flex items-center gap-1">
            <input
              type="checkbox"
              :checked="storyChannels.includes(c)"
              @change="storyChannels = toggle(storyChannels, c)"
            />{{ channelLabels[c] }}
          </label>
          <Button variant="ghost" size="sm" @click="loadStories"><Icon :icon="RefreshCw" :size="14" /></Button>
        </div>
      </div>
      <p v-if="storiesError" class="mt-4 rounded-xl bg-warning-soft p-4 text-sm text-warning-soft-foreground">
        {{ storiesError }}
      </p>
      <ul v-else-if="stories" class="mt-4 divide-y divide-border">
        <li
          v-for="story in stories.slice(0, 20)"
          :key="story.link"
          class="flex flex-wrap items-center justify-between gap-3 py-3"
        >
          <div class="min-w-0">
            <a :href="story.link" target="_blank" rel="noopener" class="font-editorial text-lg hover:text-primary">{{
              story.title
            }}</a>
            <p class="truncate text-xs text-muted-foreground">{{ story.topic }} · {{ story.excerpt }}</p>
          </div>
          <span v-if="story.draftId" class="rounded-full bg-success-soft px-3 py-1 text-xs text-success-soft-foreground"
            >Qoralama bor</span
          >
          <Button
            v-else
            size="sm"
            :is-disabled="storyChannels.length === 0 || desk.busy.value !== null || !products.length"
            @click="draftStory(story)"
            >Post qoralamasi</Button
          >
        </li>
      </ul>
    </section>
  </div>
</template>
