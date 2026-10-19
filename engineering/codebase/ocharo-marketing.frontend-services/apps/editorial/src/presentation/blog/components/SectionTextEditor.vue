<script setup lang="ts">
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import { computed, ref, watch } from 'vue';
import { MarkdownEditor } from '@wow-two-beta/fe/presentation/forms';
import type { ArticleSection } from '../../../domain/blog/BlogModels';
import { formatSection, markdownParagraphs, maximumMarkdownCharacters } from '../../../domain/blog/SectionFormatting';
import { joinParagraphs, splitLines, splitParagraphs } from '../../../domain/blog/BlogRules';
const { t: ui } = useUiLocale();

defineOptions({ name: 'SectionTextEditor' });
const props = defineProps<{ section: ArticleSection; id: string; disabled?: boolean }>();
const emit = defineEmits<{ change: [section: ArticleSection] }>();
const view = ref<'edit' | 'split' | 'preview'>('split');
const viewOptions = [
  { value: 'edit', label: 'Edit' },
  { value: 'split', label: 'Split' },
  { value: 'preview', label: 'Preview' },
] as const;
const markdown = computed(() => props.section.paragraphs.join('\n\n'));
const plainText = ref(joinParagraphs(props.section.paragraphs)),
  plainList = ref((props.section.list ?? []).join('\n'));
// Preserve a typed blank line while emitting normalized content immediately for dirty tracking.
watch(
  () => props.section.paragraphs,
  (paragraphs) => {
    if (JSON.stringify(paragraphs) !== JSON.stringify(splitParagraphs(plainText.value)))
      plainText.value = joinParagraphs(paragraphs);
  },
  { deep: true },
);
watch(
  () => props.section.list,
  (list) => {
    if (JSON.stringify(list) !== JSON.stringify(splitLines(plainList.value))) plainList.value = (list ?? []).join('\n');
  },
  { deep: true },
);
function enable(): void {
  if (!props.disabled) emit('change', formatSection(props.section));
}
function updateMarkdown(value: string): void {
  if (!props.disabled) emit('change', { ...props.section, paragraphs: markdownParagraphs(value) });
}
function updateParagraphs(value: string): void {
  if (!props.disabled) {
    plainText.value = value;
    emit('change', { ...props.section, paragraphs: splitParagraphs(value) });
  }
}
function updateList(value: string): void {
  if (!props.disabled) {
    plainList.value = value;
    emit('change', { ...props.section, list: splitLines(value) });
  }
}
</script>

<template>
  <div class="section-text-editor mt-3">
    <template v-if="section.format === 'markdown'">
      <label class="label" :for="id">{{ ui('Formatted text') }}</label>
      <p class="mb-2 text-xs text-muted-foreground">
        {{ ui('Headings, bold, italic, links, lists and quotes. Use the cover editor for images.') }}
      </p>
      <div role="group" :aria-label="ui('View mode')" class="mb-2 flex flex-wrap gap-2">
        <button
          v-for="option in viewOptions"
          :key="option.value"
          type="button"
          class="btn-outline text-xs"
          :aria-pressed="view === option.value"
          @click="view = option.value"
        >
          {{ ui(option.label) }}
        </button>
      </div>
      <MarkdownEditor
        :id="id"
        v-model:view="view"
        class="localized-markdown"
        :model-value="markdown"
        :is-disabled="disabled"
        :show-images="false"
        min-height="16rem"
        @update:model-value="updateMarkdown"
      />
      <p
        :class="[
          'mt-2 text-xs',
          markdown.length > maximumMarkdownCharacters ? 'text-destructive' : 'text-muted-foreground',
        ]"
        :role="markdown.length > maximumMarkdownCharacters ? 'alert' : undefined"
      >
        {{ markdown.length.toLocaleString() }} / {{ maximumMarkdownCharacters.toLocaleString() }} {{ ui('characters') }}
      </p>
    </template>
    <template v-else>
      <div class="mb-2 flex flex-wrap items-center justify-between gap-2">
        <label class="label" :for="id">{{ ui('Paragraphs (separate with blank lines)') }}</label>
        <button type="button" class="btn-outline text-xs" :disabled="disabled" @click="enable">
          {{ ui('Enable formatting') }}
        </button>
      </div>
      <textarea
        :id="id"
        :value="plainText"
        rows="6"
        class="field"
        :disabled="disabled"
        @input="updateParagraphs(($event.target as HTMLTextAreaElement).value)"
      />
      <label class="label mt-3" :for="`${id}-list`">{{ ui('List (one item per line, optional)') }}</label>
      <textarea
        :id="`${id}-list`"
        :value="plainList"
        rows="2"
        class="field"
        :disabled="disabled"
        @input="updateList(($event.target as HTMLTextAreaElement).value)"
      />
    </template>
  </div>
</template>

<style scoped>
/* SDK 0.0.23 exposes controlled view, but its mode enum labels have no locale hook. */
.section-text-editor :deep(.localized-markdown [role='radiogroup']) {
  display: none;
}
.section-text-editor {
  min-width: 0;
}
/* The SDK preview pane exposes .prose; this product does not load Tailwind Typography. */
.section-text-editor :deep(.prose) {
  min-width: 0;
  line-height: 1.7;
  overflow-wrap: anywhere;
}
.section-text-editor :deep(.prose :is(h1, h2, h3, h4, h5, h6)) {
  margin-block: 1.25rem 0.5rem;
  font-family: var(--font-editorial);
  font-weight: 700;
  line-height: 1.3;
}
.section-text-editor :deep(.prose h1) {
  font-size: 1.75rem;
}
.section-text-editor :deep(.prose h2) {
  font-size: 1.5rem;
}
.section-text-editor :deep(.prose h3) {
  font-size: 1.25rem;
}
.section-text-editor :deep(.prose :is(h4, h5, h6)) {
  font-size: 1rem;
}
.section-text-editor :deep(.prose :is(p, ul, ol, blockquote, pre, table, hr)) {
  margin-block: 0.85rem;
}
.section-text-editor :deep(.prose > div > :first-child) {
  margin-top: 0;
}
.section-text-editor :deep(.prose > div > :last-child) {
  margin-bottom: 0;
}
.section-text-editor :deep(.prose :is(ul, ol)) {
  padding-inline-start: 1.5rem;
  list-style-position: outside;
}
.section-text-editor :deep(.prose ul) {
  list-style-type: disc;
}
.section-text-editor :deep(.prose ol) {
  list-style-type: decimal;
}
.section-text-editor :deep(.prose li + li) {
  margin-top: 0.25rem;
}
.section-text-editor :deep(.prose li :is(ul, ol)) {
  margin-block: 0.35rem;
}
.section-text-editor :deep(.prose li::marker) {
  color: var(--color-primary);
}
.section-text-editor :deep(.prose a) {
  color: var(--color-primary);
  text-decoration: underline;
  text-underline-offset: 0.18em;
}
.section-text-editor :deep(.prose a:focus-visible) {
  outline: 2px solid var(--color-ring);
  outline-offset: 2px;
}
.section-text-editor :deep(.prose blockquote) {
  border-inline-start: 3px solid var(--color-primary);
  padding-inline-start: 1rem;
  color: var(--color-foreground);
  font-style: italic;
}
.section-text-editor :deep(.prose code) {
  border-radius: 0.25rem;
  background: var(--color-muted);
  padding: 0.1em 0.3em;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.9em;
}
.section-text-editor :deep(.prose pre) {
  max-width: 100%;
  overflow: auto;
  border: 1px solid var(--color-border);
  border-radius: 0.5rem;
  background: var(--color-muted);
  padding: 0.85rem;
  line-height: 1.5;
}
.section-text-editor :deep(.prose pre code) {
  background: transparent;
  padding: 0;
}
.section-text-editor :deep(.prose table) {
  display: block;
  max-width: 100%;
  overflow: auto;
  border-collapse: collapse;
}
.section-text-editor :deep(.prose :is(th, td)) {
  border: 1px solid var(--color-border);
  padding: 0.5rem 0.75rem;
  text-align: start;
}
.section-text-editor :deep(.prose th) {
  background: var(--color-muted);
  font-weight: 700;
}
.section-text-editor :deep(.prose hr) {
  border: 0;
  border-top: 1px solid var(--color-border);
}
</style>
