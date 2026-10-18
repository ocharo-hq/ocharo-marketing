# Operator languages

Marketing desk and Editorial support Uzbek, Russian and English. The visible header picker persists `ocharo.operator.locale`; a supported browser language is used before a preference is saved. Restricted storage falls back to the current session.

The operator language controls navigation, labels, known workflow messages, accessible names, dates, and the published WoW2 SDK formatting/image controls. It never writes article/post language, catalogue text, product names, captions, alt text, filenames, or source evidence. Authored languages remain separate controls on their documents. Platform owns catalogue editions and the expanded public catalogue languages.

Shared implementation lives in `packages/connect/src/UseUiLocale.ts`, `UiMessages.ts`, `SdkMessages.ts` and `LanguagePicker.vue`. English source messages use explicit Uzbek/Russian translations. Legacy workflow messages have exact aliases; unknown provider responses remain verbatim. Named values inside translated notices remain verbatim.

Each app calls the published SDK `provideLocale` in its root. SDK keys absent from the dictionary retain the SDK fallback. SDK 0.0.22 exposes a controlled Markdown view but prints its view identifiers directly; Editorial provides a translated view selector and hides that duplicate SDK selector using scoped CSS. The SDK package itself is unchanged.

Validation: both app type checks, tests and production builds. Operator-locale regression tests cover persisted choices, denied storage, known errors, SDK messages, and preserving authored text while switching languages.
