# Catalogue discovery verification

*Last updated: 2026-09-29*

## Scope

The buyer catalogue uses the canonical released products and their approved edition.
The discovery controls refine the product view; they never change the printed page order
or PDF bytes.

| Capability | Behaviour |
|---|---|
| Search | Matches product reference, name, garment name, material, category and sizes; each entered word must match. |
| Filters | Intersects category, material and size; values come from the current released feed. |
| Sorting | Catalogue order, localized name order, or numeric price order within one currency. |
| Unknown prices | Remain “Price on enquiry” and sort after known prices in either direction. |
| Different currencies | Price sorting is unavailable; no implicit currency conversion is invented. |
| URL state | Search, filters, sorting and language survive reload and copied links. |
| Language switch | Keeps search, size and sort; clears localized category and material values. |
| Reset | Restores the original catalogue order and all pieces while keeping language. |
| Page links | Every piece links to every released PDF page that explicitly lists its product ID. |
| No JavaScript | Live GET forms render the filtered/sorted result on the server. |
| Static export | Includes the entire product data, standalone discovery script and exact edition links. |

A size filter describes the released size range. It does not promise current stock or
confirmed measurements. Unknown availability stays unknown.

## Verification

- [x] Complete Editorial backend suite: 83 passed, zero failures.
- [x] Thirteen discovery tests cover filter intersections, unknown filters, price ordering,
  different currencies, hostile input encoding, Unicode length bounds, exact page links, empty feeds and static exports.
- [x] Existing edition tests retain release-pinned asset/download and locale contracts.
- [x] Anonymous browser verification against the 29 original pieces; thirteen scenarios passed.
- [x] Desktop (1440px) and mobile (390px) screenshots inspected; no horizontal overflow or JavaScript errors.
- [x] Browser checks without JavaScript and against a static HTML fixture without a backend.
- [x] Browser Back restores the filtered result after visiting a product and its printed page.

Tests: `engineering/codebase/ocharo-blog.backend-services/tests/OcharoBlog.Tests/CatalogueDiscoveryTests.cs`.
Source: `OcharoBlog.Application/Blog/Site/CatalogueDiscovery.cs` in the same backend root.

## Boundaries

No customer enquiry, outbound message or order is submitted by discovery verification.
The existing catalogue edition, asset provenance and publication approval remain unchanged.
Browser evidence stays in the ignored `.local/catalogue-review/` folder.

## Evidence

- `.local/catalogue-review/discovery-result.json`: browser assertions and verification timestamp.
- `.local/catalogue-review/discovery-desktop.png`: buyer filters with the original pieces.
- `.local/catalogue-review/discovery-mobile.png`: full mobile filter layout and first result row.
- `.local/catalogue-review/discovery-browser.mjs`: preserved anonymous browser verification script.

The browser checks use the existing trusted review certificate through `NODE_EXTRA_CA_CERTS`;
TLS certificate validation remains enabled. The static fixture reuses the published HTML/script
and intercepts the static host locally, while the backend suite verifies actual ZIP contents.
