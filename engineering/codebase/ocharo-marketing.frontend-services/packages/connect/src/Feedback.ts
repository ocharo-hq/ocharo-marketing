import { watch, type Ref } from "vue";
import {
  createFeedbackBus,
  feedbackSessionExpiry,
  NoticeTone,
  SessionExpiredNotice,
} from "@wow-two-beta/fe/feedback";
import { useUiLocale } from "./UseUiLocale";

/** The app's one feedback stream; each app bundle owns its own instance of this module. */
export const feedbackBus = createFeedbackBus();

/** Reports each request failure message as one error toast in the operator language. */
export function useErrorToast(error: Ref<string | null>) {
  const { t } = useUiLocale();
  watch(
    error,
    (message) => {
      if (message)
        feedbackBus.notify({
          id: message,
          tone: "danger",
          title: t("Could not complete the request"),
          description: t(message),
          durationMs: 10000,
        });
    },
    { immediate: true },
  );
}

/** Reports one display-safe English source message in the operator language; a repeated `id` replaces the last. */
export function notifyFeedback(
  message: string,
  id: string,
  tone: typeof NoticeTone.Info | typeof NoticeTone.Danger = NoticeTone.Info,
): void {
  const { t } = useUiLocale();
  feedbackBus.notify(
    tone === NoticeTone.Danger
      ? {
          id,
          tone,
          title: t("Could not complete the request"),
          description: t(message),
          durationMs: 10000,
        }
      : { id, tone, title: t(message), durationMs: 10000 },
  );
}

/** Creates the session-expiry listener: the SDK's one neutral sign-out notice, in the operator language. */
export function sessionExpiryNotice(): () => void {
  const { t } = useUiLocale();
  return () => {
    const { title } = SessionExpiredNotice;
    feedbackSessionExpiry(feedbackBus, {
      ...SessionExpiredNotice,
      title: typeof title === "string" ? t(title) : title,
    })();
  };
}
