import { computed, readonly, ref } from "vue";
import { translateUi, type UiLocale } from "./UiMessages";
export type { UiLocale } from "./UiMessages";
export const operatorLocaleKey = "ocharo.operator.locale";
const currentLocale = ref<UiLocale>("en");
const localeNames = { en: "en-GB", uz: "uz-Latn-UZ", ru: "ru-RU" } as const;
const uzMonths = [
  "yan",
  "fev",
  "mar",
  "apr",
  "may",
  "iyn",
  "iyl",
  "avg",
  "sen",
  "okt",
  "noy",
  "dek",
];
function isUiLocale(value: unknown): value is UiLocale {
  return value === "en" || value === "uz" || value === "ru";
}
/** Initializes only operator preferences; article and product languages stay in their own documents. */
export function initializeUiLocale(): void {
  let saved: string | null = null;
  try {
    saved = localStorage.getItem(operatorLocaleKey);
  } catch {
    /* Private sessions retain an in-memory choice. */
  }
  const preferred =
    saved ??
    (typeof navigator === "undefined"
      ? "en"
      : navigator.language.split("-")[0]);
  currentLocale.value = isUiLocale(preferred) ? preferred : "en";
  if (typeof document !== "undefined")
    document.documentElement.lang = currentLocale.value;
}
export function useUiLocale() {
  function setUiLocale(value: string): void {
    if (!isUiLocale(value)) return;
    currentLocale.value = value;
    if (typeof document !== "undefined") document.documentElement.lang = value;
    try {
      localStorage.setItem(operatorLocaleKey, value);
    } catch {
      /* The current session still switches. */
    }
  }
  function t(
    source: string | number | null | undefined,
    variables?: Record<string, string | number>,
  ): string {
    return source == null
      ? ""
      : translateUi(String(source), currentLocale.value, variables);
  }
  function formatDate(
    value: string | Date | null | undefined,
    options: Intl.DateTimeFormatOptions = {},
  ): string {
    if (!value) return "—";
    const date = value instanceof Date ? value : new Date(value);
    if (!Number.isFinite(date.getTime())) return t("Date needs checking");
    const settings: Intl.DateTimeFormatOptions = {
      day: "numeric",
      month: "short",
      ...options,
      timeZone: "Asia/Tashkent",
    };
    // Explicit Uzbek month names avoid incomplete ICU locale data on some browsers.
    if (currentLocale.value === "uz") {
      const parts = new Intl.DateTimeFormat("en-GB", {
        ...settings,
        month: settings.month ? "numeric" : undefined,
        hourCycle: "h23",
      }).formatToParts(date);
      return parts
        .map((part) =>
          part.type === "month" && settings.month === "short"
            ? (uzMonths[Number(part.value) - 1] ?? part.value)
            : part.value,
        )
        .join("");
    }
    return new Intl.DateTimeFormat(localeNames[currentLocale.value], {
      ...settings,
      hourCycle: "h23",
    }).format(date);
  }
  return {
    uiLocale: readonly(currentLocale),
    setUiLocale,
    t,
    formatDate,
    formatWhen: (value: string | Date | null | undefined, withTime = true) =>
      formatDate(value, {
        year: "numeric",
        ...(withTime ? { hour: "2-digit", minute: "2-digit" } : {}),
      }),
    formatNumber: (value: number) =>
      new Intl.NumberFormat(localeNames[currentLocale.value]).format(value),
    sdkMessages: computed(() => sdkMessageDictionary(currentLocale.value)),
  };
}
import { sdkMessageDictionary } from "./SdkMessages";
