# Backlog

*Last updated: 2026-11-09*

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
added while adopting `@wow-two-beta/fe` 0.0.28 for the suite frame, 0.0.31 for its glass and 0.0.32 for the glass
phone bar (2026-10-21). Each is a fix for the SDK, not a product workaround; the narrow product adapters stay until a
published release closes the gap.

| Gap | Product adapter | Closes when |
|---|---|---|
| `FilePickerProps` omits the native `accept` attribute; strict Vue templates reject it (still open in 0.0.32) | `apps/web/.../compatibility/MediaPicker.vue` | The SDK declares `accept`; delete the adapter |
| `SelectPickerTriggerProps` omits `aria-label`; strict Vue templates reject it (still open in 0.0.32) | `apps/web/.../compatibility/PickerTrigger.vue` | The SDK declares native accessible naming; delete the adapter |
| `MarkdownEditor` prints its view-mode identifiers with no locale key (still open in 0.0.32) | Editorial's translated selector hides the SDK one (`SectionTextEditor.vue`) | The SDK localizes view-mode labels |
| The router has no app-wide "unsaved work" state: `useNavigationBlocker` guards navigation only, so a frame cannot hold sign-out on the same state | `packages/connect/src/UnsavedWork.ts` (`trackUnsavedWork`, `unsavedWork`) | The SDK exposes the blockers' combined state; delete the registry |
| `MIGRATION.md` has no rows for 0.0.29 and 0.0.30 (read-only inputs and pickers, combobox select triggers) | None needed | The SDK records every published version |

## Session expiry adoption

Marketing and Editorial sign in through Ocharo's Identity BFF with secure HttpOnly cookies, an in-memory CSRF token and an
identity-change reload rule (`packages/connect/src/session.ts`); they do not use the SDK auth bridge. Adopted narrowly:
a 401 is quiet (no error toast), and each expiry of a signed-in session publishes the SDK's one neutral
`feedbackSessionExpiry` notice. Kept product behavior: expiry never navigates, so unsaved editors survive, and
the sign-in link carries the return path captured at expiry. `signInRedirect` and `useReturnTo`
are not adopted because the server sign-in route restores the return path. Full bridge adoption needs a cookie-session
strategy with CSRF, the identity-change rule and the legacy ticket fallback; it is a separate auth change.

## Suite frame follow-ups

- **Page headings.** Pages keep their editorial headlines ("Each channel's purpose") as `<h1>`; the shell convention
  asks the heading to carry the navigation label. Align when the pages are next reworked.

## Identity follow-ups

After the move to Ocharo's Identity (2026-11-04):

- **Sessions.** Both hosts keep BFF sessions in memory, so a restart signs everyone out; a durable SQLite or shared cache
  would end that.
- **Local containers.** `deployment/docker-compose.yml` and `deployment/blog/docker-compose.yml` set no `Identity:Client:*`
  or organization id, so neither boots alone.
- **A removed person's queued publication.** Editorial's `ScheduledPublishingWorker` poses the same question: a person
  schedules a story, is removed, and it still publishes, with no declared reaction, because the timer records no person.
  `PublicationWorker` records no person either: a publication is the organization's
  approved plan, cutting a provider call midway leaves its state unknown, and a due date days away outlives the tracker's
  one-hour signal memory. Whether a removal should withdraw that person's queued plans and scheduled stories needs a
  decision before live publishing turns on.
- **Public writes in the BFF.** Each host branches `/api/public/*` around `UseIdentityBff`, whose antiforgery check would
  refuse a signed-in visitor's form; an SDK exemption for public writes would replace that branch.

## Convention sweep

WoW2 convention breaks found on 2026-11-09, grouped by theme; the top row is the next to pull. Each id is a row of the [sweep](../research/convention-sweep.md).

| Item | Type | Notes |
|---|---|---|
| PostgreSQL for Blog and Marketing | `engineering` | Both services move from SQLite volumes to PostgreSQL; import existing data, drop `.db` volumes. Size L; changes data. Covers M-23 ([sweep](../research/convention-sweep.md)). Decided 2026-11-09 (developer): one `ocharo_{env}` database, shared with Desk and Identity, with schemas `blog` and `marketing`; bespoke SQL migrations; FKs to Identity's organization and user ids; the SQLite data copied, dev first, the tool reused for prod; BFF sessions and Data Protection keys in PostgreSQL, which ends sign-out on restart. |
| Bespoke SQL migrations and deploy verbs | `engineering` | Marketing and Blog replace EF migrations and `EnsureCreated` with SQL sets, `app migrate` and `migrate:` deploy items. Size L; changes data. Covers M-24, M-1 ([sweep](../research/convention-sweep.md)). |
| SDK host composition | `engineering` | Both hosts adopt the API boot bundle, `/health`, `SpaHosting`, SDK JSON, option records, `AddPeriodicJob`, `config check`. Size L; M-14 changes behaviour (JSON typing). Covers M-2, M-9, M-11, M-12, M-13, M-14, M-15, M-16, M-17 ([sweep](../research/convention-sweep.md)). |
| Mediator and AppResult | `engineering` | Marketing and Blog return `AppResult` from handlers instead of exceptions plus middleware; split Marketing into subdomains. Size L; no behaviour change. Covers M-20, M-21 ([sweep](../research/convention-sweep.md)). |
| TimeProvider and line length | `engineering` | Replace own `IClock` and direct `UtcNow` reads with `TimeProvider`; format to 120 characters. Size M; no behaviour change. Covers M-18, M-19, M-22 ([sweep](../research/convention-sweep.md)). |
| AwesomeAssertions and test structure | `engineering` | Convert ~1,450 `Assert.*` calls, split test tiers, add `FakeTimeProvider`, rename tests. Marketing and Blog. Size L; no behaviour change. Covers M-25, M-26, M-27, M-28 ([sweep](../research/convention-sweep.md)). |
| Frontend SDK and structure | `engineering` | Re-pin `@wow-two-beta/fe` 0.0.32 to latest, delete app-local wrappers, return `Result` in connect, slice `marketing` by area. Size M; M-29 changes behaviour. Covers M-29, M-33, M-34, M-35 ([sweep](../research/convention-sweep.md)). |
| Private app document | `engineering` | Web and Editorial gain favicon, `apple-touch-icon`, `noindex` meta and a boot splash twin. Size S; changes behaviour (head and markup). Covers M-30, M-31, M-32 ([sweep](../research/convention-sweep.md)). |
| Deploy descriptor and CI hygiene | `engineering` | `deploy.yml` identity, `dotnetEnvironment`, `verify`; pipelines tag pin, Renovate, port ledger, Dockerfile comment. Size S; M-4 changes dev config. Covers M-3, M-4, M-5, M-6, M-7, M-8, M-10 ([sweep](../research/convention-sweep.md)). |
| Docs hygiene | `engineering` | Move `output/` and `evidence/` to `engineering/operations/`, untrack 4 MB HTML, add lead docs, delete `handoff.md`, split v0.1, fix backlog tables. Size M; no behaviour change. Covers M-36, M-37, M-38, M-39, M-40, M-41, M-42, M-43 ([sweep](../research/convention-sweep.md)). |

## Retained source follow-ups

- Newsletter capture and story-to-campaign preparation remain in the adopted Blog research backlog.
- Hosted access, real provider acceptance and SDK extraction require their own verification.
- [Historical Blog backlog](../research/previous-work/blog/backlog.md) preserves unbuilt source items.
- [External setup](../research/previous-work/external-setup.md) preserves account/provider requirements.
