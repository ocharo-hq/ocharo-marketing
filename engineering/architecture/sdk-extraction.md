# SDK extraction inventory

*Last updated: 2026-10-18*

## Boundary

The user requested reusable wow-two components and brokers, extracted after the app proves them. Current work keeps these boundaries product-local and separate. Nothing here claims a published SDK package. Rough-track completion/review precedes extraction, and a full capability vector inventory follows the first consumer slice.

| Candidate | Reusable scope | Product-owned scope | Extraction evidence |
|---|---|---|---|
| Image generation | Neutral generation/edit/reference models; OpenAI/Gemini mapping; capabilities; safe failures; operation receipts | Product-fidelity prompts, approved source selection, budgets and workflow | Fake-transport contracts, reference/edit cases, ambiguous outcomes, actual configured acceptance later |
| Content assistance | Text/vision inputs, structured suggestions, provider refusal/output handling | Ocharo copy tone, product claims, language review | Bounded text/image parsing and caller review semantics |
| Social publication | Channel adapters, media upload, checkpoints, receipt models, credential-mode distinctions | Recipient permission, content approval, selected destination, scheduling decisions | Stage recovery, no duplicate resend, OAuth/token lifecycle and provider acceptance |
| Identity and observations | Provider identity verification, account-bound receipt reads, normalized counter availability and timestamps | Chosen accounts, read budgets, operator approval, Ocharo dashboard labels | Exact identity checks, partial counters, real zero versus missing data, fake read transports |
| Reviewable source assistance | Source hash evidence, explicit request identity, separate suggestion and reviewed text, stale-review rejection | Catalogue facts, accepted business wording, deciding how reviewed copy enters a post | SQLite intent persistence, replay refusal, source-change and delayed-review tests |
| Document rendering | Offline HTML → PNG/PDF, font/image readiness, dimension/overflow checks | T01/T02 design, copy/asset eligibility, brand palette | Same preview/export, exact pixel size, portable bundled resources |
| Durable work | Claims, leases, attempts, deterministic retry, completion deduplication | Render versus charged-generation versus publication policy | Restart/race evidence and persisted unknown outcomes |
| UI primitives | Job status, connection state, accessible agenda, image framing where SDK lacks them | Maftuna's screens, source fields, presets, brand treatment | Second neutral use or explicit public API design; existing SDK reused first |

## Complete-vector pass

Inventory generation/edit/multiple-reference/continuation/cancellation/usage models; provider capability discovery and versioning; credential setup/refresh/revocation; media formats and upload stages; publication reconciliation and webhooks; metric dimensions/time ranges/freshness; rendering resource isolation and portability; queue concurrency and storage adapters. Mark unsupported capabilities explicitly instead of implying broad parity.

SDK extraction must include public exports, documentation, contract tests, dependency boundaries, verified package artifacts, and consuming-app adoption. Publishing remains a human action. Ocharo facts, templates, and workflow decisions stay in this venture.

## Published UI compatibility gaps found by this consumer

The frontend currently uses thin local wrappers around published `@wow-two-beta/fe` 0.0.22 components. Button click and picker/file-input native attributes need declaration compatibility without replacing their runtime behavior. Record these as SDK declaration fixes during extraction; do not mistake a local wrapper for a published SDK upgrade. Consumer acceptance must include strict Vue typechecking, keyboard activation and disabled-state behavior.

## Added consumer evidence

The delivery increment now exercises frozen publication snapshots, destination verification, cancellation before dispatch, interrupted-outcome holds, explicit known-ID receipt reads, durable read budgets and immutable metric history. `useSnapshotConfirmation` supplies a local reusable UI boundary for consent invalidation and stable command identity. Source-image review keeps provider suggestions separate from human wording and preserves later editor changes. These remain product-local candidates; no extraction, version bump or SDK publication occurred.
