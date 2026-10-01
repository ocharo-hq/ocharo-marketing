import { onScopeDispose, reactive, ref, shallowRef, watch } from 'vue';
import type { ArticleSummary } from '../../domain/blog/BlogModels';
import { blogApi, describeFailure } from '../../integration/blog';
import { useBlogStore } from './BlogStore';

/** Loads the story list for the current filters. */
export function useArticleList() {
  const store = useBlogStore();
  const filter = reactive({ status: '', topic: '', q: '', language: '', publication: '' });
  const articles = shallowRef<ArticleSummary[]>([]);
  const loading = ref(false);
  let controller: AbortController | null = null;
  let debounce: ReturnType<typeof setTimeout> | undefined;

  async function load() {
    controller?.abort();
    controller = new AbortController();
    const signal = controller.signal;
    loading.value = true;
    const query = Object.fromEntries(Object.entries(filter).filter(([, v]) => v !== ''));
    const result = await blogApi.list(query, signal);
    if (signal.aborted) return;
    loading.value = false;
    if (result.ok) articles.value = result.value;
    else store.fail(describeFailure(result.failure));
  }

  watch(
    () => ({ ...filter }),
    () => {
      clearTimeout(debounce);
      debounce = setTimeout(load, 180);
    },
  );
  onScopeDispose(() => {
    controller?.abort();
    clearTimeout(debounce);
  });
  return { filter, articles, loading, load };
}
