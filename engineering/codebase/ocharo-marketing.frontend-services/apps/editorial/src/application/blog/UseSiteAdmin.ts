import { ref, shallowRef } from 'vue';
import type { BrandStatus, MediaView, SiteSettings, TopicDocument } from '../../domain/blog/BlogModels';
import { blogApi, describeFailure } from '../../integration/blog';
import { useBlogStore } from './BlogStore';

/** Site settings, topics and the media library. */
export function useSiteAdmin() {
  const store = useBlogStore();
  const settings = ref<SiteSettings | null>(null);
  const revision = ref(0);
  const media = shallowRef<MediaView[]>([]);
  const brand = shallowRef<BrandStatus | null>(null);
  const busy = ref(false);
  const loading = ref(false);
  async function loadSettings() {
    loading.value = true;
    try {
      const result = await blogApi.settings();
      if (!result.ok) { store.fail(describeFailure(result.failure)); return false; }
      settings.value = result.value.settings;
      revision.value = result.value.revision;
      return true;
    } finally { loading.value = false; }
  }
  async function loadMedia() {
    const result = await blogApi.media();
    if (result.ok) media.value = result.value;
    else store.fail(describeFailure(result.failure));
  }
  return {
    settings,
    revision,
    media,
    brand,
    busy,
    loading,
    async load() {
      await Promise.all([loadSettings(), loadMedia(), (async () => {
        const result = await blogApi.brand();
        if (result.ok) brand.value = result.value;
        else store.fail(describeFailure(result.failure));
      })()]);
    },
    async save() {
      if (!settings.value) return;
      busy.value = true;
      const result = await blogApi.saveSettings(settings.value, revision.value);
      busy.value = false;
      if (!result.ok) return store.fail(describeFailure(result.failure));
      revision.value = result.value.revision;
      if (!await loadSettings()) { store.fail('Saved successfully. The current view could not refresh. Your local edits are preserved.'); return; }
      store.succeed('Sayt sozlamalari saqlandi.');
    },
    async upload(file: File) {
      const result = await blogApi.upload(file);
      if (!result.ok) return store.fail(describeFailure(result.failure));
      await loadMedia();
      return result.value;
    },
    async saveTopic(topic: TopicDocument) {
      const result = await blogApi.saveTopic(topic);
      if (!result.ok) return store.fail(describeFailure(result.failure));
      await store.loadTopics();
      store.succeed(`“${result.value.name}” bo‘limi saqlandi.`);
    },
    async deleteTopic(key: string) {
      const result = await blogApi.deleteTopic(key);
      if (!result.ok) return store.fail(describeFailure(result.failure));
      await store.loadTopics();
      store.succeed('Bo‘lim o‘chirildi.');
    },
  };
}
