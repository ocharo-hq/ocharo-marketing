import { describe, it, expect, vi } from 'vitest';
import { emptyArticle, toInput } from '../src/domain/blog/BlogRules';
import type { ArticleDocument } from '../src/domain/blog/BlogModels';
import { createBlogApi } from '../src/integration/blog/BlogApi';
describe('editorial preservation', () => {
  it('round trips publication metadata through a saved revision', () => {
    const source = {
      ...emptyArticle('cloth'),
      publication: 'atlas',
      kind: 'textile',
      status: 'draft',
      publishAt: null,
      publishedAt: null,
      translationKey: null,
      places: ['Bukhara'],
      techniques: ['Ikat'],
      motifs: ['Unattributed'],
      occasions: [],
      textileSlugs: [],
      imageProvenance: { kind: 'archival', credit: 'Museum', rights: 'CC0', sourceUrl: 'https://example.test/object' },
      textile: {
        objectId: '1',
        collection: 'Museum',
        place: 'Bukhara',
        period: 'Unknown',
        materials: 'Silk',
        technique: 'Ikat',
        dimensions: null,
        maker: null,
        attributionNote: 'Unknown maker',
      },
    } as ArticleDocument;
    const draft = toInput(source, 3);
    expect(draft.publication).toBe('atlas');
    expect(draft.textile).toEqual(source.textile);
    expect(draft.imageProvenance).toEqual(source.imageProvenance);
    draft.places?.push('Other');
    expect(source.places).toEqual(['Bukhara']);
  });
  it('does not retry an expired save or mutate the draft', async () => {
    const fetcher = vi
      .fn()
      .mockResolvedValue(
        new Response(JSON.stringify({ detail: 'Connect a current ticket' }), {
          status: 401,
          headers: { 'Content-Type': 'application/problem+json' },
        }),
      );
    const draft = emptyArticle('cloth');
    draft.title = 'Unsaved words';
    const result = await createBlogApi(fetcher).update('story-id', draft);
    expect(result.ok).toBe(false);
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(draft.title).toBe('Unsaved words');
  });
});
