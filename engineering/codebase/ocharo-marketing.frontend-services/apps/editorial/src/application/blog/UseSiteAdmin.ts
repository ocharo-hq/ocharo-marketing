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
  return {
    settings,
    revision,
    media,
    brand,
    busy,
    async load() {
      const [s, m, b] = await Promise.all([blogApi.settings(), blogApi.media(), blogApi.brand()]);
      brand.value = b.ok ? b.value : null;
      if (s.ok) {
        settings.value = s.value.settings;
        revision.value = s.value.revision;
      } else store.fail(describeFailure(s.failure));
      if (m.ok) media.value = m.value;
    },
    async save() {
      if (!settings.value) return;
      busy.value = true;
      const result = await blogApi.saveSettings(settings.value, revision.value);
      busy.value = false;
      if (!result.ok) return store.fail(describeFailure(result.failure));
      settings.value = result.value.settings;
      revision.value = result.value.revision;
      store.succeed('Sayt sozlamalari saqlandi.');
    },
    async upload(file: File) {
      const result = await blogApi.upload(file);
      if (!result.ok) return store.fail(describeFailure(result.failure));
      media.value = [result.value, ...media.value.filter((m) => m.sha256 !== result.value.sha256)];
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
