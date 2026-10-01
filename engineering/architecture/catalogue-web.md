# Public catalogue versions

Marketing renders the published catalogue projection owned by Ocharo Platform.

## Release contract

`GET /api/public/v1/brands/{brandSlug}/catalogue?locale={locale}` supplies one release-consistent read.
The additive `catalogues` array contains named versions with `slug`, `title`, `description`,
`locale`, `availableLocales`, optional `web` pages, and optional `pdf` edition.
Each web page references canonical products from that same response.

Platform owns active-page selection, product translation approval and PDF eligibility before rendering.
Marketing accepts the exact requested language, rejects malformed versions, removes unknown web product
references, and omits empty web pages. It never fetches a draft as fallback.

## Routes

- `/catalogue/` lists named catalogues and available Web/PDF actions, followed by product discovery.
- `/catalogue/collections/{slug}/` reads the responsive web version or the available PDF entry.
- `/catalogue/collections/{slug}/pages/{pageId}/` links directly to a released web section.
- `/catalogue/pieces/{slug}/` retains product details and enquiries.
- `/catalogue/editions/{id}/` and page/download routes retain fixed PDF presentation.

The legacy gallery remains available when an older release has no named catalogue metadata.
Live catalogue routes re-read the active release and send `Cache-Control: no-store`.
Withdrawn or wrong-language named routes return 404.

## Screens

Primary target: phone. Public readers also support tablet and desktop.

| Class | Verification | Layout |
|---|---|---|
| Phone | 390 × 844 | One-column product flow, wrapped section navigation |
| Tablet | 820 × 1180 | Adaptive image/copy columns and two-column galleries |
| Desktop | 1440 × 900 | Landscape image/copy spreads and wider galleries |

The `editorial` and `grid` themes accept `spread`, `grid`, and `collage` web pages.
Web readers display semantic product text, prices, media, captions and links; PDF page images belong
only to the PDF reader. Touch links remain available without hover.

## Designer and reader parity

The release freezes `mediaGroup`, `productMedia`, `priceDisplay`, and translated `copy` alongside web
pages. Marketing resolves the selected media hashes against the same response's approved images and
keeps their order. An explicitly empty selection stays empty; it never substitutes another group.
Spread, grid, and collage show up to three, one, and six approved images respectively.

Confirmed price tiers are projected separately from draft prices. Retail, wholesale, currency and
quantity are independently switchable. Both previews use locale-grouped whole numbers with an
optional ISO currency code. The catalogue header, cover note, footer, materials, sizes, quantity and
price-on-request labels come from the frozen selected-language copy.

The canonical designer preview and public product flow share these layout choices:

- Below 768px: one product column; two-column image group with a lead image.
- From 768px: image/copy columns at 1.15:1; grid pages use two product columns.
- From 1024px: editorial image/copy columns at 1.5:1, grid theme at 1:1; grid pages use three columns.
- Spread lead-image height caps at 700px, secondary images at 360px; collages cap at 450px.
- Paper `#f6f0e5`, ink `#302b25`, muted text `#847562`, rules `#d1c3af`.

The editor uses container breakpoints to represent the selected viewport. The public reader uses
viewport breakpoints and adds buyer navigation, product links, share actions and optional PDF links.

## Static export

`/api/blog/export/catalogue.zip?locale={locale}` includes named routes, web sections, and available
release-pinned PDF files. The artifact exposes only the selected content language.
The builder accepts `OCHARO_CATALOGUE_LOCALE`; an explicitly requested language must match the
archive manifest. `OCHARO_EXPORT_ARCHIVE` retains the existing offline build path.
Product photographs continue using the canonical released media URLs; static PDF files are embedded.

## Verification

`WebCatalogueTests` covers web-only/PDF-only versions, exact slugs and locales, hidden products/pages,
malformed upstream entries, optional downloads, withdrawal, named static exports, selected approved
media, explicit empty groups, translated copy and independent price-tier display flags.
The layout checks render test fixtures without starting or restarting an application service.

Executed on 2026-10-01: **161 backend/public tests passed**, including 11 named web-catalogue tests.
All six reader/gallery fixtures passed at phone, tablet and desktop widths. See the
[verification receipt](../planning/version-track/v0.1/catalogue-web-verification.md) for exact checks,
fixture names and limits. **Owner review remains pending.**
