import { ref, shallowRef } from 'vue';
import type { TopicDocument } from '../../domain/blog/BlogModels';
import { blogApi, blogLinks, describeFailure } from '../../integration/blog';

/** App-wide Studio state: topics and the one notice/error banner. */
const topics = shallowRef<TopicDocument[]>([]);
const error = ref<string | null>(null);
const notice = ref<string | null>(null);

export function useBlogStore() {
  return {
    topics,
    error,
    notice,
    links: blogLinks,
    async loadTopics() {
      const result = await blogApi.topics();
      if (result.ok) topics.value = result.value;
      else error.value = describeFailure(result.failure);
    },
    topicName(key: string) {
      return topics.value.find((t) => t.key === key)?.short ?? key;
    },
    fail(message: string) {
      notice.value = null;
      error.value = message;
    },
    succeed(message: string) {
      error.value = null;
      notice.value = message;
    },
  };
}
