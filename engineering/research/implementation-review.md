# Ocharo Marketing frontend/API review

*Last updated: 2026-09-27*

Reviewed 2026-09-27. Read-only source review against `engineering/architecture/marketing-contract.md`; this note does not establish browser runtime verification. No frontend/backend files changed. Composition persistence and framing were already being repaired by their owners.

## Original findings — repaired in source

The following findings describe the pre-repair code. All three have been repaired in the subsequent owner changes; see the recheck below.

| Priority | Finding and evidence | Concrete reproduction / smallest repair |
|---|---|---|
| P2 | All HTTP 409s become a draft-conflict message. `apps/web/src/application/marketing/UseMarketingWorkspace.ts:27` returns before inspecting `problem.detail`. `GenerationPanel.vue:165` enables Generate without provider configuration; `MarketingController.cs:74` returns 409 for missing provider settings. Text assistance and asset-integrity failures also use 409. | On a fresh unconfigured installation, submit an image request. UI claims the post changed and recommends reloading, although no post changed. Classify genuine draft conflicts explicitly; preserve safe API details for unavailable configuration, integrity failures and workspace-busy conflicts. Optional disabled actions should retain actionable setup guidance. |
| P2 | Delayed save/approve responses overwrite newer local edits. `PostComposerPage.vue:145–168` captures a request, awaits the command plus workspace refresh, then unconditionally applies server fields. `applySaved()` at 126–140 overwrites the form and marks it clean. Approve does the same at 201–203; editable inputs are not frozen. | Save caption A, type B before the response, then resolve saved A: B disappears and dirty becomes false. Capture an edit generation/snapshot and merge server identity/revision while retaining later edits, or freeze all editable controls for the entire operation. Approval must never erase changes entered after the approved snapshot was submitted. Add deferred-response tests for both paths. |
| P2 | Existing plan labels drift to the current draft. `CalendarPage.vue:47–48` gets titles from mutable drafts; links at 107/137 open the latest editor despite the agenda displaying `draftRevision`. `ResultsPage.vue:86` uses the same lookup, though no published records currently exist. | Approve/schedule revision 1 titled A, then save revision 2 titled B. The unchanged revision-1 plan appears as B and opens revision 2. Expose/capture the scheduled title, and clearly label links as opening the current draft; warn when the current revision differs. A later snapshot viewer can provide exact historical review. SQLite revision snapshots themselves remain safe. |

## Recheck after owner repairs

| Original issue | Status | Current evidence |
|---|---|---|
| Generic HTTP 409 misguidance | Repaired, source verified | `UseMarketingWorkspace.ts:27–29` prefers bounded API problem details and uses a neutral conflict fallback. Quiet workspace polling now preserves command errors rather than erasing them (`:43`). Missing-provider, integrity and stale-draft explanations can remain distinct. |
| Delayed save/approve edit loss | Repaired; delayed-save test executed | `PostComposerPage.vue:126–144` compares current editor state with the submitted snapshot. Changed fields stay in the editor, server identity/revision still advance, and the submitted snapshot remains the clean baseline. Both save and approve pass captured snapshots. A dirty check after awaited save blocks approving intervening unsaved changes. |
| Historical plan title drift | Repaired, source verified | `MarketingRepository.GetSchedulesAsync` hydrates absent `DraftTitle` from `FindRevisionAsync(draftId, draftRevision)`. Calendar uses that title and warns when the link opens a newer draft. Results uses the captured title. The persistence test now asserts revision-1 title remains unchanged after revision-2 edits. |

Executed existing targeted composer tests with Node 24.11.0: **4 passed, 0 failed**, exit 0. Cases: delayed-save edits survive; saved recipe reopens; stale render output is unavailable; composition changes invalidate selected output. No new test files or product files were edited during this review.

Final owner verification: the 13-test frontend suite includes both delayed-save and delayed-approval regressions. The 60-test backend suite covers immutable schedule titles. Extended browser testing additionally exposed an SDK response-envelope detail loss and unsupported upload roles; both were repaired. The browser now shows the specific disabled-provider reason and accepts original PNG uploads. No remaining original P2 finding was observed. OAuth, actual delivery and observed-metrics collection remain intentionally unimplemented increments, not repaired/live features.

## Verified source behavior

- Completed card outputs are selectable only while their render composition signature matches current inputs. Changing artwork fields clears selected media synchronously. Preview responses use abort plus a monotonic revision, preventing older responses from replacing current proofs.
- Reopening now hydrates saved subtitle, fabric selection, scale and normalized offsets. Route wrappers remount by `route.fullPath`, avoiding a reused editor retaining another draft's initial state. Existing composer tests explicitly cover reopen, stale-output rejection and selected-artwork invalidation.
- Approval/export use the explicitly selected output bytes, shown separately from the editable HTML preview. Saving edits clears backend approval; approval verifies selected asset hashes. Scheduling atomically checks revision and approval.
- Atlas PDF selection contains completed card output assets only, rather than new AI imagery or preview HTML. Backend accepts 1–24 rendered PNG cards. PDF is a review artifact; persisted recipes/templates drive regeneration.
- Connections distinguish configured from verified, and absence of credentials remains visible. No credentials are entered in browser forms. API mutations disable automatic retries.
- Metrics preserve null as an em dash and observed zero as zero. Empty observations receive explicit empty-state guidance. Published results require status `published`; planned schedules are excluded.
- Backend schedules currently always return `planned`, with explicit live-delivery-disabled guidance. Neither provider credentials nor passage of time automatically dispatch these plans. Calendar explains plans are unpublished.

## Evidence limits and follow-up

No live provider calls, charged generation, social publication or analytics import occurred. Provider broker tests previously passed 26/26 using fake HTTP handlers; that evidence does not verify credentials or live integrations. This recheck adds four executed frontend test results, but no browser-run claim. Owner browser evidence is in frontend `evidence/features-qa.json`, `pipeline-qa.json` and `navigation-qa.json`. These checks used configured-off providers and no live posts.
