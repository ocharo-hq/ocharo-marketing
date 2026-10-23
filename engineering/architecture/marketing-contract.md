# Marketing implementation contract

*Last updated: 2026-10-24*

## Scope

Ocharo Marketing is an independent, temporary venture built for Maftuna. Brand Workspace is moving and is neither an implementation dependency nor an edit target. Use .NET 10, Vue 3, strict TypeScript, Tailwind 4, and local SQLite for the first single-operator pilot. All provider credentials remain server-side configuration. No real publishing or paid generation is exercised during autonomous build.

## Lane ownership

| Lane | Exclusive writes |
|---|---|
| Root | Product/engineering docs, API contract, Api/SeedData, Application/Cards, Infrastructure/Cards, Tests/Cards, Scanning folders in each backend layer |
| Foundation agent | Remaining backend, project/solution/DI/package files, deployment/runtime scripts |
| Provider agent | Application/Providers, Infrastructure/Providers, Tests/Providers |
| Frontend agent | Entire frontend workspace, including package/config/test files |

Coordinate shared contract changes before editing another lane. No SDK repository is modified during rough build. Keep reusable components in separate folders and product-independent contracts; SDK extraction follows the working product seam.

## HTTP contract

Base: `/api/marketing`. camelCase JSON; dates are UTC ISO strings. Product/asset/preset IDs are strings; record IDs may be GUID strings. A failed command returns ProblemDetails with useful `detail` and a conflict/validation status; no successful fake response.

`GET /workspace` returns:

```ts
interface Workspace {
  brand: { name: string; timezone: string; handle: string | null };
  products: Product[];
  assets: Asset[];
  presets: Preset[];
  drafts: Draft[];
  schedules: Schedule[];
  connections: Connection[];
  metrics: Metric[];
  generationJobs: GenerationJob[];
  publicationOperations: PublicationOperation[];
  deliverySettings: { publishingEnabled: boolean; metricReadsEnabled: boolean; accountVerificationEnabled: boolean };
}
interface Product {
  id: string; name: string; category: string; material: string;
  description: string; heroAssetId: string; assetIds: string[];
  sizes: string[]; price: number | null; currency: string;
  reviewStatus: string; sourceRevision: string; issues: string[];
}
interface Asset {
  id: string; fileName: string; contentType: string; byteLength: number;
  sha256: string; provenance: string; url: string; role: string;
}
interface Preset {
  id: string; name: string; family: string; surface: string;
  description: string; requiresFabric: boolean; width: number; height: number;
}
interface Draft {
  id: string; title: string; productId: string; presetId: string;
  locale: string; caption: string; channels: string[]; mediaIds: string[];
  revision: number; approvedRevision: number | null;
  status: string; createdAt: string; updatedAt: string;
  composition: CardRecipe | null;
}
interface CardRecipe {
  title: string; subtitle: string; fabricAssetId: string | null;
  scale: number; offsetX: number; offsetY: number;
  boundMediaIds?: string[]; // Only generated outputs explicitly tied to this recipe.
}
interface Schedule {
  id: string; draftId: string; draftRevision: number;
  channel: string; scheduledAt: string; status: string;
  message: string | null; externalId: string | null;
  draftTitle: string | null; // Captured from the scheduled immutable revision.
}
interface Connection {
  id: string; name: string; kind: 'social' | 'image' | 'text';
  status: string; capabilities: string[]; setupHint: string;
  documentationUrl: string;
  connectionRevision: number; verificationStatus: string;
  executionEnabled: boolean; metricReadsEnabled: boolean; canVerify: boolean;
  accountId: string | null; destinationId: string | null; displayName: string | null;
  verifiedAt: string | null; safeMessage: string | null;
}
interface Metric {
  channel: string; name: string; value: number | null;
  status: string; observedAt: string | null; explanation: string;
}
interface GenerationJob {
  id: string; kind: string; provider: string; prompt: string;
  productId: string | null; status: string; createdAt: string;
  outputAssetIds: string[]; message: string | null;
}
```

| Endpoint | Request and response |
|---|---|
| `POST /drafts` | `{ title, productId, presetId, locale, caption, channels, mediaIds?, composition? }` → Draft |
| `PUT /drafts/{id}` | Same fields + `expectedRevision` → Draft; stale write returns 409 |
| `POST /drafts/{id}/approve` | `{ expectedRevision }` → Draft; approve displayed copy and selected output, not whole catalogue |
| `POST /drafts/{id}/schedule` | `{ expectedRevision, channel, scheduledAt }` → Schedule; approved revision required |
| `POST /schedules/{id}/cancel` | Empty body → Schedule; no unsend implication |
| `GET /drafts/{id}/export` | Approved current revision only → immutable ZIP with caption, manifest, selected image bytes |
| `GET /assets/{id}` | Exact original bytes; local private route |
| `POST /assets` | Multipart `file`, `provenance`, optional `role` → Asset |
| `POST /scan` | `{ text }` → `{ summary, suggestions: string[], issues: string[] }`; suggestions are never automatically accepted facts |
| `POST /assist/text` | `{ prompt, operation, locale }` → `{ text, provider, model }`; unavailable config is explicit |
| `POST /generation-jobs` | `{ provider, prompt, productId?, referenceAssetIds?, model? }` → GenerationJob |
| `POST /cards/preview` | Card request below → `{ html, issues: string[] }` |
| `POST /cards/render` | Same card request → GenerationJob with `kind: 'card'` |
| `POST /atlas/export` | `{ assetIds: string[] }` → PDF containing exact selected card images; journal metadata outside cards |

Card request: `{ productId, presetId, title, subtitle?, locale, fabricAssetId?, scale?, offsetX?, offsetY? }`. Browser preview uses the backend-produced self-contained HTML in a sandboxed iframe. Same HTML produces authoritative 1080×1350 PNGs. Product names remain draft suggestions but may render review proofs; missing required fabric blocks T01 until supplied. T02 works with existing front cutouts. Presets: `t02-bone`, `t02-charcoal`, `t01-fabric-right`. Persist the optional `composition` in each draft revision; reopening must restore title, subtitle, fabric selection and framing. `scale` is 0.75–1.05; offsets are normalized canvas fractions (X ±0.12, Y ±0.06), not pixels. Older drafts with null composition use defaults without changing their selected output. Atlas exports accept 1–24 rendered PNG cards.

## States and durable semantics

- Draft statuses: `draft`, `approved`. Editing changes revision and clears current approval.
- Channel IDs: `instagram`, `telegram`, `whatsapp`, `x`; image IDs `openai`, `gemini`.
- Connection statuses: `not-configured`, `configured`, `unsupported`; configured is not provider-verified.
- Schedule statuses: `planned`, `queued`, `running`, `published`, `failed`, `unknown`, `cancelled`.
- Disconnected/disabled publishing accepts an approved schedule as `planned`, with explicit reason. It never claims queued delivery.
- Do not automatically activate old planned entries when credentials appear; the operator must requeue explicitly.
- Publishing defaults off. A saved schedule is not provider authorization or publication evidence.
- Jobs: `queued`, `running`, `completed`, `failed`, `unknown`; an unconfigured paid provider rejects submission clearly.
- Keep approved input snapshots immutable. Editing a draft must not silently change an existing schedule or ZIP.
- Interrupted/ambiguous external publishing or charged generation becomes `unknown`, never blindly retried.
- Local deterministic rendering can retry after a lease expires; output completion is idempotent.
- No fake engagement. A null value with `unavailable`, `not-connected`, or `not-supported` differs from observed zero.

## Runtime and extraction

A local run is loopback-only. A deployed host is public without sign-in by the owner's decision of 2026-10-24, until the Ocharo identity server lands; then remote hosting requires authentication, secret persistence and secure callback routing. Validate origin for mutations in both. Ports: API HTTPS 8276, HTTP 8277, Vue HTTPS 8358. Use Node 24.11.0 on this host and pnpm 10.33.2.

Provider and card contracts contain neutral data, not canonical business entities. Persist job state in SQLite, with a single host worker and atomic claims/completion. Backend uses published SDK surfaces where available; local namespaces retain extractable boundaries. Never publish packages, create remote repositories, send messages/posts, or invoke paid providers during verification.

## Delivery and observation increment

Manual planning remains separate from external authorization. These additive routes freeze verified server-configured identities; callers never supply arbitrary social recipients.

| Endpoint | Request and response |
|---|---|
| `POST /connections/{id}/verify` | Empty body → Connection; explicitly enabled read-only verification |
| `POST /schedules/{id}/queue` | `{ expectedDraftRevision, connectionId, expectedConnectionRevision, operationId }` → PublicationOperation |
| `GET /schedules/{id}/publication` | PublicationOperation or null; local read only |
| `POST /schedules/{id}/reconcile` | Empty body → PublicationOperation; explicit bounded read of a known owned final X receipt |
| `GET /schedules/{id}/metrics` | MetricCache; cached observations only |
| `POST /schedules/{id}/metrics/refresh` | MetricCache; explicit provider read, independently enabled |

```ts
interface DeliveryIdentity {
  connectionId: string; provider: string; revision: number;
  accountId: string; destinationId: string; displayName: string;
  capabilities: string[]; verifiedAt: string;
}
interface PublicationOperation {
  id: string; scheduleId: string; provider: string;
  connectionId: string; connectionRevision: number;
  status: string; phase: string; dueAt: string; authorizedAt: string;
  dispatchStartedAt: string | null; updatedAt: string; message: string | null;
  receipt: { provider: string; stage: string; externalId: string;
    relatedIds: Record<string, string> | null } | null;
  snapshot: { draft: Draft; identity: DeliveryIdentity;
    media: { assetId: string; sha256: string; contentType: string; fileName: string }[] };
  canCancel: boolean; canReconcile: boolean; reconciliationMessage: string;
}
interface MetricCache {
  scheduleId: string;
  observations: { id: string; scheduleId: string; connectionId: string;
    provider: string; receiptId: string; name: string; value: number | null;
    availability: string; observedAt: string | null; reason: string | null;
    scope: 'post'; aggregation: 'cumulative' }[];
  history: MetricCache['observations']; // Latest 250 immutable observations.
  historyTruncated: boolean;
  lastAttempt: { attemptedAt: string; status: string; message: string } | null;
}
```

Queueing requires the current approved draft revision, unchanged connection revision, a supported verified destination, and explicit execution flags. The operation stores the entire approved draft plus media hashes and destination identity. Repeated commands reuse their operation identity; a changed command cannot reuse it. Held or revoked operations retain immutable evidence and require a new manual plan for fresh authorization. Cancellation never implies unsending a published post.

The worker claims due operations atomically and persists a dispatch marker before the provider call. Expired or interrupted external dispatch remains unknown; no blind retry claims exactly-once delivery. Global/provider execution switches are rechecked before dispatch/checkpoints. Telegram and X are the initial queue-supported providers. Instagram remains blocked until reviewed JPEG delivery and public media hosting exist; WhatsApp needs its separate consent/template customer-message workflow.

Results read only local cache on navigation. Explicit X refresh requires separate read enablement, the configured account, an owned final receipt, cooldown and request-budget checks. Observed zero is retained; missing counters are null with reasons. Failed refreshes preserve earlier observations and their timestamps. Telegram reach and WhatsApp delivery receipts are not represented as audience engagement.

## Source-image analysis and review

| Endpoint | Request and response |
|---|---|
| `GET /source-analysis` | SourceAnalysis[]; saved local history |
| `POST /source-analysis` | `{ operationId, assetIds, provider: 'openai', locale, prompt? }` → SourceAnalysis |
| `POST /source-analysis/{id}/review` | `{ expectedRevision, text }` → SourceAnalysis; compare-and-swap review |

```ts
interface SourceAnalysis {
  id: string; revision: number;
  status: 'pending' | 'ready' | 'reviewed' | 'unavailable' | 'failed';
  provider: string; model: string | null; locale: string; prompt: string;
  sources: { assetId: string; sha256: string; fileName: string; provenance: string }[];
  suggestedText: string | null; reviewedText: string | null; message: string;
  createdAt: string; updatedAt: string; reviewedAt: string | null;
}
```

Select 1–3 different existing PNG/JPEG/WebP assets totaling at most 12 MiB. The operation ID is a caller-generated GUID reused on transport retry. Input fingerprints prevent reusing it for different instructions/assets. Source bytes must match their stored hashes before assistance and before saving reviewed wording. Persist the intent before contacting the provider; a pending replay cannot silently incur a second request. A disconnected request retains completed/failed status where available, otherwise history explicitly describes an unknown interrupted outcome.

The provider returns visible observations and suggested wording, not canonical product facts. Material composition, pricing, availability, photographed provenance and actual fit require human evidence. Review stores edited text separately from the original suggestion. Copying reviewed text into a draft is a deliberate operator action; reviewing analysis never updates the product catalogue or approves a post. Disabled providers produce explicit unavailable history without generated text.

Source-analysis readiness refresh uses a local GET. The analysis POST always represents an explicit analysis intent; it must never double as a status-only probe.

## Platform integration and automation (2026-09-28)

| Method | Path | Contract |
|---|---|---|
| POST | `/api/marketing/catalogue/sync` | Reads Ocharo Catalogue `/api/public/catalogue?locale=uz`; upserts products (copy per locale, prices, sizes) and hash-verified images; returns `{ received, created, updated, imagesStored, newProductIds, draftIds, warnings }`. 502 when the catalogue is unreachable. |
| GET/PUT | `/api/marketing/automation` | `{ slots[{ id, channel: instagram/telegram/x, days: 1–7, time: HH:mm, enabled }], draftNewCatalogueProducts, defaultPresetId, defaultChannels, defaultLocale, captionTemplates{uz,ru,en}, autoSyncHours (0 = off), autoFillDays (0 = off) }`, stored as workspace setting `automation`. |
| GET | `/api/marketing/automation/status` | Last unattended passes `{ lastSyncAt, lastSync, lastFillAt, lastFillPlanned, lastError, lastErrorAt }` (setting `automation-run`). |
| POST | `/api/marketing/automation/fill` | `{ days }` → plans approved (current-revision) drafts into free slot times per channel through the normal scheduling path; one plan per draft and channel; returns `{ planned[], skipped[] }`. |
| POST | `/api/marketing/automation/caption` | `{ productId, locale }` → `{ caption }` rendered from the saved template (used by the composer's facts button). `{available}` fills from catalogue stock: in-stock sizes, or "made to order". |
| POST | `/api/marketing/automation/drafts` | `{ productIds, locale, presetId, channels, template? }` → one draft per product with the rendered caption and hero image. |
| GET | `/api/marketing/blog/stories` | Ocharo Blog RSS items `{ title, link, excerpt, topic, publishedAt, draftId }`; relative links resolve against `Marketing:BlogBaseUrl`. |
| POST | `/api/marketing/blog/stories/draft` | `{ link, productId, channels, locale, presetId }` → draft announcing the story; remembered in setting `blog-announced`. |

Slots and rules create drafts and plans only. Queueing for external delivery keeps its explicit confirmation, verified destination and frozen snapshot.

## Channel operations and engagement (2026-09-29)

These authenticated local routes reuse the Marketing audience boundary. They never invoke provider APIs.

| Method | Path | Contract |
|---|---|---|
| GET | `/api/marketing/operations` | Channel strategy/cadence, worker health, delivery summary/attention, automation history and manual observations. |
| GET | `/api/marketing/operations/channels` | Persistent eight-channel strategy map; inactive blank defaults. |
| PUT | `/api/marketing/operations/channels/{channel}` | Expected strategy revision plus audience, purpose, formats, pillars, owner, HTTPS profile URL, weekly target and review interval. Stale writes return 409. |
| POST | `/api/marketing/operations/observations` | Caller-generated GUID, channel/post URL, title, optional matching schedule, publication/observation dates, nullable counters and evidence note. Immutable manual-source snapshot; identical replay returns original, changed replay returns 409. |

Existing SQLite Metadata stores revisioned channel records and append-only manual snapshots; no schema migration
is required. Atomic repository comparisons protect concurrent writes. Provider receipts and schedules remain unchanged.
The newest 200 observations are exposed with their full stored count; cadence uses the complete history.
Manual snapshots use channel plus exact post URL identity, never sum cumulative samples, and preserve unknown versus zero.
Reach-based engagement requires known likes, comments, shares and saves with positive reach.

Cadence distinguishes scheduled intentions, manual publication dates and confirmed receipt timestamps.
`confirmedReceiptsThisWeek` includes receipts reconciled this week and must not be presented as publication dates.
Manual and provider totals are separate; linked confirmed schedules are excluded from manual weekly counts.
Background health is process-local evidence from actual worker loops, never fabricated by GET requests.

`CardRecipe.boundMediaIds` is optional for stored-draft compatibility and must be a subset of selected draft media.
Only explicitly bound rendered output is invalidated by composition edits; library photos and independently selected
cards survive edits and reopening. The composer never infers binding from the generic asset role.

See [verified workflow and limits](../research/previous-work/marketing-operations-build.md).
