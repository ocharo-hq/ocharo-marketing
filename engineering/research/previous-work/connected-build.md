# Connected Marketing build

*Last updated: 2026-09-29*

## Delivered local product flows

- Marketing owns one frontend workspace with `apps/web`, `apps/editorial`, and independently built `apps/{atlas,wear,blog,catalogue}`.
- The adopted Editorial API owns common SQLite story/revision storage. The original thirty Blog stories and historical routes are preserved.
- Atlas adds object records, gallery filtering, place/technique/motif routes, source-linked stories and attribution. Wear adds occasion filters, guides and linked pieces. Each public app has its own visual treatment.
- Explicit research import creates drafts, preserves existing slugs, verifies imported image hashes and retains source/rights metadata.
- Editorial saves preserve publication, kind, object metadata, tags and image provenance. Publishing, scheduling, unpublishing and restoration retain revision checks.
- Both desks connect using a Platform fragment ticket exchanged for a secure HttpOnly cookie. The fragment is removed immediately; tickets are never persisted in browser storage. Expiry leaves editors mounted and unsaved edits intact.
- Private reads, writes, previews and downloads require a current `ocharo-marketing` ticket matching the configured organization. Mixed-case paths receive identical checks; approval actions require `canApprove`.
- Connected requests/introspection use HTTPS. Browser mutations require exact-origin checks, cookies are Secure and SameSite Strict, private responses retain `Cache-Control: no-store`.
- Catalogue and Marketing sync consume Platform's released `schemaVersion:1` public catalogue. Unavailable releases produce an empty public catalogue and preserve private drafts.
- Product enquiries use an explicit consented form; Marketing validates contact, product publication, bounds, honeypot and idempotency. SQLite records survive restart; inbox updates use optimistic revisions.
- Studio intake accepts a matching PNG plus `.ocharo-review.json`, verifies image/document hashes, preserves the manifest and organization context, and creates a draft labelled creative visualization. An existing product is context only. Source approval is an author claim for review, not product truth, rights clearance or publication approval.
- SDK `@wow-two-beta/ui-vue@0.0.9` supplies native Button click declarations; active buttons use direct public exports. Two pre-existing narrow type adapters remain: published FilePicker still rejects `accept`, and SelectPickerTrigger rejects `aria-label` in vue-tsc. The upstream declaration gaps remain a concrete SDK follow-up.

## Runtime configuration

The current review remains loopback-only. Marketing retains its Development-only startup guard. Automatic approval review rejected expanding this persistent hosting boundary, so remote hosting was left unchanged; local connected authentication and all unaffected product work proceeded. A concrete hosted deployment configuration requires separate approval.

| Setting | Editorial | Marketing |
|---|---|---|
| Content root | `ocharo-blog.backend-services/OcharoBlog.Api` | `ocharo-marketing.backend-services/OcharoMarketing.Api` |
| Default API HTTPS | 8280 | 8276 |
| Default UI HTTPS | 8284 (`/studio/`) | 8358 |
| Review HTTPS / HTTP | 8350 / 8351 | 8354 / 8355 |
| Data override | `Blog__DataRoot`; `ConnectionStrings__OcharoBlog` | `Marketing__DataDirectory` |
| Canonical product origin | `Blog__PlatformBaseUrl` | `Marketing__PlatformBaseUrl` |
| Brand slug | `Blog__BrandSlug` | `Marketing__BrandSlug` |
| Inquiry forwarding | `Blog__MarketingBaseUrl` (HTTPS Marketing API) | Receives `/api/public/inquiries` |

Both APIs need `PlatformIntegration__Mode=connected`, `PlatformIntegration__BaseUrl=https://…`, and `PlatformIntegration__OrganizationId=<owning organization GUID>`. Development defaults select connected mode with an empty binding, which intentionally denies private access. Explicit local mode remains loopback constrained. Main `App_Data` was not used by the review tests.

Frontend review commands from `ocharo-marketing.frontend-services`:

```sh
pnpm --filter @ocharomarketing/web dev --port 8344
pnpm --filter @ocharomarketing/editorial dev --port 8352
```

Set `VITE_API_TARGET` to the corresponding HTTPS API and `VITE_PLATFORM_URL` to the Platform UI. Supply an existing trusted pair with `OCHARO_TLS_CERT` and `OCHARO_TLS_KEY`; supply its trusted CA to Node with `NODE_EXTRA_CA_CERTS`. Proxies verify certificates. Native certificate installation is not automated by this review configuration.

Platform destinations are Marketing `/` and Editorial `/studio/`; the fragment key is `platformToken` (`ocharo-ticket` is accepted for compatibility). The receiver never trusts a fragment organization claim; backend introspection checks its configured binding. Session status is `/api/integrations/session`, exchange is `POST /api/integrations/connect`.

## Commands and public artifacts

```sh
pnpm install --frozen-lockfile
pnpm build
pnpm build:editorial
pnpm --filter @ocharomarketing/web --filter @ocharomarketing/editorial test
pnpm --filter @ocharomarketing/web --filter @ocharomarketing/editorial lint
```

Each backend: `dotnet test tests/OcharoBlog.Tests/OcharoBlog.Tests.csproj` or `dotnet test tests/OcharoMarketing.Tests/OcharoMarketing.Tests.csproj` in its own backend root. Tests use temporary isolated SQLite stores and fake providers.

Public commands `pnpm build:atlas`, `build:wear`, `build:blog`, `build:catalogue` consume an authenticated Editorial ZIP export. Set `OCHARO_EXPORT_ARCHIVE=/absolute/path/export.zip` for an already reviewed download, or `OCHARO_EDITORIAL_URL`, `OCHARO_EXPORT_TICKET`, and optional `OCHARO_CA_FILE` for HTTPS export retrieval. Tokens must remain ephemeral environment values. Each artifact lands in its own `apps/<name>/dist`.

Optional `OCHARO_PUBLIC_ATLAS_URL`, `OCHARO_PUBLIC_WEAR_URL`, `OCHARO_PUBLIC_BLOG_URL`, and `OCHARO_PUBLIC_CATALOGUE_URL` set cross-app navigation on independent hosts. Static hosting needs a same-origin `/api/public/inquiries` proxy to receive inquiries; otherwise visitors can use the listed contact channels. `build:archive` in `apps/blog` retains the original static manuscript compiler for comparison and is not the canonical publication build.

## Review routes

- Editorial: `/studio/`; story previews `/preview/{id}`; draft publication previews `/preview/apps/atlas/`, `/preview/apps/wear/`.
- Public: `/`, `/journal/`, `/atlas/`, `/atlas/gallery/`, `/atlas/textiles/{slug}/`, `/wear/`, `/wear/guides/{slug}/`, `/catalogue/pieces/{slug}/`.
- Research drafts: `POST /api/blog/research/import` (idempotent, never publishes).
- Studio intake: `/creative-imports`; multipart `POST /api/marketing/creative-imports` with `manifest`, `image`, `productId`.
- Inbox: `/inquiries`; `GET /api/marketing/inquiries`, `PUT /api/marketing/inquiries/{id}` with `expectedRevision`, `status`, `note`.
- Released product sync: `POST /api/marketing/catalogue/sync`.

## Verification recorded in this build

- Editorial backend: 46 passing tests, including security, research draft isolation, independent exports and guide publication/unpublication.
- Marketing backend: full suite 205 passing tests before final review; subsequent targeted security suite 43 passed, covering scheduling permissions and normal port migration. Automation settings and slot filling require an approver; draft creation and caption preparation remain available to editors. The old port 8242 is explicitly rejected; the allocated normal UI port is 8358.
- Frontend suites: Marketing 33 passed; Editorial 8 passed. Both production builds passed with SDK 0.0.9.
- Independent public exports passed: Atlas 5 HTML pages, Wear 3, Blog 35, Catalogue 3. Every artifact records its source export SHA-256; the export app is validated before replacement.
- Root isolated integration harness passed fourteen checks covering real Studio import/replay/tamper, released product sync, consented public enquiry into the durable inbox, stale edits, research previews and connection revocation.
- Public desktop/mobile browser review passed ten page checks with no horizontal overflow, failed images or JavaScript errors. Research stays private until explicit publication; the test product is visibly synthetic.
- Actual Desk-to-Marketing and Desk-to-Editorial browser handoffs passed six grouped checks: immediate fragment removal, secure cookie exchange, persisted inbox entry, research editor/gallery images, and logout revocation preserving both unsaved editors. Desktop/mobile screenshots are in the isolated workspace review folder.
- Final Marketing inbox and creative intake use the existing theme and SDK buttons; final production build and targeted lint passed. Focused desktop/mobile browser review then passed with full-width mobile note controls and intact connection-revocation behavior.
- Public launch, real outbound campaigns, paid provider calls and hosted deployment were not performed.
