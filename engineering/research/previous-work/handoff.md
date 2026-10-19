# Build handoff

*Last updated: 2026-10-19*

## Consolidation hold — 2026-09-29

The user requested a saved handoff and no further editing until consolidation resumes.
The current checkout is `/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing`; branch `main`, HEAD `f4ef34d407555171e6e7a145afb45fed8097d81f`.
The [central chat handoff](../../../../../docs/handoffs/ocharo-marketing-01a0a058-f53a-7a10-94ad-51c642845c30.md) owns the consolidation snapshot.

- Existing dirty relocation/reference edits and the [next-build analysis](../next-build-analysis.md) remain saved in place.
- That analysis is a proposal; no new implementation scope or product acceptance was inferred.
- Two temporary [atlas-feedback screenshots](../references/atlas-layout-feedback/atlas-layout-feedback.md) are now preserved with hashes.
- The build heartbeat remains `PAUSED`; every child lane is finished.
- No listeners were observed on 8242, 8276 or 8277. Sessions 28593/81804 below are historical, not active runtime claims.
- September 29 migration records show successful backend/frontend builds; application tests were not rerun for relocation or this handoff.
- The earlier 181-test prose needs reconciliation: its `171 + 7 + 2` breakdown equals 180. Retained TRX proves the earlier 171-test run.
- No code, data, credentials, Git index, commit, merge, remote or publication was changed by this handoff.

Sections below retain earlier implementation and review evidence. Their runtime availability, test counts and dated provider state
must not be treated as current observations; use the central handoff and recheck after consolidation resumes.

## Prior pilot scope

Ocharo Marketing is an independent temporary venture for Maftuna, outside Brand Workspace. The local pilot's planned implementation is complete and verified; developer/Maftuna review and selected external setup remain. The build heartbeat `continue-ocharo-marketing-build` is **PAUSED**, confirmed through the automation tool and saved state. Do not resume an unchanged build loop or duplicate finished lanes. See [review guide](review-guide.md), [exact remaining actions](external-setup.md), [contract](../../architecture/marketing-contract.md) and [r0.1](rough-track/r0.1/r0.1.md).

## Platform integration increment — 2026-09-28

- **Scope:** posts creation and social-media automation across the new Ocharo platforms — catalogue sync, bulk template drafts, weekly posting slots (plans only), new-product rule, blog-story posts, unattended mode (off by default; sync every N hours, plan N days ahead, never queues delivery); Avtomatika page.
- **Backend:** 181 tests pass (171 earlier + 7 automation + 2 opt-in live contracts that no-op without their variables). Opt-in live contracts (`OCHARO_CATALOGUE_URL`, `OCHARO_BLOG_URL`) passed against running Catalogue (29 products, 58 images, idempotent re-sync) and Blog (30 stories, announcement draft).
- **Frontend:** 33 tests, typecheck including 22 components, lint and format pass; production build deployed to the local host.
- **Browser (isolated QA data dir, real runtime untouched):** Avtomatika page at 1440 px and 390 px (no overflow); bulk draft created with the Uzbek template; catalogue-offline error shown in Uzbek.
- **Repository:** `.gitattributes` routes images/PDF/ZIP/fonts through Git LFS before the first commit; the built SPA in `wwwroot/` is ignored.
- **Not done:** no live provider calls; slots never queue delivery (a standing auto-queue consent is an owner decision).

## Latest verified increment

- **Backend:** 171 tests passed, zero failures/skips. Coverage includes real SQLite migrations/restart behavior, frozen publication snapshots, atomic claims, cancellation before dispatch, revoked authorization, interrupted delivery, verified account matching, bounded metric reads/history, known-ID X receipt reconciliation, source-image review and malformed provider responses. Provider HTTP calls use fakes; card PNG/PDF tests use real local Chromium.
- **Frontend:** 31 tests, typecheck including 21 Vue components, lint, formatting and production build passed. Explicit confirmation binds the displayed post and destination; changed state invalidates consent. Source-analysis review preserves newer edits and offers deliberate reuse in the composer. Cached metrics distinguish observed zero, missing counters and failed reads.
- **Browser:** final isolated enabled-flow checks passed at 1440px and 390px: consent, changed-state invalidation, re-review, queue/cancel, cached-only results navigation, explicit failed refresh retaining zero/history, delayed review and reviewed-text acceptance. Separate actual disconnected API checks passed both widths: verification, queueing and image analysis remain disabled; readiness refresh is GET-only. Both runs had zero browser errors/overflow. No durable QA publication records or provider calls were created.
- **Migration:** `20260927000200_DeliveryPipeline` applied after the original `202609270001_InitialMarketing`. A private SQLite backup was made first. Existing counts match before/after: 3 products, 13 assets, 2 drafts, 5 draft revisions, 3 schedules and 9 generation jobs. New delivery/observation/analysis tables remain empty in the review runtime.
- **Packaging:** final Node 24.11.0 combined build and fresh local publish passed. All 27 frontend files match the build, package and bytes served by the running API; gzip companions match. Three seed PNGs are included, and private runtime data is excluded.

Authoritative current evidence:

- [Delivery, migration, packaging and runtime evidence](../../../output/verification/delivery-increment.json).
- [171-test backend result](../../../output/verification/delivery-backend-tests.trx).
- [Isolated enabled UI workflows](../../codebase/ocharo-marketing.frontend-services/evidence/delivery-simulated-ui-qa.json).
- [Actual disconnected UI workflows](../../codebase/ocharo-marketing.frontend-services/evidence/delivery-disconnected-ui-qa.json).

Simulated screenshots contain explicitly fake accounts and counters; they do not prove a real connection, publication or engagement. Older `runtime-readiness.json` and `publish-verification.json` describe the first increment; use the delivery record above for current runtime/package evidence.

## Runtime ownership

Frontend watcher session **28593** serves https://localhost:8242/. API session **81804** serves HTTPS **8276** / HTTP **8277** and returned healthy after migration. It replaced the older session 5749. All final static-file parity checks used the same running API 81804. Publishing, account verification and metric reads are explicitly false; all six providers report not-configured, and publication operation count is zero. Source-analysis GET returns 200 with empty saved history.

Inspect existing processes/readiness before starting another instance. Session IDs are evidence, not a guarantee that a later chat can attach. Keep previews running for review. Node 24.11.0 is `/Users/max/.nvm/versions/node/v24.11.0/bin/node`. Provider option changes require the controlled restart/reverification described in [external setup](external-setup.md); hot configuration drift holds execution.

## Implemented boundaries

Manual scheduling does not authorize external publication. Telegram/X require a verified server-owned destination, enabled execution, current approved revision and a separate queue confirmation. The frozen operation retains copy, artwork hashes and identity. Disabling execution or failing/changing account verification revokes unstarted authorization; reenabling does not revive it. Unknown delivery never automatically reposts.

X observations and known-final-ID receipt reconciliation use explicit separately enabled reads, durable cooldown/budget reservations and account ownership checks. Results navigation reads local cache. Missing IDs and Telegram unknown outcomes require manual provider-history inspection. Instagram remains unavailable for automatic queueing until reviewed JPEG/public-hosting delivery exists; WhatsApp needs its separate consent/template/window operator workflow.

Source-image analysis preserves original source hashes and suggestions, then stores operator-reviewed wording separately. A stable operation identity prevents duplicate provider requests on retry; interrupted pending work is not blindly repeated. Reviewing analysis neither changes catalogue facts nor approves a post. OpenAI/Gemini generation remains configured explicitly and unexercised against paid providers.

## Review and setup seam

The remaining actions are concrete in [external setup](external-setup.md): review the card recipes and Uzbek operator flow; choose the local/hosted operator model; select/configure intended provider accounts; separately authorize live canaries; define Instagram public media and WhatsApp customer-message workflows; review SDK extraction and its publication/consumer acceptance seam. Missing credentials no longer block an unfinished planned local feature in this pilot. Keep r0.1 In-Progress until developer review; technical verification is not product acceptance.

All implementation agents have finished their lanes. Brand Workspace and SDK repositories were not edited. Reusable contracts/components remain separately bounded inside this venture. No commits, pushes, remote repositories, deployments or SDK package publications were made by this continuation.

## Earlier card/export evidence

The first increment verified three existing front cutouts across six 1080×1350 bone/charcoal cards, a three-page atlas PDF, stable exact-image ZIP exports, revision-safe composition editing and manual plan cancellation. Those artifacts remain under `output/verification/`, with the earlier seven-route desktop/mobile browser evidence under frontend `evidence/`. Existing local QA fixtures are review examples, not real publication history.

No paid AI request, live analytics read, social post or customer message occurred during either increment. Real product-fidelity/copy review and live provider acceptance remain unverified.
