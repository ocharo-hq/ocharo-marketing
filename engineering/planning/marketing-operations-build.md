# Marketing operations build

*Last updated: 2026-09-29*

## Result

The Marketing operator app supports the product-to-post workflow, channel strategy, cadence review,
local delivery monitoring and manual engagement history. Built-in captions use saved templates and
catalogue facts; cards render deterministically. Optional AI assistance and image generation require
configured providers and remain separate explicit actions.

## Completed scope

- [x] Select a product, template and library images; edit caption, save, approve and schedule.
- [x] Preserve independently selected images when card composition changes, including after reopening.
- [x] Persist channel audience, purpose, content pillars, formats, owner, profile and weekly target.
- [x] Compare targets with distinct weekly slots, upcoming plans, receipt confirmations and manual reports.
- [x] Show real worker heartbeat, last automation results, overdue plans and uncertain delivery states.
- [x] Record immutable manual engagement snapshots with evidence, dates and replay protection.
- [x] Preserve unsaved forms after conflict, transport failure or expired authorization.
- [x] Verify authorization, SQLite concurrency, regression cases and connected browser workflows.

## Operator routes

| Route | Capability |
|---|---|
| `/posts/new` | Product, template, ordered media selection, caption, rendering, review, export and scheduling. |
| `/automation` | Catalogue sync, bulk drafts, source-fact captions, story announcements and weekly slots. |
| `/channels` | Eight-channel strategy map, ownership, targets and separate cadence evidence. |
| `/calendar` | Approved delivery snapshots, planned/queued/held/failed/unknown states and reconciliation. |
| `/monitoring` | Local worker freshness, delivery gates, queue health, overdue plans and errors. |
| `/results` | Manual observations, per-post snapshot comparison and separate cached provider observations. |
| `/connections` | Provider configuration, verification and explicit execution/read gates. |

Channel strategy covers Instagram, Telegram, X, WhatsApp, Blog, Atlas, Wear and Catalogue.
Defaults are inactive with empty strategy fields; the app does not invent business commitments.
Strategy writes use optimistic revisions and return 409 on stale edits.

## Evidence semantics

- Cadence weeks start Monday in Asia/Tashkent. Weekly slots are distinct day/time pairs; plans are future intentions.
- **Confirmed receipts this week** means receipt confirmation time. Reconciliation can confirm an older post;
  it does not establish that post's publication date. The UI never labels this count actual publications this week.
- Manual post counts use entered publication dates. Manual and provider figures stay separate, with overlap disclosed.
  Linked manual reports already represented by confirmed schedules are excluded from the manual weekly count.
- Manual snapshots group by channel and exact post URL, keeping the latest observation per post. Cumulative snapshots
  are never summed. The newest 200 observations are returned with the total count disclosed; server cadence uses all records.
- Engagement rate requires positive reach and all four interaction counts: `(likes + comments + shares + saves) / reach`.
  Missing values stay unknown; entered zero remains zero. Snapshot differences compare matching manual post identities.
- Manual enquiries and orders are operator-entered counters, not reconciled commerce records.
- Worker health comes from real background loops, resets to unobserved on process restart and becomes stale when overdue.
  Monitoring polls only the local API. A healthy worker does not imply configured providers or successful delivery.
- Instagram, Telegram and X support weekly planning slots. WhatsApp uses manual cadence and gets no impossible slot warning.
- Recipe-bound rendered outputs are persisted explicitly through optional `composition.boundMediaIds`.
  Existing drafts remain compatible; arbitrary selected library cards are never inferred to belong to a recipe.

## Verification

Final checks against the completed code on 2026-09-29:

| Check | Result |
|---|---|
| Complete Marketing backend suite | 236 passed, 0 failed, 0 skipped. |
| Marketing web suite | 41 passed across 7 files. |
| Production web build | TypeScript, 29 Vue SFCs and Vite production build passed. |
| Frontend ESLint | Passed. |
| Connected browser workflow | Six groups passed; strict TLS; no JavaScript errors. |
| Visual review | Eight desktop/mobile captures; no horizontal overflow. |

Backend tests include SQLite compare-and-swap and initial-write races, idempotent observation replay,
unknown/zero and denominator handling, rejected foreign schedules/invalid dates/URLs/counts, Tashkent cadence,
cross-week receipt reconciliation, manual-channel warnings, real worker freshness and private-route authorization.
Frontend tests cover conflicts retaining fields, navigation protection, replay IDs, selected photo retention,
independent saved card retention and explicit recipe binding.

Browser checks used the real Desk connection and isolated local review data:

1. Sign in and acquire a Marketing audience ticket.
2. Create a post, select an image, edit its title, set a caption, save, approve and create a future plan.
3. Save channel purpose, audience, pillars, owner and weekly target; restore synthetic settings afterward.
4. Record a manual observation, verify explicit 10% reach rate and idempotent replay.
5. Read monitoring and real worker health; inspect desktop/mobile layouts.
6. Log out of Desk, confirm revoked Marketing authorization and retained unsaved channel text.

The post and future plan remained local; nothing was queued for external delivery. Synthetic review draft,
plan and observation remain in the isolated review database. The shared Desk session was logged out afterward.

Private evidence is ignored at `.local/operations-review/`: `result.json`, eight PNG captures and the browser
verification scripts. These scripts read review credentials from the private workspace file; no credentials
are copied into tracked files. Root owns the running API on 8354/8355 and frontend on 8344.

## Publication boundary

No provider account was connected, contacted or charged during this verification. Telegram and X have gated
verified publishing adapters. Instagram and WhatsApp continue through manual export pending their distinct
provider workflows. AI generation, live social delivery, external engagement refresh and hosted enablement
were not exercised. Final channel strategy, cadence and content approval remain operator decisions.
