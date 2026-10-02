import { computed, ref, shallowRef } from 'vue';
import { integrationSession } from '@ocharomarketing/connect';
import type {
  ArticleDetail,
  ArticleInput,
  CatalogueProduct,
  MediaView,
  RevisionSummary,
} from '../../domain/blog/BlogModels';
import { emptyArticle, publishBlockers, readingStats, slugify, toInput } from '../../domain/blog/BlogRules';
import { blogApi, describeFailure } from '../../integration/blog';
import { useBlogStore } from './BlogStore';
import { formatWhen } from '../../domain/blog/UzDates';

/** Edits one story: load, dirty tracking, save, lifecycle commands, media, catalogue pieces and history. */
export function useArticleEditor() {
  const store = useBlogStore();
  const detail = shallowRef<ArticleDetail | null>(null);
  const receipt = shallowRef<{ id: string; revision: number } | null>(null);
  const draft = ref<ArticleInput | null>(null);
  const baseline = ref('');
  const busy = ref(false);
  const loading = ref(false);
  const canEdit = computed(
    () => integrationSession.value?.active === true && integrationSession.value.canEdit === true,
  );
  const history = shallowRef<RevisionSummary[]>([]);
  const media = shallowRef<MediaView[]>([]);
  const products = shallowRef<CatalogueProduct[]>([]);
  const previewNonce = ref(0);
  const slugTouched = ref(false);

  const dirty = computed(() => draft.value !== null && JSON.stringify(draft.value) !== baseline.value);
  const stats = computed(() => (draft.value ? readingStats(draft.value) : { words: 0, minutes: 1 }));
  const blockers = computed(() => (draft.value ? publishBlockers(draft.value) : []));

  function adopt(next: ArticleDetail) {
    detail.value = next;
    receipt.value = { id: next.id, revision: next.revision };
    draft.value = toInput(next.document, next.revision);
    baseline.value = JSON.stringify(draft.value);
    slugTouched.value = true;
    previewNonce.value += 1;
  }

  async function loadHistory() {
    if (!detail.value) return;
    const result = await blogApi.revisions(detail.value.id);
    if (result.ok) history.value = result.value;
  }

  async function run(
    action: () => Promise<
      { ok: true; value: ArticleDetail } | { ok: false; failure: Parameters<typeof describeFailure>[0] }
    >,
    message: string,
  ) {
    busy.value = true;
    const result = await action();
    busy.value = false;
    if (!result.ok) {
      store.fail(describeFailure(result.failure));
      return false;
    }
    receipt.value = { id: result.value.id, revision: result.value.revision };
    loading.value = true;
    const current = await blogApi.get(result.value.id);
    loading.value = false;
    if (!current.ok) {
      store.fail('Saved successfully. The current view could not refresh. Your local edits are preserved.');
      return false;
    }
    adopt(current.value);
    await loadHistory();
    store.succeed(message);
    return true;
  }

  return {
    detail,
    receipt,
    draft,
    dirty,
    busy,
    loading,
    canEdit,
    history,
    media,
    products,
    stats,
    blockers,
    previewNonce,
    async load(id: string) {
      loading.value = true;
      const result = await blogApi.get(id);
      loading.value = false;
      if (!result.ok) {
        store.fail(describeFailure(result.failure));
        return false;
      }
      adopt(result.value);
      await loadHistory();
      return true;
    },
    startNew(topic: string) {
      detail.value = null;
      receipt.value = null;
      history.value = [];
      draft.value = emptyArticle(topic);
      baseline.value = JSON.stringify(draft.value);
      slugTouched.value = false;
    },
    titleChanged() {
      if (draft.value && !slugTouched.value) draft.value.slug = slugify(draft.value.title);
    },
    slugEdited() {
      slugTouched.value = true;
    },
    async loadLibraries() {
      const [m, p] = await Promise.all([blogApi.media(), blogApi.catalogue()]);
      if (m.ok) media.value = m.value;
      if (p.ok) products.value = p.value;
    },
    async upload(file: File) {
      const result = await blogApi.upload(file);
      if (!result.ok) {
        store.fail(describeFailure(result.failure));
        return null;
      }
      const current = await blogApi.media();
      if (current.ok) media.value = current.value;
      else store.fail(describeFailure(current.failure));
      return result.value;
    },
    async save(changeSummary?: string) {
      const value = draft.value;
      if (!value) return null;
      const payload = { ...value, changeSummary, expectedRevision: receipt.value?.revision ?? value.expectedRevision };
      const id = receipt.value?.id ?? detail.value?.id;
      const ok = await run(() => (id ? blogApi.update(id, payload) : blogApi.create(payload)), 'Hikoya saqlandi.');
      return ok ? (detail.value?.id ?? null) : null;
    },
    publish() {
      const d = detail.value;
      return d
        ? run(() => blogApi.publish(d.id, d.revision), 'Hikoya nashr qilindi — saytda ko‘rinadi.')
        : Promise.resolve(false);
    },
    schedule(localDateTime: string) {
      const d = detail.value;
      if (!d) return Promise.resolve(false);
      const at = new Date(localDateTime);
      if (Number.isNaN(at.getTime())) {
        store.fail('Sana va vaqtni tanlang.');
        return Promise.resolve(false);
      }
      return run(() => blogApi.schedule(d.id, d.revision, at.toISOString()), `Rejalashtirildi: ${formatWhen(at)}.`);
    },
    unpublish() {
      const d = detail.value;
      return d
        ? run(() => blogApi.unpublish(d.id, d.revision), 'Hikoya saytdan olindi (qoralama).')
        : Promise.resolve(false);
    },
    archive() {
      const d = detail.value;
      return d ? run(() => blogApi.archive(d.id, d.revision), 'Hikoya arxivlandi.') : Promise.resolve(false);
    },
    restore(revision: number) {
      const d = detail.value;
      return d
        ? run(() => blogApi.restore(d.id, revision, d.revision), `${revision}-versiya tiklandi.`)
        : Promise.resolve(false);
    },
    async translate(language: string) {
      const d = detail.value;
      if (!d) return null;
      busy.value = true;
      const result = await blogApi.translate(d.id, language);
      busy.value = false;
      if (!result.ok) {
        store.fail(describeFailure(result.failure));
        return null;
      }
      store.succeed('Tarjima qoralamasi yaratildi — matnni shu tilga o‘giring va nashr qiling.');
      return result.value.id;
    },
    reset() {
      if (detail.value) adopt(detail.value);
    },
  };
}
