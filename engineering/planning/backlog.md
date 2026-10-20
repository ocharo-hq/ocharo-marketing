# Backlog

*Last updated: 2026-10-21*

## Features

| Feature | State | Evidence |
|---|---|---|
| Editorial and public apps | implemented; owner review open | [Blog adoption](../architecture/blog-adoption.md) |
| Marketing operations | implemented; live setup open | [Marketing contract](../architecture/marketing-contract.md) |
| Catalogue publication | implemented; owner review open | [Catalogue build evidence](../research/previous-work/catalogue-edition-build.md) |
| Named responsive web catalogues | implemented; owner review pending | [Web catalogue verification](version-track/v0.1/catalogue-web-verification.md) |
| Operator languages | implementation | [Active version](version-track/v0.1/v0.1.md) |
| Ocharo suite frame and app links | implemented; owner review open | [Iteration 6](version-track/v0.1/v0.1.md#iteration-6--ocharo-suite-frame) |

## Delivery and research

Active work belongs to [v0.1](version-track/v0.1/v0.1.md). The [delivery/metrics increment](../architecture/delivery-increment.md) specifies the next independent local build; external execution remains disabled. Remaining scope beyond the initial reviewable app:

| Priority | Work | Evidence needed |
|---|---|---|
| 1 | Complete actual channel onboarding and OAuth/token lifecycle | Account-specific scopes, authorized tests, secure refresh/revocation |
| 2 | Enable authorized publication and reconciliation | Durable stage checkpoints, concrete destination, provider receipts |
| 3 | Add actual metrics and manual observations | Proven metric semantics, timestamps, unavailable states |
| 4 | Refine T03–T06 and motion | Suitable real assets, readable proofs, selected designs |
| 5 | Provide hosted access for Maftuna/SMM | Authenticated operators, backups, protected assets, deployment evidence |
| 6 | Extract reusable SDK capability vectors | Complete inventory, package verification, consumer adoption |
| 7 | Decide merge or independent generalized product | Observed use and maintenance boundary |

## SDK gaps

Found while adopting `@wow-two-beta/fe` 0.0.23 and `WoW2.Sdk.Beta.Be` 10.0.71-beta (2026-10-19), and re-checked or
added while adopting `@wow-two-beta/fe` 0.0.28 for the suite frame (2026-10-21). Each is a fix for the SDK, not a
product workaround; the narrow product adapters stay until a published release closes the gap.

| Gap | Product adapter | Closes when |
|---|---|---|
| `FilePickerProps` omits the native `accept` attribute; strict Vue templates reject it (still open in 0.0.28) | `apps/web/.../compatibility/MediaPicker.vue` | The SDK declares `accept`; delete the adapter |
| `SelectPickerTriggerProps` omits `aria-label`; strict Vue templates reject it (still open in 0.0.28) | `apps/web/.../compatibility/PickerTrigger.vue` | The SDK declares native accessible naming; delete the adapter |
| `MarkdownEditor` prints its view-mode identifiers with no locale key (still open in 0.0.28) | Editorial's translated selector hides the SDK one (`SectionTextEditor.vue`) | The SDK localizes view-mode labels |
| `AccountMenu`'s scrolling panel overflows its width by a few pixels, so a classic scrollbar draws a horizontal bar | `overflow-x-hidden` in both frames' `panel-class`, as Studio does | The SDK panel fits its content; drop the class |
| The router has no app-wide "unsaved work" state: `useNavigationBlocker` guards navigation only, so a frame cannot hold sign-out on the same state | `packages/connect/src/UnsavedWork.ts` (`trackUnsavedWork`, `unsavedWork`) | The SDK exposes the blockers' combined state; delete the registry |
| The Ocharo glass strength is not a theme token; every Ocharo app copies the `oc-glass*` utilities | `packages/connect/src/OcharoFrame.css` (shared by both apps) | `theme-ocharo` carries the glass materials |
| `MIGRATION.md` has no rows for 0.0.24–0.0.27 (tooltip layer, clipboard fallback, themed scrollbars) | None needed | The SDK records every published version |

## Session expiry adoption

Marketing and Editorial sign in through Desk SSO with secure HttpOnly cookies, an in-memory CSRF token and an
identity-change reload rule (`packages/connect/src/session.ts`); they do not use the SDK auth bridge. Adopted narrowly:
a 401 is quiet (no error toast), and each expiry of a signed-in session publishes the SDK's one neutral
`feedbackSessionExpiry` notice. Kept product behavior: expiry never navigates, so unsaved editors survive, and
Reconnect opens Desk sign-in in a new tab with the return path captured at expiry. `signInRedirect` and `useReturnTo`
are not adopted because the server sign-in route restores the return path. Full bridge adoption needs a cookie-session
strategy with CSRF, the identity-change rule and the legacy ticket fallback; it is a separate auth change.

## Suite frame follow-ups

- **SSO identity gap.** Desk's `/api/sso/session` grant carries a user id, organization and permissions but no display
  name or email, so the account menu names the session ("Signed in through Ocharo Desk") instead of the person and
  shows the anonymous avatar. Closes when Desk's SSO session (shared `Ocharo.Sso` snapshot) serves a display name and
  email for the signed-in user; then pass them as the `AccountMenu` `name` and `description`.
- **Page headings.** Pages keep their editorial headlines ("Each channel's purpose") as `<h1>`; the shell convention
  asks the heading to carry the navigation label. Align when the pages are next reworked.

## Retained source follow-ups

- Newsletter capture and story-to-campaign preparation remain in the adopted Blog research backlog.
- Hosted access, real provider acceptance and SDK extraction require their own verification.
- [Historical Blog backlog](../research/previous-work/blog/backlog.md) preserves unbuilt source items.
- [External setup](../research/previous-work/external-setup.md) preserves account/provider requirements.
