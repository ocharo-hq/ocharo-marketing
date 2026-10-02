import { requestFailureMessage } from '@ocharomarketing/connect/RequestFeedback';
import type { ImageEditRecipe } from '@wow-two-beta/ui-vue/domain/imageEditing';
import type { MediaEditingCapabilities, MediaEditPreview } from '../../domain/blog/MediaEditingModels';
import { integrationFetch } from '@ocharomarketing/connect';
import {
  ApiFailureFactory,
  createApiClient,
  rawEnvelope,
  type ApiFailure,
  type ResponseEnvelope,
} from '@wow-two-beta/ui-vue/foundation/http';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import type {
  ArticleDetail,
  ArticleInput,
  ArticleSummary,
  CatalogueProduct,
  MediaView,
  RevisionSummary,
  SettingsEnvelope,
  BrandStatus,
  SiteSettings,
  TopicDocument,
} from '../../domain/blog/BlogModels';

const envelope: ResponseEnvelope = {
  unwrap: rawEnvelope.unwrap,
  toFailure: (parsed, response) =>
    ApiFailureFactory.create(
      'http',
      response,
      typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)
        ? (parsed as Record<string, unknown>)
        : null,
    ),
};
function shape<T>(check: (value: unknown) => boolean) {
  return (value: unknown) =>
    check(value) ? ResultExtensions.ok(value as T) : ResultExtensions.fail(ApiFailureFactory.create('protocol'));
}
const isObject = (value: unknown) => typeof value === 'object' && value !== null && !Array.isArray(value);
const isArray = (value: unknown) => Array.isArray(value);
const isDetail = (value: unknown) => isObject(value) && isObject((value as { document?: unknown }).document);

const positiveInteger = (value: unknown) => Number.isSafeInteger(value) && Number(value) > 0;
const isMedia = (value: unknown): value is MediaView => {
  if (!isObject(value)) return false;
  const media = value as Record<string, unknown>;
  return (
    typeof media.sha256 === 'string' &&
    /^[a-f0-9]{64}$/.test(media.sha256) &&
    typeof media.fileName === 'string' &&
    ['image/png', 'image/jpeg', 'image/webp'].includes(String(media.contentType)) &&
    positiveInteger(media.byteLength) &&
    (media.width === null || positiveInteger(media.width)) &&
    (media.height === null || positiveInteger(media.height)) &&
    media.url === `/media/${media.sha256}` &&
    (media.previewUrl === null || media.previewUrl === `/media/${media.sha256}/preview`)
  );
};
const isEditingCapabilities = (value: unknown): value is MediaEditingCapabilities => {
  if (!isObject(value)) return false;
  const c = value as Record<string, unknown>;
  if (!isObject(c.backgroundRemoval)) return false;
  const background = c.backgroundRemoval as Record<string, unknown>;
  return (
    ['crop', 'rotate', 'flip', 'resize'].every((key) => typeof c[key] === 'boolean') &&
    Array.isArray(c.formats) &&
    c.formats.length > 0 &&
    c.formats.every((format) => ['png', 'jpeg', 'webp'].includes(String(format))) &&
    ['maximumBytes', 'maximumPixels', 'maximumDimension'].every((key) => positiveInteger(c[key])) &&
    typeof background.available === 'boolean' &&
    (background.model === null || typeof background.model === 'string')
  );
};

/** Selects safe operator copy without displaying server diagnostics. */
export function describeFailure(failure: ApiFailure): string {
  return requestFailureMessage(failure);
}

/** Same-origin blog endpoints; mutations are never retried automatically. */
export function createBlogApi(fetchImplementation: typeof fetch = integrationFetch) {
  const client = createApiClient({ envelope, retry: false, fetch: fetchImplementation });
  const root = '/api/blog';
  const lifecycle = (id: string, action: string, expectedRevision: number, publishAt?: string) =>
    client.post<ArticleDetail>(`${root}/articles/${id}/${action}`, {
      body: { expectedRevision, publishAt },
      decode: shape(isDetail),
    });
  return {
    translate(id: string, language: string) {
      return client.post<ArticleDetail>(`${root}/articles/${id}/translate`, {
        body: { language },
        decode: shape(isDetail),
      });
    },
    list(
      filter: { status?: string; topic?: string; q?: string; language?: string; publication?: string },
      signal?: AbortSignal,
    ) {
      return client.get<ArticleSummary[]>(`${root}/articles`, { query: filter, signal, decode: shape(isArray) });
    },
    get(id: string) {
      return client.get<ArticleDetail>(`${root}/articles/${id}`, { decode: shape(isDetail) });
    },
    create(input: ArticleInput) {
      return client.post<ArticleDetail>(`${root}/articles`, { body: input, decode: shape(isDetail) });
    },
    update(id: string, input: ArticleInput) {
      return client.put<ArticleDetail>(`${root}/articles/${id}`, { body: input, decode: shape(isDetail) });
    },
    publish: (id: string, revision: number) => lifecycle(id, 'publish', revision),
    schedule: (id: string, revision: number, publishAt: string) => lifecycle(id, 'schedule', revision, publishAt),
    unpublish: (id: string, revision: number) => lifecycle(id, 'unpublish', revision),
    archive: (id: string, revision: number) => lifecycle(id, 'archive', revision),
    revisions(id: string) {
      return client.get<RevisionSummary[]>(`${root}/articles/${id}/revisions`, { decode: shape(isArray) });
    },
    restore(id: string, revision: number, expectedRevision: number) {
      return client.post<ArticleDetail>(`${root}/articles/${id}/revisions/${revision}/restore`, {
        body: { expectedRevision },
        decode: shape(isDetail),
      });
    },
    topics() {
      return client.get<TopicDocument[]>(`${root}/topics`, { decode: shape(isArray) });
    },
    saveTopic(topic: TopicDocument) {
      return client.put<TopicDocument>(`${root}/topics/${encodeURIComponent(topic.key)}`, {
        body: topic,
        decode: shape(isObject),
      });
    },
    deleteTopic(key: string) {
      return client.delete(`${root}/topics/${encodeURIComponent(key)}`, { response: 'empty' });
    },
    settings() {
      return client.get<SettingsEnvelope>(`${root}/settings`, { decode: shape(isObject) });
    },
    brand() {
      return client.get<BrandStatus>(`${root}/brand`, { decode: shape(isObject) });
    },
    saveSettings(settings: SiteSettings, expectedRevision: number) {
      return client.put<SettingsEnvelope>(`${root}/settings`, {
        body: { settings, expectedRevision },
        decode: shape(isObject),
      });
    },
    editingCapabilities(signal: AbortSignal) {
      return client.get<MediaEditingCapabilities>(`${root}/media/editing-capabilities`, {
        signal,
        decode: shape(isEditingCapabilities),
      });
    },
    applyMediaEdit(sha: string, recipe: ImageEditRecipe, signal: AbortSignal) {
      return client.post<MediaView>(`${root}/media/${encodeURIComponent(sha)}/edits`, {
        signal,
        body: recipe,
        decode: shape(isMedia),
      });
    },
    async previewMediaEdit(sha: string, recipe: ImageEditRecipe, signal: AbortSignal) {
      try {
        const response = await fetchImplementation(`${root}/media/${encodeURIComponent(sha)}/edits/preview`, {
          method: 'POST',
          signal,
          credentials: 'same-origin',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(recipe),
        });
        if (!response.ok) {
          const parsed: unknown = await response.json().catch(() => null);
          return ResultExtensions.fail(
            ApiFailureFactory.create('http', response, isObject(parsed) ? (parsed as Record<string, unknown>) : null),
          );
        }
        const type = response.headers.get('Content-Type')?.split(';')[0];
        const width = Number(response.headers.get('X-Image-Width')),
          height = Number(response.headers.get('X-Image-Height'));
        if (
          !['image/png', 'image/jpeg', 'image/webp'].includes(type ?? '') ||
          !positiveInteger(width) ||
          !positiveInteger(height)
        )
          return ResultExtensions.fail(ApiFailureFactory.create('protocol'));
        const blob = await response.blob();
        if (!blob.size || blob.size > 30 * 1024 * 1024)
          return ResultExtensions.fail(ApiFailureFactory.create('protocol'));
        return ResultExtensions.ok<MediaEditPreview>({ blob, width, height });
      } catch {
        return ResultExtensions.fail(ApiFailureFactory.create(signal.aborted ? 'cancelled' : 'transport'));
      }
    },
    media() {
      return client.get<MediaView[]>(`${root}/media`, {
        decode: shape((value) => Array.isArray(value) && value.every(isMedia)),
      });
    },
    catalogue() {
      return client.get<CatalogueProduct[]>(`${root}/catalogue`, { decode: shape(isArray) });
    },
    /** Multipart upload stays on fetch: the JSON client serializes bodies. */
    async upload(file: File) {
      const form = new FormData();
      form.append('file', file);
      try {
        const response = await fetchImplementation(`${root}/media`, { method: 'POST', body: form });
        const parsed: unknown = await response.json().catch(() => null);
        if (!response.ok)
          return ResultExtensions.fail(
            ApiFailureFactory.create('http', response, isObject(parsed) ? (parsed as Record<string, unknown>) : null),
          );
        return isObject(parsed)
          ? ResultExtensions.ok(parsed as MediaView)
          : ResultExtensions.fail(ApiFailureFactory.create('protocol'));
      } catch {
        return ResultExtensions.fail(ApiFailureFactory.create('transport'));
      }
    },
  } as const;
}

export const blogApi = createBlogApi();

export const blogLinks = {
  preview: (id: string) => `/preview/${id}`,
  story: (slug: string, language = 'en') => `${language === 'en' ? '' : `/${language}`}/journal/${slug}/`,
  site: '/',
  export: '/api/blog/export/site.zip',
  media: (sha: string) => `/media/${sha}/preview`,
} as const;
