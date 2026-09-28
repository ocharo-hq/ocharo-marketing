# Ocharo Marketing: provider contracts

*Verified against official documentation: 2026-09-27. Research only; no accounts connected, external API calls executed, or credits spent.*

## Implementation order

Keep Ocharo Marketing independent. Start with local cards and approved exports, then generation, Telegram publication, Instagram publication, and optional X/WhatsApp. Adapters belong to this venture first; extract provider-neutral broker contracts after real consumer boundaries become clear. The existing wow-two broker convention separates a capability interface from provider-specific implementations.

The application owns approval, budgets, durable jobs, scheduling, connection selection, and product facts. Brokers own request mapping and provider receipts. Generation never proves product accuracy; provider acceptance never proves delivery, visibility, or engagement.

## Connection and capability matrix

| Provider | Prerequisites | Initial broker capability | Deferred or unavailable |
|---|---|---|---|
| OpenAI | Project API key, billing; image access may require organization verification | Image generation/editing; Responses text/vision | Live credentials and account access unverified |
| Gemini | Google project/API key; applicable image models require paid access | Nano Banana generation with image references | Per-model background execution requires verification |
| Telegram | BotFather token; destination channel admin posting permission | Text or one photo; receipts identify channel/message | General channel reach/view analytics unavailable through this broker |
| Instagram | Professional account; authorized Meta app/token | Publicly reachable image → container → status → publish | Exact current insights scopes/metrics gated; consumer accounts unsupported |
| X | Developer app, applicable credits; user OAuth | Upload image → create post; receipt identifies post | Account-bound public metrics and known-ID reads implemented with fakes; video/chunked upload deferred |
| WhatsApp | Business account, registered phone, access token, opted-in recipient | Approved template, or allowed customer-service text/image | Generic Status/feed publishing unsupported by the researched Cloud API contract |

Instagram Login supports professional businesses/creators without the Facebook Page prerequisite. Facebook Login additionally requires a linked Page. The official Meta collection identifies Stories publication as Business-only. [Instagram Login](https://www.postman.com/meta/instagram/folder/1z5vxzu/instagram-api-with-instagram-login), [Facebook Login](https://www.postman.com/meta/instagram/folder/9cgqucg/instagram-api-with-facebook-login).

## OpenAI

Current image documentation recommends configurable `gpt-image-2.5-flare` and `gpt-image-2.5-sunburst`. Both generation and edits return encoded image data. Transparent output uses PNG/WebP. Image preparation and card rendering remain separate: final `1080×1350` card dimensions must be rendered locally, rather than imposed on an image model's allowed size grid. [Image guide](https://developers.openai.com/api/docs/guides/image-generation).

```http
POST https://api.openai.com/v1/images/generations
Authorization: Bearer <server-side key>
Content-Type: application/json

{"model":"gpt-image-2.5-flare","prompt":"...","n":1,
 "size":"1024x1280","quality":"low","output_format":"png"}
```

```http
POST https://api.openai.com/v1/images/edits

{"model":"gpt-image-2.5-sunburst",
 "images":[{"image_url":"data:image/png;base64,..."}],
 "prompt":"Preserve the supplied garment; change the scene only.",
 "size":"1024x1280","output_format":"png"}
```

The current JSON edit contract uses `images[]` references, each containing `image_url` or `file_id`. Parse `data[].b64_json`; retain safe request IDs and actual usage when supplied. [Generate reference](https://developers.openai.com/api/reference/resources/images/methods/generate), [Edit reference](https://developers.openai.com/api/reference/resources/images/methods/edit).

Text and vision assistance use `POST /v1/responses`: configurable text model, `input` messages with `input_text` and optional `input_image`. Parse output message content containing `output_text`; empty output or refusals are unsuccessful. [Text generation](https://developers.openai.com/api/docs/guides/text), [Vision input](https://developers.openai.com/api/docs/guides/images-vision). API secrets remain server-side. A lost direct Images response has no verified durable retrieval contract; mark it ambiguous instead of silently repeating it.

## Gemini / Nano Banana

Current documented choices include `gemini-3.1-flash-image` and `gemini-3-pro-image`; the lighter and older variants differ in reference/multiturn suitability. Generated images include SynthID. The current examples use Interactions rather than assuming the legacy `generateContent` envelope. [Image guide](https://ai.google.dev/gemini-api/docs/image-generation).

```http
POST https://generativelanguage.googleapis.com/v1beta/interactions
x-goog-api-key: <server-side key>
Content-Type: application/json

{"model":"gemini-3.1-flash-image",
 "input":[{"type":"image","mime_type":"image/png","data":"BASE64"},
          {"type":"text","text":"Preserve this garment; change its background."}],
 "response_format":{"type":"image","mime_type":"image/png",
                    "aspect_ratio":"4:5","image_size":"2K"}}
```

Persist the interaction ID immediately when received. REST output uses `steps[]` with `type:model_output` and `content[]` image blocks; SDK `output_image` is a convenience accessor. Exclude user-input and thought images from final artifacts. Stored interactions support retrieval and `previous_interaction_id`. Background execution documents `background:true` and `GET /v1beta/interactions/{id}`; enable it only after verifying that the selected image model supports the path. [Interaction schema](https://ai.google.dev/api/interactions-api), [Interactions](https://ai.google.dev/gemini-api/docs/interactions-overview), [Background execution](https://ai.google.dev/gemini-api/docs/background-execution).

Image API free-tier availability differs from AI Studio access. Verify model availability and funded billing before enabling generation; keep model IDs configurable. [Pricing](https://ai.google.dev/gemini-api/docs/pricing), [Billing](https://ai.google.dev/gemini-api/docs/billing).

## Telegram

Create a bot through BotFather; store its token as a password. Posting to a channel requires suitable admin rights. [Bot setup](https://core.telegram.org/bots/tutorial).

```http
POST https://api.telegram.org/bot<TOKEN>/sendPhoto
Content-Type: multipart/form-data

chat_id=@authorized_channel
photo=<PNG/JPEG bytes>
caption=<approved caption>
```

Text uses `sendMessage` with `chat_id` and `text`. `sendPhoto` accepts uploads, URLs, or reusable file IDs; captions have a 1024-character limit after entity parsing. Success returns a Message: persist `chat.id`, `message_id`, and photo `file_id`. Albums use `sendMediaGroup`. Bot API tokens appear in URL paths, so never expose/log complete request URLs. [Bot API](https://core.telegram.org/bots/api).

The app schedules ordinary posts. A receipt enables stable Telegram links where the channel supports them. Reaction updates or member counts do not equal reach. Without a receipt after a timeout, hold the operation as ambiguous; no general safe resend/idempotency mechanism was verified.

## Instagram

Instagram Login scopes documented by Meta include `instagram_business_basic` and `instagram_business_content_publish`. Old `business_basic` names are deprecated. Facebook Login uses its own permission names and Page-token/account lookup flow; adapters must distinguish those credential modes. [Instagram Login](https://www.postman.com/meta/instagram/folder/6raa77c/instagram-api-with-instagram-login), [Official API collection](https://www.postman.com/meta/instagram/documentation/6yqw8pt/instagram-api?entity=request-23987686-6fa9ed1d-3310-4844-ad25-f0001ab66f11).

Publishing shape (configured Graph version/account):

```http
POST /{IG_USER_ID}/media
image_url=https://public-media.example/card.jpg
caption=<approved caption>

GET /{CONTAINER_ID}?fields=status_code,status

POST /{IG_USER_ID}/media_publish
creation_id={CONTAINER_ID}
```

Persist container ID before publishing. Publish only after readiness; return Pending if processing continues. Reels use `media_type=REELS` plus reachable `video_url`; the collection demonstrates `FINISHED` status followed by `media_publish`. Carousel children and Stories require separate capability validation. Do not supply localhost/private file URLs. [Official publication examples](https://www.postman.com/meta/instagram/documentation/6yqw8pt/instagram-api?entity=request-23987686-6fa9ed1d-3310-4844-ad25-f0001ab66f11).

Instagram Login uses the Instagram Graph host; Facebook Login uses Facebook Graph. Pin the version in configuration. Current Meta developer publishing/insights pages returned throttling errors during research. Official Meta Postman confirmed publication flow; exact current photo constraints, account access-review conditions, insights permission, and metric names need verification against the configured version before enabling those capabilities. No guessed metric query should ship as working analytics.

The app owns scheduling. A lost publish response requires reconciling the saved container and uniquely matching owned media; if evidence remains insufficient, mark ambiguous and require review instead of duplicating the post.

## X

Use user OAuth authorization, with `tweet.read`, `tweet.write`, `users.read`, `media.write`, and `offline.access` where refresh is needed. Confidential server applications protect their client secret. Current API billing uses prepaid pay-per-usage credits; connection readiness includes developer access and funded allowance. [OAuth scopes](https://docs.x.com/fundamentals/authentication/oauth-2-0/authorization-code), [Billing](https://docs.x.com/x-api/fundamentals/post-cap).

```http
POST https://api.x.com/2/media/upload
Authorization: Bearer <user access token>
Content-Type: application/json

{"media":"BASE64","media_category":"tweet_image"}

POST https://api.x.com/2/tweets

{"text":"Approved caption","media":{"media_ids":["MEDIA_ID"]}}
```

Persist upload `data.id` and expiry separately from published `data.id`. Posts support up to four photos, or one GIF/video. Video processing requires chunked upload and status checks; do not attach an unprocessed video. [Media upload](https://docs.x.com/x-api/media/upload-media), [Create post](https://docs.x.com/x-api/posts/create-post).

Metrics: `GET /2/tweets/{ID}?tweet.fields=public_metrics`. Public counters include impressions and interactions; own-user private/organic/promoted metrics have additional conditions and a 30-day availability window. Missing fields are omitted, and successful responses can also contain partial errors. Preserve these distinctions. [Metrics](https://docs.x.com/x-api/fundamentals/metrics), [Consistency](https://docs.x.com/x-api/fundamentals/consistency).

The app schedules posts. A saved post ID supports lookup; a lost create receipt requires a unique own-timeline match or manual reconciliation, never an automatic blind repost.

## WhatsApp Business Cloud API

This is customer messaging, not an Instagram feed. Require a registered business phone, appropriate token (`whatsapp_business_messaging`), and recipient permission. User-provided contact details plus opt-in are prerequisites; free-form replies belong within the 24-hour customer-service window. Initiating a conversation or messaging outside that window requires an approved template. [Current messaging policy](https://whatsappbusiness.com/policy/), [Official messages collection](https://www.postman.com/meta/whatsapp-business-platform/folder/o48mro7/messages).

```http
POST https://graph.facebook.com/{VERSION}/{PHONE_NUMBER_ID}/messages
Authorization: Bearer <server-side token>
Content-Type: application/json

{"messaging_product":"whatsapp","to":"E164_DIGITS","type":"template",
 "template":{"name":"APPROVED_TEMPLATE","language":{"code":"en"}}}
```

Allowed image messages use `type:image` with `image:{id:MEDIA_ID}` or `image:{link:PUBLIC_URL}`. Uploads use `POST /{PHONE_NUMBER_ID}/media`; send success returns `messages[].id` (`wamid...`). [Image example](https://www.postman.com/meta/whatsapp-business-platform/documentation/wlk6lh4/whatsapp-cloud-api?entity=request-13382743-13714319-4f97-49ff-ae37-3e8b64183a83), [Media endpoints](https://www.postman.com/meta/whatsapp-business-platform/folder/13382743-ecb27be5-4d27-4763-bbee-6a8002c04bf3).

Webhook statuses distinguish sent, delivered, read, and failed. Arrival order can differ from timestamps; deduplicate by provider message ID/status/time. An accepted send is not delivery. App scheduling must revalidate the conversation window/template at execution. Lost `wamid` receipts stay ambiguous; there is no verified generic message-status GET to invent. [Status webhook](https://www.postman.com/meta/whatsapp-business-platform/request/rgtfq23/message-status-update-notifications).

## Shared durable-operation rules

Before a side effect, persist operation ID, approved content/asset hashes, destination, configuration/model version, and attempt state. Do not persist secrets in request snapshots. After each successful provider stage, persist upload/container/interaction/message IDs. Reuse them for read-only recovery.

| Outcome | Meaning | Caller action |
|---|---|---|
| Succeeded | Concrete generated artifact or final provider receipt obtained | Save artifact/receipt; record publication separately from later metrics |
| Pending | Known operation/container is still processing | Poll the documented read endpoint; do not create again |
| Rejected | Validation, authorization, quota, or explicit provider rejection | Fix the stated prerequisite; no fabricated receipt |
| Unsupported | Adapter/account cannot provide the requested pathway | Offer approved export or supported capability |
| Ambiguous | Request could have executed but receipt is unavailable | Freeze retries; reconcile durable IDs/evidence |

Cancellation before sending can safely stop. Cancellation/transport failures after dispatch are ambiguous unless a provider response proves rejection. Safe GET polling can retry; side effects cannot share a generic automatic retry policy. HTTP 5xx after sending and malformed success bodies also need conservative handling.

Metrics should carry `value:null` with availability/reason (`unsupported`, `unconnected`, `permission_missing`, `not_returned`, `expired`, `pending`) and observation time. Use numeric zero only when the provider actually returns zero. Do not merge WhatsApp delivery, Telegram reactions, and Instagram/X reach as the same measure.

## Useful later integrations

Google Drive fits SMM handoff and asset import/export. Start with user-selected files and `drive.file`, a non-sensitive per-file scope; avoid broad Drive access. Refresh tokens belong in secure server storage. [Drive scopes](https://developers.google.com/workspace/drive/api/guides/api-specific-auth).

UTM-tagged product/order links can connect approved card variants to enquiries using the application's own landing page and events. This is first-party conversion evidence, separate from social reach. A spreadsheet export is sufficient before another analytics service is needed.

Threads is a possible later Meta destination, but adds another permission and publication contract. Keep it deferred until Maftuna wants that channel; avoid expanding the initial connection burden.

## Release checks

Use fake HTTP handlers to verify exact payloads, reference encoding, receipt extraction, Pending, explicit rejection, missing fields, malformed success, and ambiguous timeouts. No tests should contact live providers. Default application policy disables external execution; credentials alone must not activate publishing or paid generation. Connection diagnostics report configured versus verified access distinctly. Account review, OAuth onboarding, public media hosting, webhooks, and live provider acceptance remain deployment gates.


## Additional channel comparison

| Candidate | Why it may fit | Recommendation | Evidence boundary |
|---|---|---|---|
| Google Drive | Product/media handoff to an SMM operator | Add after the local workflow when shared-file handoff is needed | Official `drive.file` supports user-selected files; no account connected |
| Pinterest | Persistent visual discovery from product imagery | Pilot selected imagery after Instagram/Telegram preparation works | Official APIs support image/video Pins and organic analytics; audience value remains a hypothesis |
| Tagged product/order links | Attribute concrete enquiries to source posts | Add neutral campaign/link fields before a separate analytics vendor | First-party event capture requires a real landing/order flow; no conversion evidence exists yet |
| Facebook Pages | Reuse prepared visual content within Meta | Conditional on an actual Page and audience | Direct official Pages documentation could not be retrieved in this pass; capability not implemented |
| Threads | Additional Meta text/media distribution | Defer until there is a demonstrated audience reason | Separate auth/publication contract, not automatic Instagram parity |

Sources checked on 2026-09-27: [Pinterest publishing](https://developers.pinterest.com/docs/work-with-organic-content-and-users/create-boards-and-pins/), [Pinterest organic reporting](https://developers.pinterest.com/docs/analytics-and-reports/organic-reporting/), [Drive scopes](https://developers.google.com/workspace/drive/api/guides/api-specific-auth). Recommendations are product judgments, not evidence of Ocharo's audience demand.


## Delivery increment: verified identity, observations and receipts

Verified official documentation on 2026-09-27. Source and fake HTTP verification establish implemented contracts; no credentialed provider calls, paid requests, social publications, or account verification occurred.

### Identity contracts

Telegram performs three reads: `GET /bot{token}/getMe`, `getChat?chat_id={numeric-or-@alias}`, then `getChatMember?chat_id={resolved-id}&user_id={bot-id}`. It requires the returned bot ID/is_bot, channel ID/type, membership user ID, and administrator `can_post_messages=true` (or creator). Canonical numeric IDs are retained; aliases resolve only during verification. Official IDs have at most 52 significant bits. Token-bearing paths must remain out of HTTP logs; redirects remain disabled. [Bot API methods and types](https://core.telegram.org/bots/api#getme), [getChat](https://core.telegram.org/bots/api#getchat), [getChatMember](https://core.telegram.org/bots/api#getchatmember), [administrator rights](https://core.telegram.org/bots/api#chatmemberadministrator).

X uses `GET https://api.x.com/2/users/me` with a user-context access token and exact configured user-ID matching. The implemented bearer path requires OAuth 2 user context; OAuth 1 signing, PKCE acquisition, refresh and credential storage are outside these adapters. App-only credentials cannot verify /me. Reads are permitted when publishing OR metric reads are explicitly enabled, so metric-only setup can verify identity. Identity success does not prove write scopes or metric access. Returned capabilities describe enabled supported adapter paths. [Authenticated user requirements](https://docs.x.com/x-api/users/lookup/quickstart/authenticated-lookup).

### Observed metrics

`GET /2/tweets/{known-final-id}?tweet.fields=author_id,public_metrics` is available only through separate `XProviderOptions.EnableMetricReads`; publication enablement cannot authorize it. The broker requires a published X receipt, configured account, returned post ID and author ID equality. [Post lookup and requested fields](https://docs.x.com/x-api/posts/get-post-by-id).

Supported public counters are likes, replies, reposts, quotes, bookmarks and impressions. Official field names are `like_count`, `reply_count`, `retweet_count`, `quote_count`, `bookmark_count`, and `impression_count`. Public counters combine organic and promoted results; impressions are exposures rather than unique reach. Private/organic/promoted fields have separate authentication and availability requirements and are unsupported here. [Official metric definitions](https://docs.x.com/x-api/fundamentals/metrics).

Implementation policy: record zero only when an actual valid zero exists. Missing, invalid, permission-denied or unavailable values remain null with explicit availability and no fabricated observation timestamp. Only integer counters from 0 through 9,007,199,254,740,991 are accepted. Partial errors preserve valid returned counters with `HasPartialErrors=true`; ID/author mismatch rejects all counters. Orchestration owns opt-in endpoints, read reservations, cooldown and budgets; brokers perform one request without automatic retry.

### Receipt reconciliation

`IReceiptReconciliationBroker.ReconcileAsync(ReceiptReconciliationRequest(AccountId, Receipt), ct)` returns outcome/provider/code/message and optional verified receipt. X performs `GET /2/tweets/{id}?tweet.fields=author_id`, requiring explicit read enablement, account binding, exact post/author identity, and complete evidence. It never creates a post. [Existing-post lookup](https://docs.x.com/x-api/posts/get-post-by-id).

A published receipt already identifies a final post. An ambiguous publication requires `RelatedIds["post_id"] == ExternalId`; numeric upload IDs and account/destination dispatch markers cannot stand in for post IDs. Missing IDs have no fabricated search path. Telegram explicitly returns Unsupported: this adapter implements no general channel receipt lookup. Telegram’s personal-profile-chat read is a distinct contract and is not used for channel reconciliation. [Personal-chat method](https://core.telegram.org/bots/api#getuserpersonalchatmessages).

### Publishing hardening

Telegram requires configured numeric BotId/DestinationChatId and matching request AccountId, saves publication_started before send, and validates returned message/chat identity. Missing or conflicting final evidence is Ambiguous; replay is refused.

X checkpoints media_upload_started and publication_started before side effects. Uploaded media retains source account, SHA256 and expiry from returned `expires_after_secs`; expired/unbound/mismatched resumes are rejected. Partial upload/final errors remain ambiguous. Final receipts always contain account_id and, where returned, post_id. [Upload response contract](https://docs.x.com/x-api/media/upload-media).

Malformed successful JSON roots now yield explicit unknown outcomes in OpenAI assistance/images and WhatsApp. Other adapters already use guarded property readers. No raw provider body or credential appears in result messages.


The complete backend suite subsequently passed 171 tests, including the malformed-success guards, SQLite delivery and source-analysis workflows. This remains fake-provider/local verification; no live account or billing evidence is implied.
