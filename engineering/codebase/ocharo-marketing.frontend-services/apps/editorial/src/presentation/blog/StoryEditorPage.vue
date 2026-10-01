<script setup lang="ts">
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
import { loadStoryOptions, useArticleEditor, useBlogStore } from '../../application/blog';
import { languageLabels, publicationPath } from '../../domain/blog/BlogRules';
import StatusBadge from './components/StatusBadge.vue';
import SectionTextEditor from './components/SectionTextEditor.vue';
import CoverImageEditor from './components/CoverImageEditor.vue';
import type { MediaView } from '../../domain/blog/BlogModels';
import { formatWhen } from '../../domain/blog/UzDates';

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
onBeforeRouteLeave(() => !unsavedChanges.value || window.confirm('Saqlanmagan o‘zgarishlar bor. Chiqib ketilsinmi?'));

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
function addProduct() {
  if (productPick.value && editor.draft.value && !editor.draft.value.productIds.includes(productPick.value))
    editor.draft.value.productIds.push(productPick.value);
  productPick.value = '';
}
</script>

<template>
  <section v-if="editor.draft.value" class="pb-28">
    <RouterLink to="/" class="btn-ghost -ml-3 mb-3"><Icon :icon="ArrowLeft" :size="15" />Hikoyalar</RouterLink>
    <div class="flex flex-wrap items-start justify-between gap-4">
      <div class="min-w-0">
        <h1 class="font-editorial text-4xl tracking-tight">{{ editor.draft.value.title || 'Yangi hikoya' }}</h1>
        <p class="mt-2 flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
          <StatusBadge :status="status" />
          <span v-if="editor.detail.value">versiya {{ editor.detail.value.revision }}</span>
          <span>{{ editor.stats.value.words }} so‘z · {{ editor.stats.value.minutes }} daq o‘qish</span>
          <a
            v-if="status === 'published'"
            :href="publicationPath(editor.draft.value)"
            target="_blank"
            rel="noopener"
            class="text-primary underline"
            >saytda ochish</a
          >
        </p>
      </div>
    </div>

    <div class="mt-6 grid grid-cols-1 gap-6 2xl:grid-cols-[minmax(0,1fr)_minmax(460px,0.9fr)]">
      <div class="space-y-6">
        <section class="panel" aria-labelledby="publication-meta">
          <h2 id="publication-meta" class="font-editorial text-2xl">Publication &amp; provenance</h2>
          <div class="mt-4 grid gap-4 sm:grid-cols-2">
            <label class="label"
              >Publication<select
                v-model="editor.draft.value.publication"
                class="field"
                @change="editor.draft.value.kind = editor.draft.value.publication === 'wear' ? 'guide' : 'story'"
              >
                <option value="blog">Ocharo Blog</option>
                <option value="atlas">Ocharo Atlas</option>
                <option value="wear">Ocharo Wear</option>
              </select></label
            >
            <label class="label"
              >Content type<select v-model="editor.draft.value.kind" class="field">
                <option value="story">Story</option>
                <option value="textile">Textile object</option>
                <option value="archive">Archive encounter</option>
                <option value="guide">Guide</option>
              </select></label
            >
            <label
              v-for="axis in ['places', 'techniques', 'motifs', 'occasions', 'textileSlugs'] as const"
              :key="axis"
              class="label"
              >{{ axis }} (comma separated)<input
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
            Add object record
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
              >{{ field }}<input v-model="editor.draft.value.textile[field]" class="field"
            /></label>
          </div>
          <button
            v-if="!editor.draft.value.imageProvenance"
            class="btn-ghost mt-3"
            @click="editor.draft.value.imageProvenance = { kind: 'photograph', credit: '', rights: '', sourceUrl: '' }"
          >
            Add image provenance
          </button>
          <div v-if="editor.draft.value.imageProvenance" class="mt-4 grid gap-3 sm:grid-cols-2">
            <label v-for="field in ['kind', 'credit', 'rights', 'sourceUrl'] as const" :key="field" class="label"
              >{{ field }}<input v-model="editor.draft.value.imageProvenance[field]" class="field"
            /></label>
          </div>
          <p class="mt-3 text-xs text-muted-foreground">
            Record uncertainty in the attribution note. Every save retains this metadata in its revision.
          </p>
        </section>
        <section class="panel" aria-labelledby="basics">
          <h2 id="basics" class="font-editorial text-2xl">Asosiy</h2>
          <div class="mt-4 grid gap-4">
            <div>
              <label class="label" for="title">Sarlavha</label>
              <input
                id="title"
                v-model="editor.draft.value.title"
                class="field text-base"
                @input="editor.titleChanged()"
              />
            </div>
            <div class="grid gap-4 sm:grid-cols-3">
              <div class="sm:col-span-2">
                <label class="label" for="slug">Manzil (/journal/…/)</label>
                <input
                  id="slug"
                  v-model="editor.draft.value.slug"
                  class="field font-mono text-xs"
                  @input="editor.slugEdited()"
                />
              </div>
              <div>
                <label class="label" for="topic">Bo‘lim</label>
                <select id="topic" v-model="editor.draft.value.topic" class="field">
                  <option v-for="t in store.topics.value" :key="t.key" :value="t.key">{{ t.short }}</option>
                </select>
              </div>
            </div>
            <div class="grid gap-4 sm:grid-cols-3">
              <div>
                <label class="label" for="format">Format</label
                ><input id="format" v-model="editor.draft.value.format" class="field" placeholder="Wardrobe puzzle" />
              </div>
              <div>
                <label class="label" for="prompt">Kartochka iborasi</label
                ><input
                  id="prompt"
                  v-model="editor.draft.value.cardPrompt"
                  class="field"
                  placeholder="Borrow the idea."
                />
              </div>
              <div>
                <label class="label" for="lang">Til</label>
                <select id="lang" v-model="editor.draft.value.language" class="field">
                  <option value="en">English</option>
                  <option value="uz">O‘zbekcha</option>
                  <option value="ru">Русский</option>
                </select>
              </div>
            </div>
            <div>
              <label class="label" for="excerpt">Qisqa tavsif (kartochka va feed)</label
              ><textarea id="excerpt" v-model="editor.draft.value.excerpt" rows="2" class="field"></textarea>
            </div>
            <div>
              <label class="label" for="intro">Kirish</label
              ><textarea id="intro" v-model="editor.draft.value.intro" rows="4" class="field"></textarea>
            </div>
            <div>
              <label class="label" for="promise">O‘quvchiga va’da (ichki, chop etilmaydi)</label
              ><input id="promise" v-model="editor.draft.value.readerPromise" class="field" />
            </div>
          </div>
        </section>

        <section class="panel" aria-labelledby="sections">
          <div class="flex items-center justify-between">
            <h2 id="sections" class="font-editorial text-2xl">Bo‘limlar</h2>
            <button
              class="btn-outline"
              @click="
                editor.draft.value.sections.push({ heading: '', paragraphs: [], list: null, sourceIndexes: null })
              "
            >
              <Icon :icon="Plus" :size="14" />Bo‘lim
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
                  placeholder="Bo‘lim sarlavhasi"
                  aria-label="Bo‘lim sarlavhasi"
                />
                <button class="btn-ghost px-1.5" aria-label="Yuqoriga" @click="moveSection(index, -1)">
                  <Icon :icon="ArrowUp" :size="13" />
                </button>
                <button class="btn-ghost px-1.5" aria-label="Pastga" @click="moveSection(index, 1)">
                  <Icon :icon="ArrowDown" :size="13" />
                </button>
                <button
                  class="btn-ghost px-1.5 text-destructive"
                  aria-label="Bo‘limni o‘chirish"
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
                <span class="text-muted-foreground">Manbalar:</span>
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
          <h2 id="closing" class="font-editorial text-2xl">Yakun</h2>
          <div class="mt-4 grid gap-4">
            <div>
              <label class="label" for="takeaway">“Take this with you”</label
              ><textarea id="takeaway" v-model="editor.draft.value.takeaway" rows="2" class="field"></textarea>
            </div>
            <div>
              <label class="label" for="question">Yakuniy savol</label
              ><input id="question" v-model="editor.draft.value.closingQuestion" class="field" />
            </div>
          </div>
        </section>

        <section class="panel" aria-labelledby="sources">
          <div class="flex items-center justify-between">
            <h2 id="sources" class="font-editorial text-2xl">Manbalar</h2>
            <button
              class="btn-outline"
              @click="editor.draft.value.sources.push({ label: '', url: 'https://', supports: null })"
            >
              <Icon :icon="Plus" :size="14" />Manba
            </button>
          </div>
          <ul class="mt-4 space-y-3">
            <li
              v-for="(source, index) in editor.draft.value.sources"
              :key="index"
              class="grid gap-2 rounded-xl border border-border p-3 sm:grid-cols-[1fr_1fr_auto]"
            >
              <input v-model="source.label" class="field" placeholder="Nomi" aria-label="Manba nomi" />
              <input
                v-model="source.url"
                class="field font-mono text-xs"
                placeholder="https://"
                aria-label="Manba manzili"
              />
              <button class="btn-ghost text-destructive" aria-label="Manbani o‘chirish" @click="removeSource(index)">
                <Icon :icon="Trash2" :size="14" />
              </button>
              <input
                v-model="source.supports"
                class="field sm:col-span-3"
                placeholder="Qaysi fikrni tasdiqlaydi (ixtiyoriy)"
                aria-label="Izoh"
              />
            </li>
          </ul>
        </section>

        <section class="panel" aria-labelledby="links">
          <h2 id="links" class="font-editorial text-2xl">Muqova, mahsulotlar va bog‘liq hikoyalar</h2>
          <div class="mt-4 grid gap-5" :class="imageEditing ? 'lg:grid-cols-1' : 'lg:grid-cols-2'">
            <div>
              <p class="label">Muqova</p>
              <div v-if="editor.draft.value.cover" class="rounded-xl border border-border p-3">
                <img
                  :src="store.links.media(editor.draft.value.cover.mediaSha256)"
                  alt=""
                  class="mx-auto max-h-56 object-contain"
                />
                <input
                  v-model="editor.draft.value.cover.alt"
                  class="field mt-2"
                  placeholder="Rasm tavsifi (alt)"
                  aria-label="Muqova tavsifi"
                />
                <input
                  v-model="editor.draft.value.cover.caption"
                  class="field mt-2"
                  placeholder="Izoh (figcaption)"
                  aria-label="Muqova izohi"
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
                  Muqovani olib tashlash
                </button>
              </div>
              <div v-else class="rounded-xl border border-dashed border-border p-4 text-sm text-muted-foreground">
                Tipografik muqova ishlatiladi.
                <select
                  class="field mt-2"
                  aria-label="Kutubxonadan tanlash"
                  @change="
                    editor.draft.value.cover = {
                      mediaSha256: ($event.target as HTMLSelectElement).value,
                      alt: '',
                      caption: '',
                      cardClass: null,
                    }
                  "
                >
                  <option value="">Kutubxonadan tanlash…</option>
                  <option v-for="m in editor.media.value" :key="m.sha256" :value="m.sha256">{{ m.fileName }}</option>
                </select>
              </div>
              <button
                v-if="coverMedia && !imageEditing"
                type="button"
                class="btn-outline mt-2 mr-2"
                :disabled="imageBusy || editor.busy.value || !editor.canEdit.value"
                @click="imageEditing = true"
              >
                Edit image
              </button>
              <button
                type="button"
                class="btn-outline mt-2"
                :disabled="imageBusy || editor.busy.value || !editor.canEdit.value"
                @click="coverInput?.click()"
              >
                <Icon :icon="ImagePlus" :size="14" />Rasm yuklash
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
                <p class="label">Katalogdagi mahsulotlar (hikoya ostida)</p>
                <div class="flex gap-2">
                  <select v-model="productPick" class="field" aria-label="Mahsulot tanlash">
                    <option value="">
                      {{ editor.products.value.length ? 'Mahsulot tanlang…' : 'Katalog ulanmagan' }}
                    </option>
                    <option v-for="p in editor.products.value" :key="p.id" :value="p.id">
                      {{ p.name }} — {{ p.id }}
                    </option>
                  </select>
                  <button class="btn-outline" :disabled="!productPick" @click="addProduct">Qo‘shish</button>
                </div>
                <ul class="mt-2 flex flex-wrap gap-2">
                  <li
                    v-for="(pid, index) in editor.draft.value.productIds"
                    :key="pid"
                    class="chip bg-secondary text-secondary-foreground"
                  >
                    {{ editor.products.value.find((p) => p.id === pid)?.name ?? pid }}
                    <button
                      :aria-label="`${pid} ni olib tashlash`"
                      @click="editor.draft.value.productIds.splice(index, 1)"
                    >
                      ×
                    </button>
                  </li>
                </ul>
              </div>
              <div>
                <p class="label">“Keep reading” (bo‘sh — avtomatik)</p>
                <select
                  v-for="slot in [0, 1]"
                  :key="slot"
                  class="field mb-2"
                  :value="editor.draft.value.relatedSlugs[slot] ?? ''"
                  :aria-label="`Bog‘liq hikoya ${slot + 1}`"
                  @change="
                    (() => {
                      const value = ($event.target as HTMLSelectElement).value;
                      const next = [...editor.draft.value!.relatedSlugs];
                      next[slot] = value;
                      editor.draft.value!.relatedSlugs = next.filter((s) => s);
                    })()
                  "
                >
                  <option value="">—</option>
                  <option
                    v-for="o in others.filter((o) => o.slug !== editor.draft.value!.slug)"
                    :key="o.slug"
                    :value="o.slug"
                  >
                    {{ o.title }}
                  </option>
                </select>
              </div>
              <div>
                <p class="label">SEO</p>
                <input
                  v-model="editor.draft.value.seoTitle"
                  class="field mb-2"
                  placeholder="SEO sarlavha (ixtiyoriy)"
                  aria-label="SEO sarlavha"
                />
                <textarea
                  v-model="editor.draft.value.seoDescription"
                  rows="2"
                  class="field"
                  placeholder="SEO tavsif (ixtiyoriy)"
                  aria-label="SEO tavsif"
                ></textarea>
              </div>
            </div>
          </div>
        </section>
      </div>

      <aside class="space-y-6 2xl:sticky 2xl:top-24 2xl:self-start">
        <section class="panel" aria-labelledby="publishing">
          <h2 id="publishing" class="font-editorial text-2xl">Nashr</h2>
          <p
            v-if="editor.blockers.value.length"
            class="mt-3 rounded-lg bg-warning-soft px-3 py-2 text-sm text-warning-soft-foreground"
          >
            Nashrdan oldin qo‘shing: {{ editor.blockers.value.join(', ') }}.
          </p>
          <p v-if="!editor.detail.value" class="mt-3 text-sm text-muted-foreground">Avval qoralama sifatida saqlang.</p>
          <p v-else-if="unsavedChanges" class="mt-3 text-sm text-warning">
            Nashr amallari oldidan o‘zgarishlarni saqlang.
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
              <Icon :icon="Send" :size="14" />Hozir nashr qilish
            </button>
            <a v-if="previewUrl" :href="previewUrl" target="_blank" rel="noopener" class="btn-outline"
              ><Icon :icon="Eye" :size="14" />Ko‘rish</a
            >
            <button
              v-if="status === 'published' || status === 'scheduled'"
              class="btn-outline"
              :disabled="unsavedChanges || editor.busy.value"
              @click="editor.unpublish()"
            >
              <Icon :icon="Undo2" :size="14" />Qoralamaga
            </button>
            <button
              v-if="status !== 'archived' && editor.detail.value"
              class="btn-ghost"
              :disabled="unsavedChanges || editor.busy.value"
              @click="editor.archive()"
            >
              <Icon :icon="Archive" :size="14" />Arxiv
            </button>
          </div>
          <div class="mt-4 flex flex-wrap items-end gap-2">
            <div>
              <label class="label" for="when">Rejalashtirish (mahalliy vaqt)</label>
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
              <Icon :icon="CalendarClock" :size="14" />Rejalashtirish
            </button>
          </div>
          <p v-if="editor.detail.value?.document.publishAt" class="mt-2 text-xs text-muted-foreground">
            Rejada: {{ formatWhen(editor.detail.value.document.publishAt) }}
          </p>
        </section>

        <section v-if="editor.detail.value" class="panel" aria-labelledby="editions">
          <h2 id="editions" class="font-editorial text-2xl">Tillar</h2>
          <p class="mt-1 text-xs text-muted-foreground">
            Bu hikoya: {{ languageLabels[editor.draft.value.language] }}. Tahrirlar bir-biriga hreflang bilan
            bog‘lanadi; o‘zbekcha /uz/, ruscha /ru/ ostida.
          </p>
          <ul v-if="editions.length" class="mt-3 space-y-1 text-sm">
            <li v-for="e in editions" :key="e.id">
              <RouterLink :to="`/stories/${e.id}`" class="text-primary underline"
                >{{ languageLabels[e.language] }}: {{ e.title }}</RouterLink
              >
              <span class="ml-1 text-xs text-muted-foreground">({{ e.status }})</span>
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
              {{ languageLabels[l] }} tahririni yaratish
            </button>
          </div>
        </section>

        <section v-if="previewUrl" class="panel p-0" aria-label="Jonli ko‘rinish">
          <div class="flex items-center justify-between border-b border-border px-4 py-2 text-xs text-muted-foreground">
            <span>Saqlangan versiya ko‘rinishi</span>
            <a :href="previewUrl" target="_blank" rel="noopener" class="text-primary">yangi oynada</a>
          </div>
          <iframe
            :key="previewUrl"
            :src="previewUrl"
            title="Hikoya ko‘rinishi"
            class="h-[70vh] w-full rounded-b-2xl bg-white"
          ></iframe>
        </section>

        <section v-if="editor.history.value.length" class="panel" aria-labelledby="history">
          <h2 id="history" class="flex items-center gap-2 font-editorial text-2xl">
            <Icon :icon="History" :size="18" />Tarix
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
                <Icon :icon="RotateCcw" :size="13" />Tiklash
              </button>
            </li>
          </ol>
        </section>
      </aside>
    </div>

    <div class="fixed inset-x-0 bottom-0 z-30 border-t border-border bg-card/95 backdrop-blur lg:left-60">
      <div class="mx-auto flex max-w-[1600px] flex-wrap items-center gap-3 px-5 py-3 sm:px-8 xl:px-10">
        <span :class="['text-sm', unsavedChanges ? 'font-semibold text-warning' : 'text-muted-foreground']">{{
          unsavedChanges ? 'Saqlanmagan o‘zgarishlar' : 'Hammasi saqlangan'
        }}</span>
        <span v-if="pendingImageEdits" class="text-xs text-warning">Apply or cancel image edits before saving.</span>
        <input
          v-model="summary"
          class="field max-w-xs flex-1"
          placeholder="O‘zgarish izohi (ixtiyoriy)"
          aria-label="O‘zgarish izohi"
        />
        <button
          class="btn-ghost ml-auto"
          :disabled="!editor.dirty.value || !editor.detail.value || imageBusy || editor.busy.value"
          @click="
            imageEditing = false;
            editor.reset();
          "
        >
          Bekor qilish
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
          <Icon :icon="Save" :size="15" />Saqlash
        </button>
      </div>
    </div>
  </section>
  <p v-else-if="loaded" class="panel p-10 text-center text-sm text-muted-foreground">Hikoya topilmadi.</p>
  <p v-else class="text-sm text-muted-foreground" role="status">Yuklanmoqda…</p>
</template>
