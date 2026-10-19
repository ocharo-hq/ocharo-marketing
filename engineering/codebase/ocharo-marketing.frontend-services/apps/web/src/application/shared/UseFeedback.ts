import { watch, type Ref } from 'vue';
import { createFeedbackBus, feedbackSessionExpiry, SessionExpiredNotice } from '@wow-two-beta/fe/feedback';
import { useUiLocale } from '@ocharomarketing/connect/UseUiLocale';
export const feedbackBus = createFeedbackBus();
export function useErrorToast(error: Ref<string | null>) {
  const { t } = useUiLocale();
  watch(
    error,
    (message) => {
      if (message)
        feedbackBus.notify({
          id: message,
          tone: 'danger',
          title: t('Could not complete the request'),
          description: t(message),
          durationMs: 10000,
        });
    },
    { immediate: true },
  );
}
/** Creates the session-expiry listener: the SDK's one neutral sign-out notice, in the operator language. */
export function sessionExpiryNotice(): () => void {
  const { t } = useUiLocale();
  return () => {
    const { title } = SessionExpiredNotice;
    feedbackSessionExpiry(feedbackBus, {
      ...SessionExpiredNotice,
      title: typeof title === 'string' ? t(title) : title,
    })();
  };
}
