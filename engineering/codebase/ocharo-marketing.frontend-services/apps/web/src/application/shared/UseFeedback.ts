import { watch, type Ref } from 'vue';
import { createFeedbackBus } from '@wow-two-beta/ui-vue/feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
export const feedbackBus = createFeedbackBus();
export function useErrorToast(error: Ref<string | null>) {
  const { t } = useUiLocale();
  watch(error, message => {
    if (message) feedbackBus.notify({ id: message, tone: 'danger', title: t('Could not complete the request'), description: t(message), durationMs: 10000 });
  }, { immediate: true });
}
