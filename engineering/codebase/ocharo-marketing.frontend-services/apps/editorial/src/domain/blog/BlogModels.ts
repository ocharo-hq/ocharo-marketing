/** Blog wire and view models, mirroring the OcharoBlog API documents. */
export type ArticleStatus = 'draft' | 'scheduled' | 'published' | 'archived';

export interface ArticleSection {
  /** Only explicitly opted-in sections interpret Markdown. */
  format?: 'plain' | 'markdown' | null;
  heading: string;
  paragraphs: string[];
  list: string[] | null;
  sourceIndexes: number[] | null;
}

export interface ArticleSource {
  label: string;
  url: string;
  supports: string | null;
}

export interface ArticleCover {
  mediaSha256: string;
  alt: string;
  caption: string;
  cardClass: string | null;
}

export interface ArticleDocument {
  publication?: 'blog' | 'atlas' | 'wear';
  kind?: 'story' | 'textile' | 'archive' | 'guide';
  places?: string[];
  techniques?: string[];
  motifs?: string[];
  occasions?: string[];
  textileSlugs?: string[];
  textile?: {
    objectId: string;
    collection: string;
    place: string;
    period: string;
    materials: string;
    technique: string;
    dimensions: string | null;
    maker: string | null;
    attributionNote: string | null;
  } | null;
  imageProvenance?: { kind: string; credit: string; rights: string; sourceUrl: string } | null;

  slug: string;
  topic: string;
  status: ArticleStatus;
  language: string;
  title: string;
  excerpt: string | null;
  intro: string | null;
  format: string | null;
  readerPromise: string | null;
  sections: ArticleSection[];
  takeaway: string | null;
  closingQuestion: string | null;
  sources: ArticleSource[];
  relatedSlugs: string[];
  cardPrompt: string | null;
  cover: ArticleCover | null;
  productIds: string[];
  publishAt: string | null;
  publishedAt: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  translationKey: string | null;
}

export interface ArticleDetail {
  id: string;
  document: ArticleDocument;
  revision: number;
  createdAt: string;
  updatedAt: string;
  readMinutes: number;
  words: number;
}

export interface ArticleSummary {
  publication?: 'blog' | 'atlas' | 'wear';
  kind?: 'story' | 'textile' | 'archive' | 'guide';
  id: string;
  slug: string;
  topic: string;
  status: ArticleStatus;
  title: string;
  excerpt: string | null;
  readMinutes: number;
  words: number;
  hasCover: boolean;
  productCount: number;
  publishAt: string | null;
  publishedAt: string | null;
  revision: number;
  updatedAt: string;
  language: string;
  translationKey: string | null;
}

export interface RevisionSummary {
  revision: number;
  savedAt: string;
  summary: string;
}

export interface TopicText {
  name: string;
  short: string;
  description: string;
}

export interface TopicDocument {
  key: string;
  name: string;
  short: string;
  description: string;
  sortOrder: number;
  translations?: Record<string, TopicText> | null;
}

export interface SiteLink {
  label: string;
  url: string;
}

export interface SiteSettings {
  title: string;
  eyebrow: string;
  mastheadDescription: string;
  homeTitle: string;
  homeDescription: string;
  footerTagline: string;
  editionNote: string;
  noIndex: boolean;
  baseUrl: string | null;
  links: SiteLink[];
  featuredSlug: string | null;
  logoSha256: string | null;
  heroSha256: string | null;
  heroAlt: string;
  bandSha256: string | null;
  bandAlt: string;
}

/** Whether the Brand service currently supplies the public site's logo and footer links. */
export interface BrandStatus {
  connected: boolean;
  release: number;
  logoSha256: string | null;
  links: { label: string; url: string }[];
}

export interface SettingsEnvelope {
  settings: SiteSettings;
  revision: number;
}

export interface MediaView {
  sha256: string;
  fileName: string;
  contentType: string;
  byteLength: number;
  width: number | null;
  height: number | null;
  url: string;
  previewUrl: string | null;
}

export interface CatalogueProduct {
  id: string;
  name: string;
  garmentName: string;
  fabric: string | null;
  retailPrice: number | null;
  imageUrl: string | null;
  previewUrl: string | null;
}

/** Create/update payload. */
export interface ArticleInput {
  publication?: 'blog' | 'atlas' | 'wear';
  kind?: 'story' | 'textile' | 'archive' | 'guide';
  places?: string[];
  techniques?: string[];
  motifs?: string[];
  occasions?: string[];
  textileSlugs?: string[];
  textile?: {
    objectId: string;
    collection: string;
    place: string;
    period: string;
    materials: string;
    technique: string;
    dimensions: string | null;
    maker: string | null;
    attributionNote: string | null;
  } | null;
  imageProvenance?: { kind: string; credit: string; rights: string; sourceUrl: string } | null;

  expectedRevision?: number;
  slug: string;
  topic: string;
  language: string;
  title: string;
  excerpt: string | null;
  intro: string | null;
  format: string | null;
  readerPromise: string | null;
  sections: ArticleSection[];
  takeaway: string | null;
  closingQuestion: string | null;
  sources: ArticleSource[];
  relatedSlugs: string[];
  cardPrompt: string | null;
  cover: ArticleCover | null;
  productIds: string[];
  seoTitle: string | null;
  seoDescription: string | null;
  translationKey?: string | null;
  changeSummary?: string;
}
