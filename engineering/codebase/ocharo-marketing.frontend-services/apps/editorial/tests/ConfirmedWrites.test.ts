import { effectScope } from 'vue';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ResultExtensions } from '@wow-two-beta/ui-vue/foundation/results';
import { ApiFailureFactory } from '@wow-two-beta/ui-vue/foundation/http';
import { blogApi } from '../src/integration/blog';
import { useArticleEditor } from '../src/application/blog/UseArticleEditor';
import { useSiteAdmin } from '../src/application/blog/UseSiteAdmin';
import { emptyArticle } from '../src/domain/blog/BlogRules';
import type { ArticleDetail } from '../src/domain/blog/BlogModels';
const scopes: ReturnType<typeof effectScope>[]=[];
afterEach(()=>{scopes.splice(0).forEach(scope=>scope.stop());vi.restoreAllMocks();});
function setup() { const scope=effectScope();scopes.push(scope);return scope.run(()=>({editor:useArticleEditor(),admin:useSiteAdmin()}))!; }
function deferred<T>() { let resolve!: (value:T)=>void; const promise=new Promise<T>(settle=>{resolve=settle;});return {promise,resolve}; }
function detail(title:string,revision=1):ArticleDetail { const document=emptyArticle('style');delete document.expectedRevision;return {id:'story-1',document:{...document,status:'draft',title,publishAt:null,publishedAt:null,translationKey:null},revision,createdAt:'2026-10-02T00:00:00Z',updatedAt:'2026-10-02T00:00:00Z',readMinutes:1,words:1}; }
describe('confirmed editorial writes',()=>{
 it('keeps the local story until the write acknowledgement and independent read, ignoring the mutation projection',async()=>{
  const {editor}=setup();editor.startNew('style');editor.draft.value!.title='Local draft';
  const write=deferred<ReturnType<typeof ResultExtensions.ok<ArticleDetail>>>();const read=deferred<ReturnType<typeof ResultExtensions.ok<ArticleDetail>>>();
  vi.spyOn(blogApi,'create').mockReturnValue(write.promise);const get=vi.spyOn(blogApi,'get').mockReturnValue(read.promise);vi.spyOn(blogApi,'revisions').mockResolvedValue(ResultExtensions.ok([]));
  const pending=editor.save();expect(get).not.toHaveBeenCalled();expect(editor.busy.value).toBe(true);expect(editor.loading.value).toBe(false);expect(editor.draft.value!.title).toBe('Local draft');
  write.resolve(ResultExtensions.ok(detail('Mutation payload')));await Promise.resolve();await Promise.resolve();
  expect(get).toHaveBeenCalledTimes(1);expect(editor.loading.value).toBe(true);expect(editor.draft.value!.title).toBe('Local draft');
  read.resolve(ResultExtensions.ok(detail('Read projection')));await pending;
  expect(editor.draft.value!.title).toBe('Read projection');expect(editor.dirty.value).toBe(false);
 });
 it('keeps the acknowledged story identity for retry when the projection fails',async()=>{
  const {editor}=setup();editor.startNew('style');editor.draft.value!.title='Preserved';
  const create=vi.spyOn(blogApi,'create').mockResolvedValue(ResultExtensions.ok(detail('Write response')));
  const unavailable=ResultExtensions.fail(ApiFailureFactory.create('http',new Response(null,{status:503})));
  vi.spyOn(blogApi,'get').mockResolvedValue(unavailable);const update=vi.spyOn(blogApi,'update').mockResolvedValue(unavailable);
  await editor.save();expect(editor.detail.value).toBeNull();expect(editor.receipt.value).toEqual({id:'story-1',revision:1});expect(editor.draft.value!.title).toBe('Preserved');expect(editor.dirty.value).toBe(true);
  await editor.save();expect(create).toHaveBeenCalledTimes(1);expect(update).toHaveBeenCalledWith('story-1',expect.objectContaining({expectedRevision:1,title:'Preserved'}));
 });
});
