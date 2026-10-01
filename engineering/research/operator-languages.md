# Operator and catalogue language verification

*Last updated: 2026-10-01*

Marketing Desk and Editorial have independent Uzbek, Russian and English operator language
preferences. Article/post content language remains authored data. Catalogue editions are authored
in Platform; Marketing exposes only released language editions from the immutable public release.
The public shell remains English while catalogue-authored fields declare their language/direction.

## Executed checks

| Area | Result |
|---|---|
| Marketing frontend | 53 tests; lint, typecheck and production build passed |
| Editorial frontend | 35 tests; lint, typecheck and production build passed |
| Marketing backend | 254 tests passed, including HTTPS-only trusted-origin cases |
| Blog/public backend | 150 tests passed, including exact catalogue language and release-bound download cases |
| Locale independence | Component tests preserve captions, titles and document language when UI language changes |
| Public navigation | Only released edition locales appear; supported untranslated editions never silently fall back |

These are local implementation checks. Opt-in live-provider tests are not provider acceptance when
credentials/configuration are absent. No external post, catalogue release or hosted deployment was
published by this verification. User-owned services were not restarted.

Implementation details: [operator languages](../codebase/ocharo-marketing.frontend-services/docs/operator-languages.md).
Human review stays open in the [active track](../planning/version-track/v0.1/v0.1.md).
