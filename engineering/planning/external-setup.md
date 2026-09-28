# Ocharo Marketing — setup and acceptance seams

*Last updated: 2026-09-27*

## Local build versus live acceptance

The local pilot is for one operator on this computer. Provider fakes verify payloads, receipts, uncertainty, ownership, budgets and persistence; they do not establish live account access. Current runtime flags remain disabled. No paid generation, real post/message, live engagement read, SDK package publication or remote deployment was performed by this build.

Use [review guide](review-guide.md) for the actual app and [handoff](handoff.md) for current verification. Review the prepared post flow and Uzbek wording before any live canary. Existing catalogue copy and generated product imagery still need product-fidelity review.

## Exact remaining actions

| Seam | Required action | Acceptance evidence |
|---|---|---|
| Product review | Review the local composer, source-analysis wording, queue confirmation, results and three starting card recipes with Maftuna | Recorded choice of starting presets and accepted operator wording; no implied acceptance from build tests |
| Operator/runtime | Choose whether the first user runs locally or needs an authenticated hosted app | Local startup ownership, or a separately reviewed authentication, backup and deployment plan |
| OpenAI/Gemini | Configure the intended project credentials server-side, verify model access and the intended spending limit, then explicitly authorize a small generation/analysis canary | Actual returned artifact/text, provider/model/request evidence and human fidelity review |
| Telegram | Choose the bot and channel, grant intended posting rights, configure numeric bot/channel identities, explicitly enable account verification | Verified bot identity and channel administrator rights; separately authorize one reviewed live post and inspect the actual destination |
| X | Choose the intended account/application, confirm scopes and available API access, configure its numeric user ID | Identity verification plus separately authorized publication/read canaries; `users/me` alone does not prove write scope or paid-read entitlement |
| Instagram | Choose credential mode and professional account; confirm current configured-version requirements; implement reviewed JPEG delivery and controlled public media hosting | Real authorized account/container/publication acceptance; localhost PNG URLs are not a hosting solution |
| WhatsApp | Define the customer-message purpose, recipient consent evidence, approved template/window rules and webhook ownership | Accepted send and later delivered/read/failed receipts recorded separately; no generic feed/Status workflow is implied |
| SDK extraction | Review the working consumer, inventory complete capability vectors and move neutral pieces through SDK source/package verification | Published package evidence and successful consumer adoption; current product-local wrappers are not extracted packages |

Instagram and WhatsApp publication brokers are available for isolated contract work. Their end-to-end operator workflows remain explicitly unavailable in this pilot; this is not feature parity with Telegram/X queueing. New channels such as Pinterest, Drive and tagged order links stay researched follow-ons until selected for an actual use case.

## Server-owned configuration

Secrets belong in the chosen server-side secret mechanism, never checked-in settings, screenshots, browser fields or API responses. Provider options are captured at startup; restart the owned API after changing them. The global delivery switches and provider disables are read dynamically. Hot-reloaded credentials/account fields that differ from the loaded broker binding hold execution until restart and reverification. Do not turn off the local pilot boundary merely to expose this app remotely.

| Configuration | Role |
|---|---|
| `Marketing:EnableLivePublishing` | Global dispatch switch; default false |
| `Marketing:EnableAccountVerification` | Explicit remote account verification; default false |
| `Marketing:EnableMetricReads` | Explicit potentially billable X metrics/reconciliation reads; default false |
| `Marketing:MaximumMetricReadsPerHour` | Persisted read-attempt budget across restart; default 10 |
| `Marketing:MetricRefreshCooldownSeconds` | Per-plan explicit read cooldown; default 60 |
| `Marketing:ConnectionRevisions:{telegram\|x}` | Optional configuration revision, included in internal connection binding |
| `Providers:Telegram:{Enabled,BotToken,BotId,DestinationChatId}` | Bot credentials and numeric source/destination binding |
| `Providers:X:{Enabled,EnableMetricReads,AccessToken,UserId}` | Separately enabled publication/read paths and source account |
| `Providers:OpenAi:{Enabled,ApiKey,ImageModel,TextModel}` | Image/edit and source/text assistance configuration |
| `Providers:Gemini:{Enabled,ApiKey,ImageModel}` | Reference-guided image-generation configuration |

Keep dispatch disabled during account setup. Verification confirms the returned account binding and supported checks. It does not silently authorize earlier plans. Queue one reviewed future plan explicitly after the intended live operation is authorized. The app freezes its caption, artwork hashes, destination and revisions. Disabled, changed or failed verification revokes unstarted authorization; create a new plan to authorize it again.

Unknown delivery outcomes never trigger automatic reposting. Read-only reconciliation accepts only a stored, known final X post ID bound to the original account. Missing final IDs, Telegram unknown outcomes and unsupported channels require inspection of actual provider history. Never substitute a media-upload or dispatch marker for a published-post ID.

Results navigation and readiness refresh are local reads. The separate refresh/verify/analyze actions identify remote work; account or credential appearance never activates old plans or causes background analytics polling. Reconciliation and metrics share the durable read budget. Missing counters remain unavailable, and failed reads keep prior observed values and timestamps.
