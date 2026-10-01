# Public catalogue edition build

*Updated: 2026-09-29*

## Product shape

The buyer catalogue contains canonical released pieces plus a fixed-page catalogue reader.
Platform owns product facts, original design documents and rendered edition artifacts.
Marketing reads one released snapshot and renders the public browsing experience.
Original page images, complete PDF and individual PDF/PNG pages use the exact Platform output.
No second design editor or mutable shared public document is introduced.

## Implemented

- [x] Read canonical products and optional edition from one public release response.
- [x] Validate edition assets against the configured Platform origin, brand and immutable edition path.
- [x] Pair a warm paper buyer view with the original cover, product grid and catalogue navigation.
- [x] Add catalogue-page gallery and individual page reader with previous/next links.
- [x] Download full PDF, individual page PDF and individual page PNG.
- [x] Copy the stable page link and connect page products to buyer detail pages.
- [x] Remove edition routes when the active release withdraws or replaces the edition.
- [x] Mark live catalogue pages no-store; no stale released data is used after a failed feed read.
- [x] Package exact edition files in explicit static export; reject incomplete export downloads.
- [x] Select matching Uzbek, Russian or English editions while preserving locale through buyer navigation.
- [x] Verify actual imported catalogue pages, downloads and responsive layout in the connected review runtime.

## Public contract

Platform feed: `GET /api/public/v1/brands/{slug}/catalogue?locale=uz|ru|en`.
Default and unsupported locales resolve to English. The consumer requires the returned edition locale to match.
Existing `schemaVersion: 1`, release identity and products remain the boundary.
Optional `edition` contains `id`, `brandId`, `sourceDesignId`, `sourceRevision`, `title`, `locale`,
`templateId`, `pageCount`, `releasedAt`, `pdfUrl` and ordered `pages`.
Each page contains `id`, `number`, `title`, `widthMm`, `heightMm`, `productIds`, `imageUrl`, `pdfUrl`, `pngUrl`.

Asset URLs are pinned under `/api/public/v1/brands/{slug}/catalogue-editions/{editionId}/`:
`catalogue.pdf`, `{pageId}.pdf`, `{pageId}.png`. Only the approved edition in the active release is served.
`?release={releaseId}` pins every asset to the same active release. `&download=1` asks Platform for an attachment response; HTML download attributes alone cannot force
cross-origin downloads. Consumer URLs reject user info, unexpected query/fragment, foreign origin,
wrong brand, wrong edition, wrong asset path, missing/wrong/duplicate release pins and malformed page order/count.

Marketing routes:

- `/catalogue/`: buyer catalogue with original cover and pieces.
- `/catalogue/editions/{id}/`: whole edition page gallery.
- `/catalogue/editions/{id}/pages/{pageId}/`: original page, downloads and corresponding pieces.
- `/catalogue/pieces/{slug}/`: existing product details and enquiry flow.

Live readers always re-fetch the active release. Explicit static exports are frozen snapshots;
they copy exact released PDF/PNG bytes to `catalogue/files/{editionId}/` and record `catalogueEditionId`
in `publication.json`. Each static export contains one selected language and displays a language indicator;
it does not offer query-string switches to absent languages. Live pages retain the three-language selector.
A published static artifact has its own deployment/withdrawal lifecycle.

## Verification

Final checks on 2026-09-29:

| Check | Result |
|---|---|
| Complete Editorial backend suite | 65 passed, 0 failed, 0 skipped. |
| Public JavaScript syntax | `node --check` passed. |
| Original buyer inventory | All 29 source pieces render; all product images loaded successfully. |
| Original fixed-page edition | 16 original page images loaded; gallery and reader use unchanged published assets. |
| Languages | Uzbek, Russian and English each resolve to their own released 16-page edition. |
| Navigation | Selected locale survives buyer, edition, page, product and export links; invalid locale falls back to English. |
| Native browser downloads | Complete PDF, page PDF and page PNG saved successfully and match the released file hashes. |
| Visual checks | Eight desktop/mobile screenshots; no horizontal overflow or JavaScript errors. |
| Public/private boundary | Buyer and downloads work anonymously; private export returns 401 without a ticket. |
| Transport | Strict TLS using the existing review certificate; verification never disables certificate validation. |

Regression tests cover complete release reads, missing/wrong/duplicate asset release pins,
foreign brand/origin/path rejection, malformed page order/count, withdrawal without cached fallback,
page navigation and product links, exact export bytes, incomplete-export failure, matching locales,
invalid-locale fallback and single-language static exports.

Anonymous browser evidence was recorded at `2026-09-29T15:19:21.211Z` before the final export-only
language-indicator adjustment. The final 65-test suite includes that adjustment; live buyer rendering
was unchanged by it. Root restarted the final Editorial binary. A bounded check at
`2026-09-29T15:22:04.711006+00:00` confirmed all three locales return 200, 29 pieces,
the matching language selection, download links and `Cache-Control: no-store`.

Downloaded English files:

| File | Bytes | SHA-256 |
|---|---:|---|
| Full catalogue PDF | 14,056,338 | `11033adaf4c115896094d7cdd122951b63f54de1960b3b5dcb0568e3fb327ba5` |
| Page 02 PDF | 738,712 | `49116a0de499a6f262d536838b514c5c243d1f92c9bb7be418b9f5d843a19302` |
| Page 02 PNG | 3,516,280 | `aab2cab8a4aaeee7466e5eb0ed6e9cc958e7a4bcddba67f53e1f93c5fdd9710f` |

Private evidence remains ignored at `.local/catalogue-review/`: `result.json`, `post-restart.json`, `browser-check.mjs`,
`catalogue.pdf`, `page-02.pdf`, `page-02.png`, and eight captures:
`{buyer,edition,page,piece}-{desktop,mobile}.png`.

The local review brand preserves the separate synthetic fixture. No production launch, remote data change,
provider call, paid generation or real customer enquiry was performed by this lane.
