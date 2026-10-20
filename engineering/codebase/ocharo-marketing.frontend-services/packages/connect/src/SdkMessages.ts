import { translateUi, type UiLocale } from "./UiMessages";
/** Keys from the published SDK versions used by Marketing and Editorial. Missing keys keep SDK fallbacks. */
const sdkSources: Record<string, string> = {
  "ImageEditor.label": "Image editor",
  "ImageEditor.history": "Edit history",
  "ImageEditor.undo": "Undo",
  "ImageEditor.redo": "Redo",
  "ImageEditor.reset": "Reset edits",
  "ImageEditor.original": "Original image · crop before rotation",
  "ImageEditor.aspect": "Crop aspect ratio",
  "ImageEditor.free": "Free crop",
  "ImageEditor.square": "Square",
  "ImageEditor.portrait": "Portrait 4:5",
  "ImageEditor.landscape": "Landscape 16:9",
  "ImageEditor.full": "Full image",
  "ImageEditor.preview": "Processed preview",
  "ImageEditor.previewAlt": "Preview of the requested image edits",
  "ImageEditor.previewHint":
    "Choose Preview edits to inspect the processed result. The original stays unchanged.",
  "ImageEditor.transform": "Rotate and flip",
  "ImageEditor.left": "Rotate left",
  "ImageEditor.right": "Rotate right",
  "ImageEditor.horizontal": "Flip horizontal",
  "ImageEditor.vertical": "Flip vertical",
  "ImageEditor.width": "Output width",
  "ImageEditor.height": "Output height",
  "ImageEditor.proportional": "Keep proportions",
  "ImageEditor.format": "Output format",
  "ImageEditor.quality": "Output quality (JPEG and WebP)",
  "ImageEditor.removeBackground": "Remove background",
  "ImageEditor.backgroundUnavailable":
    "Background removal is unavailable for this image.",
  "ImageEditor.busy": "Processing image…",
  "ImageEditor.previewAction": "Preview edits",
  "ImageEditor.apply": "Apply as new image",
  "ImageEditor.sourceChanged":
    "The image changed. Reset edits before continuing.",
  "ImageEditor.sourceError":
    "Image dimensions exceed the processing limits or are unavailable.",
  "ImageEditor.cropError": "Keep the crop inside the original image.",
  "ImageEditor.rotationError": "Choose a quarter-turn rotation.",
  "ImageEditor.resizeError":
    "Choose positive output dimensions within the processing limits.",
  "ImageEditor.outputError":
    "Choose PNG, JPEG or WebP and quality from 1 to 100.",
  "ImageEditor.backgroundError":
    "Background removal is unavailable. Turn it off to continue.",
  "MarkdownEditor.markdownFormatting": "Markdown formatting",
  "MarkdownEditor.viewMode": "View mode",
  "MarkdownEditor.preview": "Preview",
  "MarkdownEditor.h1": "Heading 1",
  "MarkdownEditor.h2": "Heading 2",
  "MarkdownEditor.bold": "Bold",
  "MarkdownEditor.italic": "Italic",
  "MarkdownEditor.code": "Inline code",
  "MarkdownEditor.link": "Link",
  "MarkdownEditor.list": "List",
  "MarkdownEditor.quote": "Blockquote",
  "FilePicker.label": "Choose file",
  "SelectPicker.loadingLabel": "Loading options…",
  "SelectPicker.clearLabel": "Clear selection",
  "SelectPickerContent.searchPlaceholder": "Search…",
  "SelectPickerContent.noResultsLabel": "No results",
  "DatePicker.placeholder": "Pick a date",
  "TimePicker.placeholder": "Pick a time",
  "NumberInput.incrementLabel": "Increase",
  "NumberInput.decrementLabel": "Decrease",
  "ToastHost.notifications": "Notifications",
  "AppShell.skipToContent": "Skip to main content",
};
/**
 * The SDK account menu in Desk's wording, so every Ocharo app names its rows alike. English is the SDK's default text;
 * `{name}` stays for the SDK to fill.
 */
const accountMenuMessages: Record<string, Readonly<Record<UiLocale, string>>> = {
  "AccountMenu.menuLabel": { en: "Account", uz: "Hisob", ru: "Учётная запись" },
  "AccountMenu.triggerLabel": {
    en: "Account menu for {name}",
    uz: "{name} hisob menyusi",
    ru: "Меню учётной записи {name}",
  },
  "AccountMenu.anonymousTriggerLabel": {
    en: "Account menu",
    uz: "Hisob menyusi",
    ru: "Меню учётной записи",
  },
  "AccountMenu.appearanceLabel": { en: "Appearance", uz: "Ko‘rinish", ru: "Оформление" },
  "AccountMenu.systemModeLabel": { en: "System", uz: "Tizim", ru: "Системная" },
  "AccountMenu.lightModeLabel": { en: "Light", uz: "Yorug‘", ru: "Светлая" },
  "AccountMenu.darkModeLabel": { en: "Dark", uz: "Qorong‘i", ru: "Тёмная" },
  "AccountMenu.languageLabel": { en: "Language", uz: "Til", ru: "Язык" },
  "AccountMenu.organizationLabel": { en: "Organization", uz: "Tashkilot", ru: "Организация" },
  "AccountMenu.versionsLabel": { en: "Versions", uz: "Versiyalar", ru: "Версии" },
  "AccountMenu.signOutLabel": { en: "Sign out", uz: "Chiqish", ru: "Выйти" },
};
export function sdkMessageDictionary(locale: UiLocale): Record<string, string> {
  return {
    ...Object.fromEntries(
      Object.entries(sdkSources).map(([key, source]) => [
        key,
        translateUi(source, locale),
      ]),
    ),
    ...Object.fromEntries(
      Object.entries(accountMenuMessages).map(([key, value]) => [key, value[locale]]),
    ),
  };
}
