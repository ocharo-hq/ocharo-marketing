import { effectScope } from 'vue';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/ui-vue/foundation/http';
import { blogApi } from '../src/integration/blog';
import { useArticleList } from '../src/application/blog/UseArticleList';
import { useSiteAdmin } from '../src/application/blog/UseSiteAdmin';
import type { ArticleSummary, SettingsEnvelope } from '../src/domain/blog/BlogModels';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
import StoriesPage from '../src/presentation/blog/StoriesPage.vue';
import EditorSkeleton from '../src/presentation/blog/components/EditorSkeleton.vue';
let wrapper: VueWrapper;
const scopes: ReturnType<typeof effectScope>[] = [];
beforeEach(() => {
  useUiLocale().setUiLocale('en');
});
afterEach(() => {
  wrapper?.unmount();
  scopes.splice(0).forEach((scope) => scope.stop());
  vi.restoreAllMocks();
});
function deferred<T>() {
  let resolve!: (value: T) => void;
  return {
    promise: new Promise<T>((settle) => {
      resolve = settle;
    }),
    resolve: (value: T) => resolve(value),
  };
}
function render() {
  wrapper = mount(StoriesPage, { global: { stubs: { RouterLink: { props: ['to'], template: '<a><slot /></a>' } } } });
  return wrapper;
}
describe('editorial query result geometry', () => {
  it('starts pending before its first read can settle, without presenting an unavailable result', () => {
    const read = vi.spyOn(blogApi, 'list');
    const scope = effectScope();
    scopes.push(scope);
    const list = scope.run(() => useArticleList())!;
    expect(list.loading.value).toBe(true);
    expect(list.hasLoaded.value).toBe(false);
    expect(read).not.toHaveBeenCalled();
  });

  it('settles an initial story failure without a false empty label or result Retry', async () => {
    vi.spyOn(blogApi, 'list').mockResolvedValue(ResultExtensions.fail(ApiFailureFactory.create('transport')));
    render();
    await flushPromises();
    expect(wrapper.text()).not.toContain('Story not found.');
    expect(wrapper.text()).toContain('Content unavailable');
    expect(wrapper.findAll('button').find((button) => button.text() === 'Refresh')).toBeTruthy();
    expect(wrapper.get('.query-label').classes()).not.toContain('panel');
  });
  it('loads six-column table skeletons and retains story identity during refresh', async () => {
    const first = deferred<ReturnType<typeof ResultExtensions.ok<ArticleSummary[]>>>();
    const read = vi.spyOn(blogApi, 'list').mockReturnValueOnce(first.promise);
    render();
    await flushPromises();
    expect(wrapper.findAll('thead th')).toHaveLength(6);
    expect(wrapper.findAll('tbody tr')).toHaveLength(5);
    expect(wrapper.findAll('tbody td')).toHaveLength(30);
    const story = {
      id: 'story',
      slug: 'story',
      title: 'Saved story',
      language: 'en',
      status: 'draft',
      topic: 'style',
      words: 100,
      readMinutes: 1,
      updatedAt: '2026-10-01',
      revision: 1,
      publishAt: null,
      publishedAt: null,
      hasCover: false,
      productCount: 0,
      excerpt: null,
      translationKey: null,
    } as ArticleSummary;
    first.resolve(ResultExtensions.ok([story]));
    await flushPromises();
    const next = deferred<ReturnType<typeof ResultExtensions.ok<ArticleSummary[]>>>();
    read.mockReturnValueOnce(next.promise);
    await wrapper
      .findAll('button')
      .find((button) => button.text() === 'Refresh')!
      .trigger('click');
    expect(wrapper.findAll('thead th')).toHaveLength(6);
    expect(wrapper.get('tbody tr').text()).toContain('Saved story');
    next.resolve(ResultExtensions.ok([story]));
    await flushPromises();
  });
  it('renders a successful empty list as a plain label outside the table card', async () => {
    vi.spyOn(blogApi, 'list').mockResolvedValue(ResultExtensions.ok([]));
    render();
    await flushPromises();
    expect(wrapper.find('table').exists()).toBe(false);
    expect(wrapper.get('.query-label').text()).toBe('Story not found.');
    expect(wrapper.get('.query-label').classes()).not.toContain('panel');
  });
  it.each(['settings', 'story'] as const)('uses actual form fields for the %s first-load shape', (kind) => {
    wrapper = mount(EditorSkeleton, { props: { kind } });
    expect(wrapper.findAll('.panel')).toHaveLength(kind === 'settings' ? 3 : 2);
    expect(wrapper.findAll('div').some((div) => div.classes().includes('md:grid-cols-2'))).toBe(true);
    expect(wrapper.find('.h-36').exists()).toBe(false);
    expect(wrapper.findAll('[role=status]')).toHaveLength(1);
  });
  it('protects local settings through a failed independent read', async () => {
    const scope = effectScope();
    scopes.push(scope);
    const admin = scope.run(() => useSiteAdmin())!;
    const settings = {
      title: 'Saved',
      eyebrow: '',
      mastheadDescription: '',
      homeTitle: '',
      homeDescription: '',
      footerTagline: '',
      editionNote: '',
      noIndex: true,
      baseUrl: null,
      links: [],
      featuredSlug: null,
      logoSha256: null,
      heroSha256: null,
      heroAlt: '',
      bandSha256: null,
      bandAlt: '',
    };
    vi.spyOn(blogApi, 'media').mockResolvedValue(ResultExtensions.ok([]));
    vi.spyOn(blogApi, 'brand').mockResolvedValue(
      ResultExtensions.ok({ connected: false, release: 0, logoSha256: null, links: [] }),
    );
    const read = vi
      .spyOn(blogApi, 'settings')
      .mockResolvedValueOnce(ResultExtensions.ok({ settings, revision: 1 } as SettingsEnvelope));
    await admin.load();
    expect(admin.dirty.value).toBe(false);
    admin.settings.value!.title = 'Unsaved';
    expect(admin.dirty.value).toBe(true);
    read.mockResolvedValueOnce(ResultExtensions.fail(ApiFailureFactory.create('transport')));
    await admin.load();
    expect(admin.settings.value!.title).toBe('Unsaved');
    expect(admin.loading.value).toBe(false);
    expect(admin.dirty.value).toBe(true);
  });
});
