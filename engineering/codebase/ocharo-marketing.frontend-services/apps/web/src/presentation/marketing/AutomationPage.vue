<script setup lang="ts">
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import { useErrorToast } from '../../application/shared/UseFeedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onMounted, ref, watch } from 'vue';
import { BookOpen, CalendarClock, Plus, RefreshCw, Sparkles, Trash2, Zap } from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useMarketingWorkspace } from '../../application/marketing';
import type {
  AutomationRunModel,
  AutomationSettingsModel,
  BlogStoryModel,
  CatalogueSyncReportModel,
  SlotFillReportModel,
} from '../../domain/marketing';
const { t: ui, formatDate } = useUiLocale();

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
useErrorToast(storiesError);
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
const shortWeekdays: Record<string, string> = {
  Mon: 'Du',
  Tue: 'Se',
  Wed: 'Ch',
  Thu: 'Pa',
  Fri: 'Ju',
  Sat: 'Sh',
  Sun: 'Ya',
};
function localTime(value: string) {
  const weekday = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Tashkent', weekday: 'short' }).format(
    new Date(value),
  );
  return `${ui(shortWeekdays[weekday] ?? '')} ${formatDate(value, { hour: '2-digit', minute: '2-digit' })}`.trim();
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
    await loadSettings();
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
      <p class="text-xs uppercase tracking-widest text-primary">{{ ui('Automation') }}</p>
      <h1 class="mt-2 font-editorial text-3xl">{{ ui('Post flow') }}</h1>
      <p class="mt-3 max-w-3xl text-sm text-muted-foreground">
        {{
          ui(
            'Import catalogue products, create post drafts in one click and place approved posts into weekly slots. Scheduling creates a plan only; delivery needs a verified account and separate queue authorization.',
          )
        }}
      </p>
    </div>

    <section v-if="settings" class="rounded-2xl border border-border bg-card p-6" aria-labelledby="unattended-heading">
      <h2 id="unattended-heading" class="flex items-center gap-2 font-editorial text-2xl">
        <Icon :icon="Zap" :size="18" />{{ ui('Automatic mode') }}
      </h2>
      <p class="mt-1 text-sm text-muted-foreground">
        {{
          ui(
            'The server checks every 5 minutes. It refreshes the catalogue and schedules approved posts into available slots; it does not send anything automatically.',
          )
        }}
      </p>
      <div class="mt-4 flex flex-wrap items-end gap-4 text-sm">
        <label class="block text-xs font-semibold"
          >{{ ui('Catalogue refresh interval in hours (0 — off)') }}
          <input
            v-model.number="settings.autoSyncHours"
            type="number"
            min="0"
            max="168"
            class="mt-1 block w-28 rounded-lg border border-border bg-background px-3 py-2 text-sm"
          />
        </label>
        <label class="block text-xs font-semibold"
          >{{ ui('Days to schedule ahead (0 — off)') }}
          <input
            v-model.number="settings.autoFillDays"
            type="number"
            min="0"
            max="60"
            class="mt-1 block w-28 rounded-lg border border-border bg-background px-3 py-2 text-sm"
          />
        </label>
        <Button size="sm" :is-disabled="desk.busy.value !== null" @click="saveSettings">{{ ui('Save') }}</Button>
      </div>
      <p v-if="run" class="mt-3 text-xs text-muted-foreground">
        {{ ui('Last catalogue refresh:') }} {{ run.lastSyncAt ? localTime(run.lastSyncAt) : '—' }}
        {{ ui('· Last scheduling:') }}
        {{ run.lastFillAt ? `${localTime(run.lastFillAt)} (${run.lastFillPlanned})` : '—' }}
        <span v-if="run.lastError" class="block text-warning"
          >{{ ui('Last error (') }}{{ run.lastErrorAt ? localTime(run.lastErrorAt) : '' }}):
          {{ ui('Needs attention') }}</span
        >
      </p>
    </section>

    <section class="rounded-2xl border border-border bg-card p-6" aria-labelledby="catalogue-heading">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 id="catalogue-heading" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="RefreshCw" :size="18" />{{ ui('Ocharo Catalogue') }}
          </h2>
          <p class="mt-1 text-sm text-muted-foreground">
            {{ products.length }}
            {{
              ui(
                'products in the workspace. Active catalogue products, prices and text in three languages appear here.',
              )
            }}
          </p>
        </div>
        <Button :is-disabled="desk.busy.value !== null" @click="sync"
          ><Icon :icon="RefreshCw" :size="15" class="mr-2" />{{ ui('Refresh from catalogue') }}</Button
        >
      </div>
      <label v-if="settings" class="mt-4 flex items-center gap-2 text-sm">
        <input v-model="settings.draftNewCatalogueProducts" type="checkbox" @change="saveSettings" />
        {{ ui('Automatically create a post draft when a new product appears in the catalogue') }}
      </label>
      <div v-if="syncReport" class="mt-4 rounded-xl bg-muted p-4 text-sm">
        {{ syncReport.received }} {{ ui('received ·') }} {{ syncReport.created }} {{ ui('new ·') }}
        {{ syncReport.updated }} {{ ui('updated ·') }} {{ syncReport.imagesStored }} {{ ui('images saved') }}
        <span v-if="syncReport.draftIds.length"> · {{ syncReport.draftIds.length }} {{ ui('drafts created') }}</span>
        <ul v-if="syncReport.warnings.length" class="mt-2 list-disc pl-5 text-xs text-muted-foreground">
          <li v-for="w in syncReport.warnings" :key="w">{{ w }}</li>
        </ul>
      </div>
    </section>

    <section class="rounded-2xl border border-border bg-card p-6" aria-labelledby="bulk-heading">
      <h2 id="bulk-heading" class="flex items-center gap-2 font-editorial text-2xl">
        <Icon :icon="Sparkles" :size="18" />{{ ui('Create post') }}
      </h2>
      <p class="mt-1 text-sm text-muted-foreground">
        {{ ui('Select products to create template-based drafts for each one. You approve them.') }}
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
            >{{ ui('Language') }}
            <MarketingSelect
              v-model="bulkLocale"
              class="mt-1 w-full"
              :label="ui('Language')"
              :options="[
                { value: 'uz' as const, label: 'O‘zbekcha' },
                { value: 'ru' as const, label: ui('Russian') },
                { value: 'en' as const, label: ui('English') },
              ]"
              :disabled="desk.busy.value !== null"
            />
          </label>
          <label class="block text-xs font-semibold"
            >{{ ui('Card') }}
            <MarketingSelect
              v-model="bulkPreset"
              class="mt-1 w-full"
              :label="ui('Card')"
              :options="presets.map((preset) => ({ value: preset.id, label: preset.name }))"
              :disabled="desk.busy.value !== null"
            />
          </label>
        </div>
        <fieldset class="space-y-2 text-sm">
          <legend class="text-xs font-semibold">{{ ui('Channels') }}</legend>
          <label v-for="c in ['instagram', 'telegram', 'x', 'whatsapp']" :key="c" class="flex items-center gap-2">
            <input
              type="checkbox"
              :checked="bulkChannels.includes(c)"
              @change="bulkChannels = toggle(bulkChannels, c)"
            />{{ channelLabels[c] }}
          </label>
        </fieldset>
        <label class="block text-xs font-semibold"
          >{{ ui('Caption template') }}
          <textarea
            v-model="bulkTemplate"
            rows="7"
            class="mt-1 w-full rounded-lg border border-border bg-background px-3 py-2 font-mono text-xs"
          ></textarea>
          <span class="mt-1 block font-normal text-muted-foreground">{{
            ui(
              '{name} {fabric} {decoration} {sizes} {price} {wholesale} {brand} — lines with empty values are omitted.',
            )
          }}</span>
        </label>
      </div>
      <div class="mt-4 flex justify-end">
        <Button
          :is-disabled="selected.size === 0 || bulkChannels.length === 0 || desk.busy.value !== null"
          @click="createDrafts"
        >
          {{ selected.size }} {{ ui('drafts to create') }}
        </Button>
      </div>
    </section>

    <section v-if="settings" class="rounded-2xl border border-border bg-card p-6" aria-labelledby="slots-heading">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 id="slots-heading" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="CalendarClock" :size="18" />{{ ui('Publishing schedule') }}
          </h2>
          <p class="mt-1 text-sm text-muted-foreground">
            {{ ui('Tashkent time.') }} {{ approvedWaiting }} {{ ui('approved posts have no plan.') }}
          </p>
        </div>
        <div class="flex items-center gap-2">
          <label class="text-xs"
            >{{ ui('Day') }}
            <input
              v-model.number="days"
              type="number"
              min="1"
              max="60"
              class="ml-1 w-16 rounded-lg border border-border bg-background px-2 py-1.5 text-sm"
            />
          </label>
          <Button :is-disabled="desk.busy.value !== null" @click="fill"
            ><Icon :icon="Zap" :size="15" class="mr-2" />{{ ui('Fill available slots') }}</Button
          >
        </div>
      </div>
      <table class="mt-4 w-full text-sm">
        <thead class="text-left text-[11px] uppercase tracking-wider text-muted-foreground">
          <tr>
            <th class="py-2">{{ ui('Channel') }}</th>
            <th>{{ ui('Days') }}</th>
            <th>{{ ui('Time') }}</th>
            <th>{{ ui('Active') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(slot, index) in settings.slots" :key="index" class="border-t border-border">
            <td class="py-2 pr-2">
              <MarketingSelect
                v-model="slot.channel"
                :label="ui('Channel')"
                :options="[
                  { value: 'telegram' as const, label: 'Telegram' },
                  { value: 'instagram' as const, label: 'Instagram' },
                  { value: 'x' as const, label: 'X' },
                ]"
                :disabled="desk.busy.value !== null"
              />
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
                  {{ ui(d.label) }}
                </button>
              </div>
            </td>
            <td class="pr-2">
              <input
                v-model="slot.time"
                type="time"
                class="rounded-lg border border-border bg-background px-2 py-1.5"
                :aria-label="ui('Time')"
              />
            </td>
            <td><input v-model="slot.enabled" type="checkbox" :aria-label="ui('Active')" /></td>
            <td class="text-right">
              <button
                class="rounded p-1 text-muted-foreground hover:text-destructive"
                :aria-label="ui('Delete slot')"
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
          <Icon :icon="Plus" :size="14" class="mr-1" />{{ ui('Slot') }}
        </Button>
        <Button size="sm" :is-disabled="desk.busy.value !== null" @click="saveSettings">{{
          ui('Save schedule')
        }}</Button>
      </div>
      <div v-if="fillReport" class="mt-4 rounded-xl bg-muted p-4 text-sm">
        <p class="font-semibold">{{ fillReport.planned.length }} {{ ui('new plans') }}</p>
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
        <p class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">{{ ui('Upcoming plans') }}</p>
        <ul class="mt-2 grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
          <li v-for="s in upcoming" :key="s.id" class="rounded-lg border border-border px-3 py-2 text-xs">
            <span class="font-semibold">{{ localTime(s.scheduledAt) }}</span> ·
            {{ channelLabels[s.channel] ?? s.channel }}<br />
            <span class="text-muted-foreground">{{ s.draftTitle ?? s.draftId }} · {{ ui(s.status) }}</span>
          </li>
        </ul>
      </div>
    </section>

    <section class="rounded-2xl border border-border bg-card p-6" aria-labelledby="blog-heading">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 id="blog-heading" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="BookOpen" :size="18" />{{ ui('Blog stories') }}
          </h2>
          <p class="mt-1 text-sm text-muted-foreground">
            {{ ui('A post draft announcing a published Ocharo Blog story.') }}
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-2 text-sm">
          <MarketingSelect
            v-model="storyProduct"
            :label="ui('Product for image')"
            :options="[
              { value: '' as const, label: ui('Image: first product') },
              ...products.map((p) => ({ value: p.id, label: ui('Image:') + ' ' + p.name + ' · ' + p.id })),
            ]"
            :disabled="desk.busy.value !== null"
          />
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

      <ul v-if="stories" class="mt-4 divide-y divide-border">
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
          <span
            v-if="story.draftId"
            class="rounded-full bg-success-soft px-3 py-1 text-xs text-success-soft-foreground"
            >{{ ui('Draft exists') }}</span
          >
          <Button
            v-else
            size="sm"
            :is-disabled="storyChannels.length === 0 || desk.busy.value !== null || !products.length"
            @click="draftStory(story)"
            >{{ ui('Post draft') }}</Button
          >
        </li>
      </ul>
    </section>
  </div>
</template>
