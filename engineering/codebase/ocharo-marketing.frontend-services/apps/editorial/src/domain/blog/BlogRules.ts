import type { ArticleDocument, ArticleInput, ArticleStatus } from './BlogModels';

export const statusLabels: Record<ArticleStatus, string> = {
  draft: 'Qoralama',
  scheduled: 'Rejalashtirilgan',
  published: 'Nashr qilingan',
  archived: 'Arxivda',
};

/** Converts a title into a public address: lowercase words joined by hyphens. */
export function slugify(title: string): string {
  return title
    .toLowerCase()
    .replace(/[‘’ʻʼ']/g, '')
    .normalize('NFKD')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 90)
    .replace(/-+$/g, '');
}

/** Paragraph textarea ⇄ list: blank lines separate paragraphs. */
export function splitParagraphs(text: string): string[] {
  return text
    .split(/\n\s*\n/)
    .map((p) => p.replace(/\s*\n\s*/g, ' ').trim())
    .filter((p) => p.length > 0);
}

export function joinParagraphs(paragraphs: string[]): string {
  return paragraphs.join('\n\n');
}

/** List textarea ⇄ list: one item per line. */
export function splitLines(text: string): string[] | null {
  const lines = text
    .split('\n')
    .map((l) => l.trim())
    .filter((l) => l.length > 0);
  return lines.length ? lines : null;
}

/** Words and reading time, matching the server's 200 words/minute estimate. */
export function readingStats(input: Pick<ArticleInput, 'intro' | 'takeaway' | 'closingQuestion' | 'sections'>) {
  const texts = [
    input.intro ?? '',
    input.takeaway ?? '',
    input.closingQuestion ?? '',
    ...input.sections.flatMap((s) => [...s.paragraphs, ...(s.list ?? [])]),
  ];
  const words = texts.reduce((sum, text) => sum + text.split(/\s+/).filter((w) => w.length > 0).length, 0);
  return { words, minutes: Math.max(1, Math.ceil(words / 200)) };
}

/** What still blocks publication (mirrors the API rule); empty when publishable. */
export function publishBlockers(input: Pick<ArticleInput, 'excerpt' | 'intro' | 'sections' | 'cover'>): string[] {
  const missing: string[] = [];
  if (!input.excerpt?.trim()) missing.push('qisqa tavsif');
  if (!input.intro?.trim()) missing.push('kirish');
  if (!input.sections.length || input.sections.some((s) => s.paragraphs.length === 0))
    missing.push('kamida bitta bo‘lim va paragraf');
  if (input.cover && (!input.cover.alt.trim() || !input.cover.caption.trim())) missing.push('muqova tavsifi va izohi');
  return missing;
}

export function toInput(document: ArticleDocument, expectedRevision?: number): ArticleInput {
  return {
    expectedRevision,
    publication: document.publication ?? 'blog',
    kind: document.kind ?? 'story',
    places: [...(document.places ?? [])],
    techniques: [...(document.techniques ?? [])],
    motifs: [...(document.motifs ?? [])],
    occasions: [...(document.occasions ?? [])],
    textileSlugs: [...(document.textileSlugs ?? [])],
    textile: document.textile ? { ...document.textile } : null,
    imageProvenance: document.imageProvenance ? { ...document.imageProvenance } : null,
    slug: document.slug,
    topic: document.topic,
    language: document.language,
    title: document.title,
    excerpt: document.excerpt,
    intro: document.intro,
    format: document.format,
    readerPromise: document.readerPromise,
    sections: structuredClone(document.sections),
    takeaway: document.takeaway,
    closingQuestion: document.closingQuestion,
    sources: structuredClone(document.sources),
    relatedSlugs: [...document.relatedSlugs],
    cardPrompt: document.cardPrompt,
    cover: document.cover ? { ...document.cover } : null,
    productIds: [...document.productIds],
    seoTitle: document.seoTitle,
    seoDescription: document.seoDescription,
    translationKey: document.translationKey,
  };
}

export const languageLabels: Record<string, string> = { en: 'English', uz: 'O‘zbekcha', ru: 'Русский' };

export function emptyArticle(topic: string): ArticleInput {
  return {
    publication: 'blog',
    kind: 'story',
    places: [],
    techniques: [],
    motifs: [],
    occasions: [],
    textileSlugs: [],
    textile: null,
    imageProvenance: null,
    slug: '',
    topic,
    language: 'en',
    title: '',
    excerpt: null,
    intro: null,
    format: null,
    readerPromise: null,
    sections: [{ heading: '', paragraphs: [], list: null, sourceIndexes: null }],
    takeaway: null,
    closingQuestion: null,
    sources: [],
    relatedSlugs: [],
    cardPrompt: null,
    cover: null,
    productIds: [],
    seoTitle: null,
    seoDescription: null,
  };
}

/** Retains each publication's authored public route when linking from the desk. */
export function publicationPath(document: {
  slug: string;
  language?: string;
  publication?: string;
  kind?: string;
}): string {
  if (document.publication === 'atlas')
    return `/atlas/${document.kind === 'textile' || document.kind === 'archive' ? 'textiles' : 'stories'}/${document.slug}/`;
  if (document.publication === 'wear') return `/wear/guides/${document.slug}/`;
  return `${document.language && document.language !== 'en' ? '/' + document.language : ''}/journal/${document.slug}/`;
}
