# Ocharo Marketing: next delivery and metrics increment

*Last updated: 2026-09-27*

*Proposed 2026-09-27. Design only. Current application saves plans, not live delivery. No credentials, accounts, publication, paid reads or provider execution have been verified.*

## Increment boundary

Keep the existing .NET 10/Vue/SQLite application, `MarketingService`, conditional repository updates, typed HTTP brokers and loopback pilot. Add one publication worker and persisted publication operations; do not introduce a scheduler service, message bus, new repository, Brand Workspace dependency or SDK changes.

Deliver the first actual pathway for **Telegram text/single photo**, behind explicit configuration and operator queueing. The same durable contract then supports **Instagram single photo** after account authorization and public JPEG hosting exist. Add **X observed public metrics** using owned receipt IDs and opt-in reads; X publication can reuse its current broker after identity and upload-expiry handling are completed. Keep WhatsApp recipient messaging separate from social-feed publication.

Existing reusable source seams:

- `OcharoMarketing.Application/Providers/Brokers/ISocialPublishingBroker.cs` already maps neutral content/media through `PublishAsync(SocialPublishRequest, CancellationToken)` and returns `SocialPublishResult`/`ProviderReceipt`.
- `SocialPublishRequest.SaveCheckpointAsync` and `ResumeReceipt` already support Instagram `container_created` and X `media_uploaded` stages.
- `Persistence/Marketing/Repositories/MarketingRepository` demonstrates conditional SQLite claims, claim-token checks, immutable revisions and interrupted external work becoming `unknown`.
- `MarketingOptions.EnableLivePublishing` already exists and defaults false; no current worker dispatches schedules.

## 1. Destination and account setup

The operator must select an authorized destination, not type an arbitrary identifier into a publication request. Store a safe `ConnectionIdentity` containing connection ID, provider, authorized account ID, destination ID, display name, credential **reference**, credential/configuration revision, login mode, API version, granted capabilities, verification time and safe verification result. Never return token, refresh token, app secret, token fingerprint or OAuth authorization code through `/workspace`.

Initially bind one configured account/destination per provider using server options and environment credentials. Add safe destination metadata to the existing connection response; separate `configured`, `verified` and execution-enabled states. A completed local build with no credentials still reports unconfigured. Verification is an explicit operator command, and uses read-only provider endpoints rather than sending a test post.

| Channel | Setup/verification before queueing | Execution guard |
|---|---|---|
| Telegram | BotFather token; `getMe`, destination `getChat`, bot's `getChatMember`; confirm channel identity and administrator posting permission. Resolve a username to its stable numeric chat ID. | Freeze resolved chat ID plus bot ID; verify returned Message chat ID matches the frozen destination. Arbitrary chat IDs are rejected. [Bot API](https://core.telegram.org/bots/api#getchatmember). |
| Instagram | Professional account and authorized Meta app/token. Distinguish Instagram Login from Facebook Login, linked Page/token flow where applicable, Graph host and pinned version. Existing account option must gain an account-ID binding. | Queue destination must equal the verified authorized IG account. Instagram Login publish scopes include `instagram_business_basic` and `instagram_business_content_publish`; its setup does not require the Facebook Page link. [Official Meta collection](https://www.postman.com/meta/instagram/folder/6raa77c/instagram-api-with-instagram-login), [Facebook Login](https://www.postman.com/meta/instagram/folder/u4g5a2a/instagram-api-with-facebook-login). |
| X | Authorized user OAuth; verify `GET /2/users/me` matches configured `UserId`. Scopes: `tweet.read`, `tweet.write`, `users.read`, `media.write`; add `offline.access` for renewal. | Destination must equal verified user ID, and uploaded media must belong to that account. [OAuth](https://docs.x.com/fundamentals/authentication/oauth-2-0/authorization-code). |
| WhatsApp | Business phone/token, explicit recipient permission, and approved template or currently valid customer-service window. | Freeze sender phone and recipient permission evidence; revalidate window/template at due time. A future scheduled message cannot reuse a stale boolean asserting a current window. `accepted` is not `delivered` or a feed post. [Messaging policy](https://whatsappbusiness.com/policy/). |

OAuth UI is a later setup step, not a prerequisite for building the safe queue seam with fake credentials. Use server-owned authorization-code exchange, a single-use expiring state bound to the operator/session, exact registered callbacks, PKCE for X, and protected server storage for refresh tokens. The Vue app receives only connection status. Refresh single-flight, persist rotated refresh tokens atomically, and recheck returned account identity. An account change invalidates queued authorization; same-account token renewal may advance credentials without changing the frozen destination. Meta scope/callback/token renewal must follow the selected login mode rather than sharing X assumptions.

The local pilot's loopback restriction remains intact. Remote OAuth/webhook deployment requires authenticated operator sessions and registered HTTPS callbacks before exposing these routes. No current public callback/account setup is claimed.

## 2. Explicit queue API and operator action

Keep `POST /drafts/{id}/schedule` as **plan only**. New proposed `POST /schedules/{id}/queue` takes:

```json
{
  "expectedDraftRevision": 3,
  "connectionId": "telegram-main",
  "expectedConnectionRevision": 2,
  "operationId": "client-generated-id-for-this-queue-action",
  "reviewedDeliveryMediaSha256": "sha256-if-a-reviewed-derivative-is-required"
}
```

Return the persisted schedule/operation state, never a publication success. In one SQLite transaction, require: plan remains `planned`; global live publishing and provider execution are enabled; connection identity/capability is verified; schedule channel matches connection; the current draft revision equals the plan revision and is approved; selected asset hashes remain valid; due time is future; channel caption/media requirements pass; any transformed delivery artifact has been explicitly reviewed. Freeze the approved caption, selected bytes/hashes, recipe, account/destination, configuration revision and queue authorization time.

The initial queue endpoint deliberately rejects an older plan after the draft was edited; the operator reviews and creates a new plan. Do not treat `DraftRevisions` JSON as historical approval evidence: current approval lives in `Drafts` and snapshots were recorded before approval. A later historical-revision queue would require explicit immutable per-revision approval evidence and an original-revision viewer.

Use a unique operation per schedule and idempotent **local command** identity: replaying the same operation ID returns the recorded state; a second ID cannot create another operation for an already queued plan. This protects browser retries without implying provider idempotency. No automatic requeue on credentials appearing or application restart.

Queue confirmation shows exact account/channel, due time in Tashkent, frozen title/revision, caption and delivery artwork. Label the action as authorizing scheduled delivery. Existing plans and manual exports remain useful when execution is disabled. Turning publishing off holds unstarted work as plans requiring a fresh explicit queue action when enabled again; it must not silently resume old authorization.

## 3. Delivery artwork and public Instagram JPEG

Use the already selected authoritative PNG; never rerun generation or extract a card layout from the PDF at send time. Create a deterministic JPEG derivative locally from that PNG, preserving the 1080×1350 layout and compositing transparency against the approved card background. Proposed application defaults: RGB JPEG quality 95, bounded encoded size, stable derivative ID from source hash plus transform version. These are application choices, not newly verified Meta limits.

Because JPEG changes bytes, include source PNG hash, derivative hash and transform version in the delivery snapshot. Display that derivative before queueing and require its reviewed hash. Keep the original export/atlas PNG unchanged. If a derivative exceeds the configured bound or cannot preserve the reviewed dimensions, block queueing rather than shrinking fonts, cropping or recomposing the card silently.

Instagram's broker currently requires `image/jpeg` and a publicly reachable HTTPS URL. A private `/assets` or localhost URL cannot supply this. Add a small optional `IPublicMediaStore` seam under Application/Marketing and an Infrastructure adapter for the actually configured existing hosting/storage mechanism. Keep it unconfigured until a real public origin/storage credential exists; do not broaden the private asset route or expose the loopback API.

At due time, upload only the reviewed immutable JPEG, persist the public object key/URL/expiry checkpoint, then create the Instagram container. URL must remain fetchable through container processing, with no browser cookie or authorization header. Bind publication to an allowlisted configured HTTPS origin; include expiry in the receipt and ensure it covers the due-time processing window. No arbitrary operator URLs/SSRF probes. Validate provider photo constraints against the pinned Graph version before enabling Instagram execution: current Meta developer publishing pages were inaccessible during research, so this design does not invent verified size/ratio limits. The official collection confirms container → readiness → publish. [Publication examples](https://www.postman.com/meta/instagram/documentation/6yqw8pt/instagram-api?entity=request-23987686-6fa9ed1d-3310-4844-ad25-f0001ab66f11).

## 4. Persisted operation and due claims

Add a `PublicationOperationRecord`, keeping scalar fields queryable and a neutral JSON snapshot for payload details:

```text
Id; ScheduleId (unique); Provider; ConnectionId; ConnectionRevision;
Status; Phase; DueAt; NextCheckAt; ClaimToken; LeaseExpiresAt; Attempts;
SnapshotJson; ReceiptJson; AuthorizedAt; DispatchStartedAt;
AcceptedAt; PublishedAt; SafeCode; SafeMessage; UpdatedAt
```

`SnapshotJson` contains approved revision/title/caption, selected asset IDs/hashes, delivery derivative/hash/transform version, authorized account/destination, Graph/login mode, optional messaging permission/window/template evidence. It excludes all secrets. `ReceiptJson` carries existing `ProviderReceipt` stages plus storage/expiry evidence. Mirror schedule status and receipt summary in the same transaction.

Use one `PublicationWorker` in the existing host, separate from `GenerationWorker`; publication lease recovery has different rules from deterministic card jobs. Atomically claim only due authorized operations with supported stages and no active claim. CAS the expected status/phase and set a fresh token/lease. Every checkpoint and final state write requires the same token. A stale worker cannot change a reclaimed operation; callbacks fail before the next provider effect if ownership was lost. Extend/renew lease before bounded provider stages; safe polling retains a `NextCheckAt` rather than spinning.

Apply global/provider enablement and identity guards at queue time **and immediately before each external mutation**, not merely when options are loaded. Caption/media validation must be for the supported single-photo pathway; ten draft images do not imply carousel support. Never truncate a caption automatically.

Schema changes must upgrade existing SQLite databases explicitly and preserve plans/snapshots; `EnsureCreated` alone cannot add tables to an existing database. Use the venture's controlled initialization/migration owner, not a destructive database reset.

## 5. Checkpoints, outcomes and recovery

Persist a dispatch marker **before** the first possible external side effect. Current Telegram, WhatsApp and X text-only brokers do not save a pre-send checkpoint, so the application must provide a durable outer dispatch marker or extend these brokers before wiring the worker. A process can crash after the HTTP request and before its result is returned.

| Provider/stage | Persisted evidence and next step |
|---|---|
| Telegram | Before send: local operation dispatch marker. Success: returned message ID plus numeric chat ID; verify chat identity before recording `published`. No general safe status lookup/repost contract is assumed after a missing receipt. |
| Instagram | Public JPEG object; `container_created` persisted; safe status GET; `publication_started` persisted before `media_publish`; final media ID plus container ID. Resume only a known unexpired container stage. `PUBLISHED` without media ID becomes `unknown`, not a second publish. |
| X | Before upload/send marker; `media_uploaded` plus expiry; `publication_started`; final post ID. Current broker does not retain upload expiry: add `expires_after_secs`/absolute expiry before resumed uploads can be used. The current upload response also permits partial errors; inspect them rather than accepting arbitrary `data.id`. [Upload response](https://docs.x.com/x-api/media/upload-media). |
| WhatsApp | Before send marker; returned `wamid` records `accepted`. Delivery/read/failure require deduplicated, authenticated status webhook evidence. Do not map acceptance to `published`; add explicit messaging states before enabling this pathway. [Status notifications](https://www.postman.com/meta/whatsapp-business-platform/request/rgtfq23/message-status-update-notifications). |

Outcome rules:

- `Succeeded` plus final IG/X/Telegram receipt → `published`; no engagement inferred. WhatsApp → `accepted`, with delivery unavailable.
- `Pending` plus a known container → processing state with bounded safe-GET polling. No new container and no blind generic `PublishAsync` replay after final dispatch.
- `Rejected`/`Unsupported` → explicit failure/setup reason. Corrections require a new operator action; no invisible credential-switch retry.
- `Ambiguous`, malformed accepted response, interrupted side effect, or receipt persistence failure → `unknown`. Preserve all checkpoints and prohibit automatic repost.
- Expired claim with **no dispatch marker** can recover safely; with final-send marker it becomes unknown. A saved intermediate container/upload can resume only its documented safe stage and original identity. Do not reuse the existing generation policy unchanged.

For unknown outcomes, expose receipt/checkpoint details, provider history link and a read-only reconciliation action. Candidate timeline matches are evidence for review, not proof from caption equality. Record an operator-confirmed owned receipt only after account/identity validation; otherwise remain unknown. There is no promise of exactly-once delivery across these providers. Local cancellation of running/unknown/published work cannot unsend it.

## 6. Observed metrics without synthetic zero

Add a small provider-neutral read seam; no business entities enter brokers:

```csharp
public interface ISocialMetricsBroker
{
    string Provider { get; }
    Task<SocialMetricsResult> ReadAsync(SocialMetricsRequest request,
        CancellationToken cancellationToken);
}
// Request: authorized account, final receipt, requested supported metric names.
// Observation: name, decimal? value, availability/reason, observedAt,
//              provider/receipt identity, period/scope when supplied.
```

Cache observations in SQLite per connection, receipt, metric and observation time. Keep scope explicit: post versus account, cumulative versus period, organic versus promoted. `/workspace` and a proposed `GET /schedules/{id}/metrics` return cached observations only; loading the UI must not call external providers or consume paid reads. A proposed explicit `POST /schedules/{id}/metrics/refresh` performs bounded provider **GETs** when `EnableMetricReads=false` has been deliberately enabled. X API reads may cost credits; label that capability and enforce configured read limits separately from publish enablement.

| Channel | First supported observations | Null availability |
|---|---|---|
| X | `GET /2/tweets/{id}?tweet.fields=author_id,public_metrics`; require returned ID/author match owned receipt/account. Parse only present nonnegative counters, including `like_count`, `reply_count`, `retweet_count`, `quote_count`, `bookmark_count`, `impression_count`. | Missing/partial fields → `not_returned`; permission → `permission_missing`; missing receipt → `not_published`; invalid response → `read_failed`. User-context private/organic/promoted fields are optional and limited to eligible own posts within 30 days. [Metrics](https://docs.x.com/x-api/fundamentals/metrics). |
| Instagram | Proposed media-insights reader uses the final owned media ID and the selected Graph host/version; exact current metric allowlist, scopes and response mapping must be verified for that version/login mode first. | Until that verification, `capability_unverified`; do not ship guessed `engagement`/deprecated counters as live results. Permission/unsupported/absent fields remain distinct. |
| Telegram | General channel reach/views are not provided by the current Bot API broker. Member counts or reactions must remain different metric names if later collected. | `not_supported` for reach/views; never convert a publication receipt into a view count. |
| WhatsApp | Sent/delivered/read/failed are message-status observations from verified webhook events, separate from engagement analytics. | `delivery_unverified` until evidence arrives; not a social-reach metric. |

Numeric zero appears only when a provider returned an explicit zero. For unsuccessful refresh, preserve prior observed values/timestamps and attach latest-attempt availability/error; do not overwrite observed data with zero or relabel its stale timestamp as new observation. Retry only safe GETs with bounded backoff/Retry-After and rate/budget controls. Deduplicate observations; do not sum duplicate polls or mix impressions with unique reach.

## 7. Small acceptance batches

1. **Queue/persistence:** disabled flags reject queue; credential appearance never activates plans; fixed account/destination; approved current revision and hashes required; duplicate local queue command creates one operation; historical plan requires re-review; two workers claim once; edits after queue cannot mutate the frozen operation.
2. **Worker/Telegram:** fake handlers verify exact selected bytes/caption and destination; pre-send checkpoint persisted; wrong returned chat/account blocked; shutdown after send becomes unknown; revoked identity holds before send; no automatic ambiguous retry. No live posts in tests.
3. **Instagram delivery media:** deterministic reviewed JPEG/hash; no localhost/private URL; unconfigured public storage blocks; saved container survives restart; readiness GET resumes without recreating; final dispatch checkpoint precedes publish; expired/PUBLISHED/malformed/timeout paths truthful. Verify pinned Meta photo constraints before real enablement.
4. **Metrics:** fake X GET for owned receipt; explicit zero versus missing/partial/errors; stale observed values preserved; no request on workspace load; paid-read flag/budget enforced; unavailable IG/Telegram states honest; messaging acceptance excluded from published results.
5. **Operator pilot:** account owner provides credentials, verifies identity, enables the relevant flags, reviews the concrete media/caption/destination and explicitly queues one test plan. A real provider receipt and then an actual observation are separate acceptance events. Until then the build is implemented/fake-tested only.

No code in shared lanes is changed by this design. The first implementation batch can build these states and fake HTTP tests without accounts; actual execution remains disabled until the operator completes the concrete setup and queue action.
