# Backlog

*Last updated: 2026-11-09*

## Features

| Feature | State | Spec |
|---|---|---|
| Editorial and public apps | shipped v0.1 | [features](../../product/features/features.md) |
| Marketing operations | shipped v0.1 | [features](../../product/features/features.md) |
| Catalogue publication | shipped v0.1 | [features](../../product/features/features.md) |
| Named responsive web catalogues | shipped v0.1 | [features](../../product/features/features.md) |
| Operator languages | shipped v0.1 | [features](../../product/features/features.md) |
| Ocharo suite frame and app links | shipped v0.1 | [features](../../product/features/features.md) |
| File storage in WoW2 Files | shipped v0.1 | — |

## Delivery

| Item | Type | Notes |
|---|---|---|
| Channel onboarding and OAuth token lifecycle | `feature` | Account-specific scopes, authorized tests, secure refresh and revocation. Next local build: [delivery increment](../architecture/delivery-increment.md); external execution stays disabled. |
| Authorized publication and reconciliation | `feature` | Durable stage checkpoints, a concrete destination, provider receipts. |
| Actual metrics and manual observations | `feature` | Proven metric semantics, timestamps, unavailable states. |
| T03–T06 templates and motion | `feature` | Suitable real assets, readable proofs, selected designs. |
| Hosted access for the SMM operators | `product` | Authenticated operators, backups, protected assets, deployment evidence. |
| Newsletter capture and story-to-campaign preparation | `feature` | Kept from the [historical Blog backlog](../research/previous-work/blog/backlog.md); account needs in [external setup](../research/previous-work/external-setup.md). |
| Reusable SDK capability vectors | `engineering` | Complete inventory, package verification, consumer adoption ([SDK extraction](../architecture/sdk-extraction.md)). |
| Merge or keep an independent generalized product | `product` | Decide from observed use and the maintenance boundary. |

## SDK gaps

Each is a fix for the SDK, not a product workaround; the narrow product adapter stays until a published release closes the gap.

| Item | Type | Notes |
|---|---|---|
| `FilePickerProps` lacks the native `accept` attribute | `engineering` | Strict Vue templates reject it (still open in 0.0.41). Adapter `apps/web/src/presentation/products/MediaPicker.vue`; delete it once the SDK declares `accept`. |
| `SelectPickerTriggerProps` lacks `aria-label` | `engineering` | Strict Vue templates reject it (still open in 0.0.41). Adapter `apps/web/src/presentation/common/PickerTrigger.vue`; delete it once the SDK declares native accessible naming. |
| `MarkdownEditor` view-mode labels have no locale key | `engineering` | Editorial's translated selector hides the SDK one (`SectionTextEditor.vue`) until the SDK localizes them. |
| The router exposes no app-wide unsaved-work state | `engineering` | `useNavigationBlocker` guards navigation only, so a frame cannot hold sign-out on it. Registry `packages/connect/src/UnsavedWork.ts` goes once the SDK exposes the blockers' combined state. |
| `SelectPicker` lacks native `required`, `fieldset[disabled]` inheritance and `aria-label` | `engineering` | Adapter `packages/connect/src/MarketingSelect.vue`; delete it once the SDK picker validates, inherits disabled state and takes accessible naming (still open in 0.0.41). |
| `MIGRATION.md` skips published versions | `engineering` | No rows for 0.0.29, 0.0.30 or 0.0.37 onward; the SDK records every published version. |

## Identity follow-ups

| Item | Type | Notes |
|---|---|---|
| A removed person's queued publication | `product` | Editorial's `ScheduledPublishingWorker` and Marketing's `PublicationWorker` record no person, so a removed person's scheduled story or plan still publishes. Decide whether a removal withdraws them before live publishing turns on. |
| Local containers boot alone | `fix` | `deployment/docker-compose.yml` and `deployment/blog/docker-compose.yml` set no `Identity:Client:*` or organization id. |
| SDK BFF session strategy | `engineering` | `@wow-two-beta/fe` 0.0.38+ ships a BFF session strategy that may replace the hand-rolled `packages/connect/src/session.ts`; check it covers CSRF, the identity-change rule and return paths ([architecture](../architecture/architecture.md#sign-in-and-session-expiry)). |
| Public writes inside the BFF | `engineering` | Each host branches `/api/public/*` around `UseIdentityBff`, whose antiforgery check refuses a signed-in visitor's form; an SDK exemption for public writes replaces the branch. |
| Page headings carry the navigation label | `fix` | Pages keep editorial headlines ("Each channel's purpose") as `<h1>`; align with the shell convention when the pages are next reworked. |

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
