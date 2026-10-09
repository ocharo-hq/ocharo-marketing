<script setup lang="ts">
import MarketingSelect from '@ocharomarketing/connect/MarketingSelect.vue';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import { onBeforeRouteLeave, RouterLink, useRoute, useRouter } from 'vue-router';
import {
  Archive,
  ArrowDown,
  ArrowLeft,
  ArrowUp,
  CalendarClock,
  Eye,
  History,
  ImagePlus,
  Plus,
  RotateCcw,
  Save,
  Send,
  Trash2,
  Undo2,
} from 'lucide-vue-next';
import { Icon } from '@wow-two-beta/ui-vue/foundation/icons';
import { Button } from '@wow-two-beta/ui-vue/presentation/actions';
import { useRefresh } from '@wow-two-beta/ui-vue/query';
import EditorSkeleton from './components/EditorSkeleton.vue';
import { loadStoryOptions, useArticleEditor, useBlogStore } from '../../application/blog';
import { languageLabels, publicationPath } from '../../domain/blog/BlogRules';
import StatusBadge from './components/StatusBadge.vue';
import SectionTextEditor from './components/SectionTextEditor.vue';
import CoverImageEditor from './components/CoverImageEditor.vue';
import type { MediaView } from '../../domain/blog/BlogModels';
const { t: ui, formatWhen } = useUiLocale();

/** Structured story editor with live server preview and publishing controls. */
defineOptions({ name: 'StoryEditorPage' });
const route = useRoute();
const router = useRouter();
const store = useBlogStore();
const editor = useArticleEditor();
const loaded = ref(false);
const imageEditing = ref(false),
  imageBusy = ref(false),
  pendingImageEdits = ref(false);
const unsavedChanges = computed(() => editor.dirty.value || pendingImageEdits.value || imageBusy.value);
const coverMedia = computed(() =>
  editor.media.value.find((media) => media.sha256 === editor.draft.value?.cover?.mediaSha256),
);
const summary = ref('');
const scheduleAt = ref('');
const coverInput = ref<HTMLInputElement | null>(null);
const productPick = ref('');
const id = computed(() => (typeof route.params.id === 'string' ? route.params.id : null));
const { refresh, refreshing } = useRefresh(async () => {
  if (id.value) await editor.load(id.value);
});
const others = ref<
  { id: string; slug: string; title: string; language: string; translationKey: string | null; status: string }[]
>([]);
const groupKey = computed(
  () => editor.detail.value?.document.translationKey ?? editor.detail.value?.document.slug ?? null,
);
const editions = computed(() =>
  others.value.filter(
    (o) =>
      o.id !== editor.detail.value?.id &&
      groupKey.value !== null &&
      (o.translationKey ?? o.slug) === groupKey.value &&
      o.status !== 'archived',
  ),
);
const missingLanguages = computed(() =>
  ['en', 'uz', 'ru'].filter((l) => l !== editor.draft.value?.language && !editions.value.some((e) => e.language === l)),
);
async function createEdition(language: string) {
  const created = await editor.translate(language);
  if (created) await router.push(`/stories/${created}`);
}
const previewUrl = computed(() =>
  editor.detail.value ? `${store.links.preview(editor.detail.value.id)}?v=${editor.previewNonce.value}` : null,
);
const status = computed(() => editor.detail.value?.document.status ?? 'draft');

onMounted(async () => {
  if (!store.topics.value.length) await store.loadTopics();
  if (id.value) await editor.load(id.value);
  else editor.startNew(store.topics.value[0]?.key ?? 'pieces');
  await editor.loadLibraries();
  others.value = await loadStoryOptions();
  loaded.value = true;
});
function warn(event: BeforeUnloadEvent) {
  if (unsavedChanges.value) event.preventDefault();
}
window.addEventListener('beforeunload', warn);
onBeforeUnmount(() => window.removeEventListener('beforeunload', warn));
onBeforeRouteLeave(
  () => !unsavedChanges.value || window.confirm(ui('Saqlanmagan o‘zgarishlar bor. Chiqib ketilsinmi?')),
);

async function save() {
  if (imageBusy.value || pendingImageEdits.value || editor.busy.value) return;
  const wasNew = !editor.detail.value;
  const saved = await editor.save(summary.value || undefined);
  if (saved) summary.value = '';
  if (saved && wasNew) await router.replace(`/stories/${saved}`);
}
function toggleSource(index: number, source: number) {
  const section = editor.draft.value?.sections[index];
  if (!section) return;
  const set = new Set(section.sourceIndexes ?? []);
  if (set.has(source)) set.delete(source);
  else set.add(source);
  section.sourceIndexes = set.size ? [...set].sort((a, b) => a - b) : null;
}
function moveSection(index: number, delta: number) {
  const list = editor.draft.value?.sections;
  if (!list) return;
  const target = index + delta;
  if (target < 0 || target >= list.length) return;
  const [item] = list.splice(index, 1);
  if (item) list.splice(target, 0, item);
}
function removeSource(index: number) {
  const draft = editor.draft.value;
  if (!draft) return;
  draft.sources.splice(index, 1);
  for (const section of draft.sections)
    section.sourceIndexes = (section.sourceIndexes ?? [])
      .filter((i) => i !== index)
      .map((i) => (i > index ? i - 1 : i));
}
async function onCover(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0],
    draft = editor.draft.value;
  if (!file || !draft || imageBusy.value || editor.busy.value || !editor.canEdit.value) return;
  const originalSha = draft.cover?.mediaSha256;
  imageBusy.value = true;
  try {
    const stored = await editor.upload(file);
    if (stored && editor.draft.value === draft && draft.cover?.mediaSha256 === originalSha)
      draft.cover = { mediaSha256: stored.sha256, alt: '', caption: '', cardClass: null };
  } finally {
    imageBusy.value = false;
    if (coverInput.value) coverInput.value.value = '';
  }
}
function applyEditedCover(media: MediaView) {
  const cover = editor.draft.value?.cover;
  if (!cover || !imageEditing.value || !editor.canEdit.value) return;
  editor.media.value = [media, ...editor.media.value.filter((item) => item.sha256 !== media.sha256)];
  editor.draft.value!.cover = { ...cover, mediaSha256: media.sha256 };
  imageEditing.value = false;
}
function chooseCover(sha: string) {
  if (
    !sha ||
    !editor.draft.value ||
    !editor.canEdit.value ||
    editor.busy.value ||
    editor.loading.value ||
    refreshing.value ||
    imageBusy.value
  )
    return;
  editor.draft.value.cover = { mediaSha256: sha, alt: '', caption: '', cardClass: null };
}
function chooseRelated(slot: number, value: string) {
  if (!editor.draft.value || !editor.canEdit.value || editor.busy.value || editor.loading.value || refreshing.value)
    return;
  const next = [...editor.draft.value.relatedSlugs];
  next[slot] = value;
  editor.draft.value.relatedSlugs = next.filter(Boolean);
}
function addProduct() {
  if (productPick.value && editor.draft.value && !editor.draft.value.productIds.includes(productPick.value))
    editor.draft.value.productIds.push(productPick.value);
  productPick.value = '';
}
</script>

<template>
  <div class="mb-4 flex justify-end">
    <Button
      v-if="id"
      variant="outline"
      :is-disabled="editor.busy.value || editor.loading.value || refreshing || unsavedChanges"
      :is-loading="refreshing"
      @click="refresh()"
      >{{ ui('Refresh') }}</Button
    >
  </div>
  <EditorSkeleton v-if="editor.loading.value && !editor.draft.value" />
  <section v-if="editor.draft.value" :aria-busy="editor.loading.value || refreshing" class="pb-28">
    <fieldset :disabled="editor.busy.value || editor.loading.value || refreshing" class="min-w-0 border-0 p-0">
      <RouterLink to="/" class="btn-ghost -ml-3 mb-3"
        ><Icon :icon="ArrowLeft" :size="15" />{{ ui('Stories') }}</RouterLink
      >
      <div class="flex flex-wrap items-start justify-between gap-4">
        <div class="min-w-0">
          <h1 class="font-editorial text-4xl tracking-tight">{{ editor.draft.value.title || ui('New story') }}</h1>
          <p class="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
            <StatusBadge :status="status" />
            <span v-if="editor.detail.value">{{ ui('version') }} {{ editor.detail.value.revision }}</span>
            <span
              >{{ editor.stats.value.words }} {{ ui('words ·') }} {{ editor.stats.value.minutes }}
              {{ ui('min read') }}</span
            >
            <a
              v-if="status === 'published'"
              :href="publicationPath(editor.draft.value)"
              target="_blank"
              rel="noopener"
              class="text-primary underline"
              >{{ ui('open on site') }}</a
            >
          </p>
        </div>
      </div>

      <div class="mt-6 grid grid-cols-1 gap-6 2xl:grid-cols-[minmax(0,1fr)_minmax(460px,0.9fr)]">
        <div class="space-y-6">
          <section class="panel" aria-labelledby="publication-meta">
            <h2 id="publication-meta" class="font-editorial text-2xl">{{ ui('Publication & provenance') }}</h2>
            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              <label class="label"
                >{{ ui('Publication')
                }}<MarketingSelect
                  v-model="editor.draft.value.publication"
                  :label="ui('Publication')"
                  :options="[
                    { value: 'blog' as const, label: 'Ocharo Blog' },
                    { value: 'atlas' as const, label: 'Ocharo Atlas' },
                    { value: 'wear' as const, label: 'Ocharo Wear' },
                  ]"
                  :disabled="editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value"
                  @update:model-value="
                    editor.draft.value.kind = editor.draft.value.publication === 'wear' ? 'guide' : 'story'
                  "
              /></label>
              <label class="label"
                >{{ ui('Content type')
                }}<MarketingSelect
                  v-model="editor.draft.value.kind"
                  :label="ui('Content type')"
                  :options="[
                    { value: 'story' as const, label: ui('Story') },
                    { value: 'textile' as const, label: ui('Textile object') },
                    { value: 'archive' as const, label: ui('Archive encounter') },
                    { value: 'guide' as const, label: ui('Guide') },
                  ]"
                  :disabled="editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value"
              /></label>
              <label
                v-for="axis in ['places', 'techniques', 'motifs', 'occasions', 'textileSlugs'] as const"
                :key="axis"
                class="label"
                >{{ ui(axis) }} {{ ui('(comma separated)')
                }}<input
                  class="field"
                  :value="editor.draft.value[axis]?.join(', ')"
                  @input="
                    editor.draft.value[axis] = ($event.target as HTMLInputElement).value
                      .split(',')
                      .map((v) => v.trim())
                      .filter(Boolean)
                  "
              /></label>
            </div>
            <button
              v-if="!editor.draft.value.textile"
              class="btn-ghost mt-3"
              @click="
                editor.draft.value.textile = {
                  objectId: '',
                  collection: '',
                  place: '',
                  period: '',
                  materials: '',
                  technique: '',
                  dimensions: null,
                  maker: null,
                  attributionNote: null,
                }
              "
            >
              {{ ui('Add object record') }}
            </button>
            <div v-if="editor.draft.value.textile" class="mt-4 grid gap-3 sm:grid-cols-2">
              <label
                v-for="field in [
                  'objectId',
                  'collection',
                  'place',
                  'period',
                  'materials',
                  'technique',
                  'dimensions',
                  'maker',
                  'attributionNote',
                ] as const"
                :key="field"
                class="label"
                >{{ ui(field) }}<input v-model="editor.draft.value.textile[field]" class="field"
              /></label>
            </div>
            <button
              v-if="!editor.draft.value.imageProvenance"
              class="btn-ghost mt-3"
              @click="
                editor.draft.value.imageProvenance = { kind: 'photograph', credit: '', rights: '', sourceUrl: '' }
              "
            >
              {{ ui('Add image provenance') }}
            </button>
            <div v-if="editor.draft.value.imageProvenance" class="mt-4 grid gap-3 sm:grid-cols-2">
              <label v-for="field in ['kind', 'credit', 'rights', 'sourceUrl'] as const" :key="field" class="label"
                >{{ ui(field) }}<input v-model="editor.draft.value.imageProvenance[field]" class="field"
              /></label>
            </div>
            <p class="mt-3 text-xs text-muted-foreground">
              {{ ui('Record uncertainty in the attribution note. Every save retains this metadata in its revision.') }}
            </p>
          </section>
          <section class="panel" aria-labelledby="basics">
            <h2 id="basics" class="font-editorial text-2xl">{{ ui('Main') }}</h2>
            <div class="mt-4 grid gap-4">
              <div>
                <label class="label" for="title">{{ ui('Title') }}</label>
                <input
                  id="title"
                  v-model="editor.draft.value.title"
                  class="field text-base"
                  @input="editor.titleChanged()"
                />
              </div>
              <div class="grid gap-4 sm:grid-cols-3">
                <div class="sm:col-span-2">
                  <label class="label" for="slug">{{ ui('URL (/journal/…/)') }}</label>
                  <input
                    id="slug"
                    v-model="editor.draft.value.slug"
                    class="field font-mono text-xs"
                    @input="editor.slugEdited()"
                  />
                </div>
                <div>
                  <label class="label" for="topic">{{ ui('Topic') }}</label>
                  <MarketingSelect
                    id="topic"
                    v-model="editor.draft.value.topic"
                    :label="ui('Topic')"
                    :options="store.topics.value.map((t) => ({ value: t.key, label: t.short }))"
                    :disabled="editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value"
                  />
                </div>
              </div>
              <div class="grid gap-4 sm:grid-cols-3">
                <div>
                  <label class="label" for="format">{{ ui('Format') }}</label
                  ><input
                    id="format"
                    v-model="editor.draft.value.format"
                    class="field"
                    :placeholder="ui('Wardrobe puzzle')"
                  />
                </div>
                <div>
                  <label class="label" for="prompt">{{ ui('Card phrase') }}</label
                  ><input
                    id="prompt"
                    v-model="editor.draft.value.cardPrompt"
                    class="field"
                    :placeholder="ui('Borrow the idea.')"
                  />
                </div>
                <div>
                  <label class="label" for="lang">{{ ui('Language') }}</label>
                  <MarketingSelect
                    id="lang"
                    v-model="editor.draft.value.language"
                    :label="ui('Language')"
                    :options="[
                      { value: 'en' as const, label: ui('English') },
                      { value: 'uz' as const, label: 'O‘zbekcha' },
                      { value: 'ru' as const, label: 'Русский' },
                    ]"
                    :disabled="editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value"
                  />
                </div>
              </div>
              <div>
                <label class="label" for="excerpt">{{ ui('Short description (card and feed)') }}</label
                ><textarea id="excerpt" v-model="editor.draft.value.excerpt" rows="2" class="field"></textarea>
              </div>
              <div>
                <label class="label" for="intro">{{ ui('Introduction') }}</label
                ><textarea id="intro" v-model="editor.draft.value.intro" rows="4" class="field"></textarea>
              </div>
              <div>
                <label class="label" for="promise">{{ ui('Reader promise (internal, not published)') }}</label
                ><input id="promise" v-model="editor.draft.value.readerPromise" class="field" />
              </div>
            </div>
          </section>

          <section class="panel" aria-labelledby="sections">
            <div class="flex items-center justify-between">
              <h2 id="sections" class="font-editorial text-2xl">{{ ui('Topics') }}</h2>
              <button
                class="btn-outline"
                @click="
                  editor.draft.value.sections.push({ heading: '', paragraphs: [], list: null, sourceIndexes: null })
                "
              >
                <Icon :icon="Plus" :size="14" />{{ ui('Topic') }}
              </button>
            </div>
            <ol class="mt-4 space-y-4">
              <li
                v-for="(section, index) in editor.draft.value.sections"
                :key="index"
                class="rounded-xl border border-border p-4"
              >
                <div class="flex items-center gap-2">
                  <span class="w-6 text-xs text-muted-foreground">{{ index + 1 }}</span>
                  <input
                    v-model="section.heading"
                    class="field font-semibold"
                    :placeholder="ui('Topic heading')"
                    :aria-label="ui('Topic heading')"
                  />
                  <button class="btn-ghost px-1.5" :aria-label="ui('Move up')" @click="moveSection(index, -1)">
                    <Icon :icon="ArrowUp" :size="13" />
                  </button>
                  <button class="btn-ghost px-1.5" :aria-label="ui('Move down')" @click="moveSection(index, 1)">
                    <Icon :icon="ArrowDown" :size="13" />
                  </button>
                  <button
                    class="btn-ghost px-1.5 text-destructive"
                    :aria-label="ui('Delete topic')"
                    @click="editor.draft.value.sections.splice(index, 1)"
                  >
                    <Icon :icon="Trash2" :size="13" />
                  </button>
                </div>
                <SectionTextEditor
                  :id="`p-${index}`"
                  :section="section"
                  :disabled="editor.busy.value || !editor.canEdit.value"
                  @change="editor.draft.value.sections[index] = $event"
                />
                <div v-if="editor.draft.value.sources.length" class="mt-3 flex flex-wrap items-center gap-2 text-xs">
                  <span class="text-muted-foreground">{{ ui('Sources:') }}</span>
                  <button
                    v-for="(source, s) in editor.draft.value.sources"
                    :key="s"
                    :class="[
                      'chip',
                      section.sourceIndexes?.includes(s)
                        ? 'bg-primary-soft text-primary'
                        : 'bg-muted text-muted-foreground',
                    ]"
                    @click="toggleSource(index, s)"
                  >
                    {{ s + 1 }}. {{ source.label.slice(0, 28) }}
                  </button>
                </div>
              </li>
            </ol>
          </section>

          <section class="panel" aria-labelledby="closing">
            <h2 id="closing" class="font-editorial text-2xl">{{ ui('Conclusion') }}</h2>
            <div class="mt-4 grid gap-4">
              <div>
                <label class="label" for="takeaway">{{ ui('“Take this with you”') }}</label
                ><textarea id="takeaway" v-model="editor.draft.value.takeaway" rows="2" class="field"></textarea>
              </div>
              <div>
                <label class="label" for="question">{{ ui('Closing question') }}</label
                ><input id="question" v-model="editor.draft.value.closingQuestion" class="field" />
              </div>
            </div>
          </section>

          <section class="panel" aria-labelledby="sources">
            <div class="flex items-center justify-between">
              <h2 id="sources" class="font-editorial text-2xl">{{ ui('Sources') }}</h2>
              <button
                class="btn-outline"
                @click="editor.draft.value.sources.push({ label: '', url: 'https://', supports: null })"
              >
                <Icon :icon="Plus" :size="14" />{{ ui('Source') }}
              </button>
            </div>
            <ul class="mt-4 space-y-3">
              <li
                v-for="(source, index) in editor.draft.value.sources"
                :key="index"
                class="grid gap-2 rounded-xl border border-border p-3 sm:grid-cols-[1fr_1fr_auto]"
              >
                <input v-model="source.label" class="field" :placeholder="ui('Name')" :aria-label="ui('Source name')" />
                <input
                  v-model="source.url"
                  class="field font-mono text-xs"
                  placeholder="https://"
                  :aria-label="ui('Source URL')"
                />
                <button
                  class="btn-ghost text-destructive"
                  :aria-label="ui('Remove source')"
                  @click="removeSource(index)"
                >
                  <Icon :icon="Trash2" :size="14" />
                </button>
                <input
                  v-model="source.supports"
                  class="field sm:col-span-3"
                  :placeholder="ui('Supporting which claim (optional)')"
                  :aria-label="ui('Note')"
                />
              </li>
            </ul>
          </section>

          <section class="panel" aria-labelledby="links">
            <h2 id="links" class="font-editorial text-2xl">{{ ui('Cover, products and related stories') }}</h2>
            <div class="mt-4 grid gap-5" :class="imageEditing ? 'lg:grid-cols-1' : 'lg:grid-cols-2'">
              <div>
                <p class="label">{{ ui('Cover') }}</p>
                <div v-if="editor.draft.value.cover" class="rounded-xl border border-border p-3">
                  <img
                    :src="store.links.media(editor.draft.value.cover.mediaSha256)"
                    alt=""
                    class="mx-auto max-h-56 object-contain"
                  />
                  <input
                    v-model="editor.draft.value.cover.alt"
                    class="field mt-2"
                    :placeholder="ui('Image description (alt)')"
                    :aria-label="ui('Cover description')"
                  />
                  <input
                    v-model="editor.draft.value.cover.caption"
                    class="field mt-2"
                    :placeholder="ui('Caption (figcaption)')"
                    :aria-label="ui('Cover caption')"
                  />
                  <button
                    type="button"
                    class="btn-ghost mt-2 text-destructive"
                    :disabled="imageBusy || editor.busy.value || !editor.canEdit.value"
                    @click="
                      imageEditing = false;
                      editor.draft.value.cover = null;
                    "
                  >
                    {{ ui('Remove cover') }}
                  </button>
                </div>
                <div v-else class="rounded-xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                  {{ ui('A typographic cover will be used.') }}
                  <MarketingSelect
                    class="mt-2"
                    :placeholder="ui('Choose from library…')"
                    :label="ui('Choose from library')"
                    :options="editor.media.value.map((m) => ({ value: m.sha256, label: m.fileName }))"
                    :disabled="
                      editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value || imageBusy
                    "
                    @update:model-value="chooseCover"
                  />
                </div>
                <button
                  v-if="coverMedia && !imageEditing"
                  type="button"
                  class="btn-outline mt-2 mr-2"
                  :disabled="imageBusy || editor.busy.value || !editor.canEdit.value"
                  @click="imageEditing = true"
                >
                  {{ ui('Edit image') }}
                </button>
                <button
                  type="button"
                  class="btn-outline mt-2"
                  :disabled="imageBusy || editor.busy.value || !editor.canEdit.value"
                  @click="coverInput?.click()"
                >
                  <Icon :icon="ImagePlus" :size="14" />{{ ui('Upload image') }}
                </button>
                <CoverImageEditor
                  v-if="imageEditing && coverMedia && editor.draft.value.cover"
                  :key="`${id ?? 'new'}-${coverMedia.sha256}`"
                  :media="coverMedia"
                  :alt="editor.draft.value.cover.alt"
                  :can-edit="editor.canEdit.value"
                  :disabled="editor.busy.value"
                  @busy="imageBusy = $event"
                  @pending-edits="pendingImageEdits = $event"
                  @applied="applyEditedCover"
                  @close="imageEditing = false"
                />
                <input
                  ref="coverInput"
                  type="file"
                  accept="image/png,image/jpeg,image/webp"
                  class="hidden"
                  @change="onCover"
                />
              </div>
              <div class="space-y-5">
                <div>
                  <p class="label">{{ ui('Catalogue products (below the story)') }}</p>
                  <div class="flex gap-2">
                    <MarketingSelect
                      v-model="productPick"
                      :placeholder="
                        editor.products.value.length ? ui('Choose a product…') : ui('Catalogue disconnected')
                      "
                      :label="ui('Select product')"
                      :options="editor.products.value.map((p) => ({ value: p.id, label: p.name + ' — ' + p.id }))"
                      :disabled="editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value"
                    />
                    <button class="btn-outline" :disabled="!productPick" @click="addProduct">{{ ui('Add') }}</button>
                  </div>
                  <ul class="mt-2 flex flex-wrap gap-2">
                    <li
                      v-for="(pid, index) in editor.draft.value.productIds"
                      :key="pid"
                      class="chip bg-secondary text-secondary-foreground"
                    >
                      {{ editor.products.value.find((p) => p.id === pid)?.name ?? pid }}
                      <button
                        :aria-label="ui('Remove {product}', { product: pid })"
                        @click="editor.draft.value.productIds.splice(index, 1)"
                      >
                        ×
                      </button>
                    </li>
                  </ul>
                </div>
                <div>
                  <p class="label">{{ ui('Keep reading (blank — automatic)') }}</p>
                  <MarketingSelect
                    v-for="slot in [0, 1]"
                    :key="slot"
                    class="mb-2"
                    :model-value="editor.draft.value.relatedSlugs[slot] ?? ''"
                    :label="ui('Related story {number}', { number: slot + 1 })"
                    :options="[
                      { value: '' as const, label: '—' },
                      ...others
                        .filter((o) => o.slug !== editor.draft.value!.slug)
                        .map((o) => ({ value: o.slug, label: o.title })),
                    ]"
                    :disabled="editor.busy.value || editor.loading.value || refreshing || !editor.canEdit.value"
                    @update:model-value="chooseRelated(slot, $event)"
                  />
                </div>
                <div>
                  <p class="label">SEO</p>
                  <input
                    v-model="editor.draft.value.seoTitle"
                    class="field mb-2"
                    :placeholder="ui('SEO title (optional)')"
                    :aria-label="ui('SEO title')"
                  />
                  <textarea
                    v-model="editor.draft.value.seoDescription"
                    rows="2"
                    class="field"
                    :placeholder="ui('SEO description (optional)')"
                    :aria-label="ui('SEO description')"
                  ></textarea>
                </div>
              </div>
            </div>
          </section>
        </div>

        <aside class="space-y-6 2xl:sticky 2xl:top-24 2xl:self-start">
          <section class="panel" aria-labelledby="publishing">
            <h2 id="publishing" class="font-editorial text-2xl">{{ ui('Publication') }}</h2>
            <p
              v-if="editor.blockers.value.length"
              class="mt-3 rounded-lg bg-warning-soft px-3 py-2 text-sm text-warning-soft-foreground"
            >
              {{ ui('Add before publishing:') }} {{ editor.blockers.value.map((blocker) => ui(blocker)).join(', ') }}.
            </p>
            <p v-if="!editor.detail.value" class="mt-3 text-sm text-muted-foreground">
              {{ ui('Save as a draft first.') }}
            </p>
            <p v-else-if="unsavedChanges" class="mt-3 text-sm text-warning">
              {{ ui('Save changes before publishing actions.') }}
            </p>
            <div class="mt-4 flex flex-wrap gap-2">
              <button
                class="btn-primary"
                :disabled="
                  !editor.detail.value ||
                  unsavedChanges ||
                  editor.busy.value ||
                  imageBusy ||
                  status === 'published' ||
                  editor.blockers.value.length > 0
                "
                @click="editor.publish()"
              >
                <Icon :icon="Send" :size="14" />{{ ui('Publish now') }}
              </button>
              <a v-if="previewUrl" :href="previewUrl" target="_blank" rel="noopener" class="btn-outline"
                ><Icon :icon="Eye" :size="14" />{{ ui('View') }}</a
              >
              <button
                v-if="status === 'published' || status === 'scheduled'"
                class="btn-outline"
                :disabled="unsavedChanges || editor.busy.value"
                @click="editor.unpublish()"
              >
                <Icon :icon="Undo2" :size="14" />{{ ui('Move to draft') }}
              </button>
              <button
                v-if="status !== 'archived' && editor.detail.value"
                class="btn-ghost"
                :disabled="unsavedChanges || editor.busy.value"
                @click="editor.archive()"
              >
                <Icon :icon="Archive" :size="14" />{{ ui('Archive') }}
              </button>
            </div>
            <div class="mt-4 flex flex-wrap items-end gap-2">
              <div>
                <label class="label" for="when">{{ ui('Schedule (local time)') }}</label>
                <input id="when" v-model="scheduleAt" type="datetime-local" class="field" />
              </div>
              <button
                class="btn-outline"
                :disabled="
                  !scheduleAt ||
                  !editor.detail.value ||
                  unsavedChanges ||
                  editor.busy.value ||
                  imageBusy ||
                  editor.blockers.value.length > 0
                "
                @click="editor.schedule(scheduleAt)"
              >
                <Icon :icon="CalendarClock" :size="14" />{{ ui('Scheduling') }}
              </button>
            </div>
            <p v-if="editor.detail.value?.document.publishAt" class="mt-2 text-xs text-muted-foreground">
              {{ ui('Planned:') }} {{ formatWhen(editor.detail.value.document.publishAt) }}
            </p>
          </section>

          <section v-if="editor.detail.value" class="panel" aria-labelledby="editions">
            <h2 id="editions" class="font-editorial text-2xl">{{ ui('Languages') }}</h2>
            <p class="mt-1 text-xs text-muted-foreground">
              {{ ui('This story:') }} {{ languageLabels[editor.draft.value.language]
              }}{{ ui('. Editions link to each other with hreflang; Uzbek uses /uz/ and Russian /ru/.') }}
            </p>
            <ul v-if="editions.length" class="mt-3 space-y-1 text-sm">
              <li v-for="e in editions" :key="e.id">
                <RouterLink :to="`/stories/${e.id}`" class="text-primary underline"
                  >{{ languageLabels[e.language] }}: {{ e.title }}</RouterLink
                >
                <span class="ml-1 text-xs text-muted-foreground">({{ ui(e.status) }})</span>
              </li>
            </ul>
            <div v-if="missingLanguages.length" class="mt-3 flex flex-wrap gap-2">
              <button
                v-for="l in missingLanguages"
                :key="l"
                class="btn-outline"
                :disabled="unsavedChanges || editor.busy.value"
                @click="createEdition(l)"
              >
                {{ languageLabels[l] }} {{ ui('create edition') }}
              </button>
            </div>
          </section>

          <section v-if="previewUrl" class="panel p-0" :aria-label="ui('Live preview')">
            <div
              class="flex items-center justify-between border-b border-border px-4 py-2 text-xs text-muted-foreground"
            >
              <span>{{ ui('Saved revision preview') }}</span>
              <a :href="previewUrl" target="_blank" rel="noopener" class="text-primary">{{ ui('in a new window') }}</a>
            </div>
            <iframe
              :key="previewUrl"
              :src="previewUrl"
              :title="ui('Story preview')"
              class="h-[70vh] w-full rounded-b-2xl bg-white"
            ></iframe>
          </section>

          <section v-if="editor.history.value.length" class="panel" aria-labelledby="history">
            <h2 id="history" class="flex items-center gap-2 font-editorial text-2xl">
              <Icon :icon="History" :size="18" />{{ ui('History') }}
            </h2>
            <ol class="mt-3 max-h-72 space-y-2 overflow-y-auto text-sm">
              <li
                v-for="h in editor.history.value"
                :key="h.revision"
                class="flex items-start justify-between gap-2 rounded-lg border border-border px-3 py-2"
              >
                <div>
                  <p class="font-semibold">v{{ h.revision }} · {{ h.summary }}</p>
                  <p class="text-xs text-muted-foreground">{{ formatWhen(h.savedAt) }}</p>
                </div>
                <button
                  v-if="h.revision !== editor.detail.value?.revision"
                  class="btn-ghost px-2 text-xs"
                  :disabled="unsavedChanges || editor.busy.value"
                  @click="
                    imageEditing = false;
                    editor.restore(h.revision);
                  "
                >
                  <Icon :icon="RotateCcw" :size="13" />{{ ui('Restore') }}
                </button>
              </li>
            </ol>
          </section>
        </aside>
      </div>

      <div class="fixed inset-x-0 bottom-0 z-30 border-t border-border bg-card/95 backdrop-blur lg:left-60">
        <div class="mx-auto flex max-w-[1600px] flex-wrap items-center gap-3 px-5 py-3 sm:px-8 xl:px-10">
          <span :class="['text-sm', unsavedChanges ? 'font-semibold text-warning' : 'text-muted-foreground']">{{
            unsavedChanges ? ui('Unsaved changes') : ui('All changes saved')
          }}</span>
          <span v-if="pendingImageEdits" class="text-xs text-warning">{{
            ui('Apply or cancel image edits before saving.')
          }}</span>
          <input
            v-model="summary"
            class="field max-w-xs flex-1"
            :placeholder="ui('Change note (optional)')"
            :aria-label="ui('Change note')"
          />
          <button
            class="btn-ghost ml-auto"
            :disabled="!editor.dirty.value || !editor.detail.value || imageBusy || editor.busy.value"
            @click="
              imageEditing = false;
              editor.reset();
            "
          >
            {{ ui('Cancel') }}
          </button>
          <button
            class="btn-primary"
            :disabled="
              (!editor.dirty.value && !!editor.detail.value) ||
              editor.busy.value ||
              imageBusy ||
              pendingImageEdits ||
              !editor.draft.value.title.trim() ||
              !editor.draft.value.slug
            "
            @click="save"
          >
            <Icon :icon="Save" :size="15" />{{ ui('Save') }}
          </button>
        </div>
      </div>
    </fieldset>
  </section>
  <p v-else-if="loaded && !editor.loading.value" class="query-label" role="status">
    {{ ui('Content unavailable. Try refreshing.') }}
  </p>
</template>
