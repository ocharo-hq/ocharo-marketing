import { describe, expect, it, vi } from 'vitest';
import { createBlogApi } from '../src/integration/blog/BlogApi';
import { toInput } from '../src/domain/blog/BlogRules';

const sha = 'b'.repeat(64);
/** A story as the SDK JSON preset writes it: every null property (`WhenWritingNull`) left out. */
const story = {
  id: 'story-1',
  revision: 3,
  createdAt: '2026-11-01T08:00:00Z',
  updatedAt: '2026-11-02T08:00:00Z',
  readMinutes: 2,
  words: 320,
  document: {
    slug: 'kept-clothes',
    topic: 'wardrobe',
    status: 'draft',
    language: 'en',
    publication: 'blog',
    kind: 'story',
    places: [],
    techniques: [],
    motifs: [],
    occasions: [],
    textileSlugs: [],
    title: 'Clothes we keep',
    sections: [{ heading: 'Why', paragraphs: ['Because.'] }],
    sources: [{ label: 'Archive', url: 'https://example.test/archive' }],
    relatedSlugs: [],
    cover: { mediaSha256: sha, alt: 'A nimcha', caption: '' },
    productIds: [],
  },
};
const fetching = (body: unknown) => vi.fn<typeof fetch>(async () => Response.json(body));

describe('Blog wire shape under the SDK JSON preset', () => {
  it('reads omitted nullable story fields as null, and sends them back explicitly', async () => {
    const result = await createBlogApi(fetching(story)).get('story-1');
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const { document } = result.value;
    expect(document).toMatchObject({
      excerpt: null,
      intro: null,
      format: null,
      readerPromise: null,
      takeaway: null,
      closingQuestion: null,
      cardPrompt: null,
      publishAt: null,
      publishedAt: null,
      seoTitle: null,
      seoDescription: null,
      translationKey: null,
      textile: null,
      imageProvenance: null,
    });
    expect(document.sections[0]).toMatchObject({ list: null, sourceIndexes: null });
    expect(document.sources[0]?.supports).toBeNull();
    expect(document.cover?.cardClass).toBeNull();
    const input = toInput(document, result.value.revision);
    expect(JSON.parse(JSON.stringify(input))).toMatchObject({
      excerpt: null,
      seoTitle: null,
      cover: { cardClass: null },
    });
  });

  it('accepts images, catalogue pieces, settings and editing limits without their empty fields', async () => {
    const media = {
      sha256: sha,
      fileName: 'a.png',
      contentType: 'image/png',
      byteLength: 4,
      url: `/media/${sha}`,
    };
    const api = (body: unknown) => createBlogApi(fetching(body));
    expect(await api([media]).media()).toEqual({
      ok: true,
      value: [{ ...media, width: null, height: null, previewUrl: null }],
    });
    expect(await api([{ id: 'p', name: 'Nimcha', garmentName: 'Nimcha' }]).catalogue()).toMatchObject({
      ok: true,
      value: [{ fabric: null, retailPrice: null, imageUrl: null, previewUrl: null }],
    });
    expect(await api({ settings: { title: 'Ocharo Blog', links: [] }, revision: 1 }).settings()).toMatchObject({
      ok: true,
      value: { settings: { baseUrl: null, featuredSlug: null, logoSha256: null, heroSha256: null, bandSha256: null } },
    });
    expect(await api({ connected: false, release: 0, links: [] }).brand()).toMatchObject({
      ok: true,
      value: { logoSha256: null },
    });
    const limits = {
      crop: true,
      rotate: true,
      flip: true,
      resize: true,
      formats: ['png'],
      maximumBytes: 1,
      maximumPixels: 1,
      maximumDimension: 1,
      backgroundRemoval: { available: false },
    };
    expect(await api(limits).editingCapabilities(new AbortController().signal)).toMatchObject({
      ok: true,
      value: { backgroundRemoval: { available: false, model: null } },
    });
  });

  it('reads omitted story-list fields as null', async () => {
    const summary = {
      id: 'story-1',
      slug: 'kept-clothes',
      topic: 'wardrobe',
      status: 'draft',
      title: 'Clothes we keep',
      readMinutes: 2,
      words: 320,
      hasCover: false,
      productCount: 0,
      revision: 3,
      updatedAt: '2026-11-02T08:00:00Z',
      language: 'en',
    };
    expect(await createBlogApi(fetching([summary])).list({})).toMatchObject({
      ok: true,
      value: [{ excerpt: null, publishAt: null, publishedAt: null, translationKey: null }],
    });
  });
});
