import type { ArticleSection } from './BlogModels';

export const maximumMarkdownCharacters = 32_000;

/** Escapes existing literal prose before the author explicitly enables formatting. */
function escapeMarkdown(value: string): string {
  return value.replace(/([\\`*_{}[\]()<>#+.!|~=-])/g, '\\$1');
}

/** Converts a plain section without interpreting its old punctuation as authored Markdown. */
export function formatSection(section: ArticleSection): ArticleSection {
  if (section.format === 'markdown') return structuredClone(section);
  const paragraphs = section.paragraphs.map(escapeMarkdown);
  if (section.list?.length) paragraphs.push(section.list.map((value) => `- ${escapeMarkdown(value)}`).join('\n'));
  return {
    ...section,
    format: 'markdown',
    paragraphs: paragraphs.length ? [paragraphs.join('\n\n')] : [],
    list: null,
    sourceIndexes: section.sourceIndexes ? [...section.sourceIndexes] : null,
  };
}

/** Preserves meaningful Markdown newlines, indentation and trailing hard-break spaces. */
export function markdownParagraphs(value: string): string[] {
  return value.trim().length ? [value] : [];
}
