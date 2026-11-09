import type {
  ArticleCover,
  ArticleDetail,
  ArticleDocument,
  ArticleSection,
  ArticleSource,
  ArticleSummary,
  BrandStatus,
  CatalogueProduct,
  MediaView,
  SettingsEnvelope,
} from '../../domain/blog/BlogModels';
import type { MediaEditingCapabilities } from '../../domain/blog/MediaEditingModels';

/**
 * The Blog API writes JSON through the SDK preset, which omits null properties (`WhenWritingNull`). These readers
 * restore each omitted nullable field as `null`, the value the editor models hold, and keep every other field as
 * the API sent it. An older API's explicit `null` reads the same.
 */
type Wire = Record<string, unknown>;

/** @internal Copies an object with each listed field `null` where the API left it out; anything else stays as sent. */
function withNulls<T>(value: unknown, fields: readonly (keyof T & string)[]): T {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) return value as T;
  const read: Wire = { ...(value as Wire) };
  for (const field of fields) read[field] ??= null;
  return read as T;
}

/** @internal Reads an array through `item`, leaving anything else for the caller's validation. */
function each<T>(value: unknown, item: (entry: unknown) => T): T[] {
  return Array.isArray(value) ? value.map(item) : (value as T[]);
}

const section = (value: unknown) => withNulls<ArticleSection>(value, ['list', 'sourceIndexes']);
const source = (value: unknown) => withNulls<ArticleSource>(value, ['supports']);
const cover = (value: unknown) => withNulls<ArticleCover>(value, ['cardClass']);

/** Reads a story document. */
export function readDocument(value: unknown): ArticleDocument {
  const document = withNulls<ArticleDocument>(value, [
    'excerpt',
    'intro',
    'format',
    'readerPromise',
    'takeaway',
    'closingQuestion',
    'cardPrompt',
    'cover',
    'publishAt',
    'publishedAt',
    'seoTitle',
    'seoDescription',
    'translationKey',
  ]);
  return {
    ...document,
    textile: document.textile
      ? withNulls<NonNullable<ArticleDocument['textile']>>(document.textile, ['dimensions', 'maker', 'attributionNote'])
      : null,
    imageProvenance: document.imageProvenance ?? null,
    sections: each(document.sections, section),
    sources: each(document.sources, source),
    cover: document.cover ? cover(document.cover) : null,
  };
}

/** Reads a story with its revision; a value without a document object is left for the caller to refuse. */
export function readDetail(value: unknown): ArticleDetail {
  const detail = value as ArticleDetail;
  return { ...detail, document: readDocument(detail.document) };
}

/** Reads the story list. */
export function readSummaries(value: unknown): ArticleSummary[] {
  return each(value, (item) =>
    withNulls<ArticleSummary>(item, ['excerpt', 'publishAt', 'publishedAt', 'translationKey']),
  );
}

/** Reads the site settings and their revision. */
export function readSettings(value: unknown): SettingsEnvelope {
  const envelope = value as SettingsEnvelope;
  return {
    ...envelope,
    settings: withNulls(envelope.settings, ['baseUrl', 'featuredSlug', 'logoSha256', 'heroSha256', 'bandSha256']),
  };
}

/** Reads whether Brand supplies the public site's logo and links. */
export function readBrand(value: unknown): BrandStatus {
  return withNulls<BrandStatus>(value, ['logoSha256']);
}

/** Reads one stored image. */
export function readMedia(value: unknown): MediaView {
  return withNulls<MediaView>(value, ['width', 'height', 'previewUrl']);
}

/** Reads the catalogue pieces a story can feature. */
export function readCatalogue(value: unknown): CatalogueProduct[] {
  return each(value, (item) => withNulls<CatalogueProduct>(item, ['fabric', 'retailPrice', 'imageUrl', 'previewUrl']));
}

/** Reads the host's image-editing limits. */
export function readEditingCapabilities(value: unknown): MediaEditingCapabilities {
  const capabilities = value as MediaEditingCapabilities;
  return {
    ...capabilities,
    backgroundRemoval: withNulls(capabilities.backgroundRemoval, ['model']),
  };
}
