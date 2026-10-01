# Named web catalogues — verification receipt

*Executed: 2026-10-01 · Owner review: pending*

## Delivered scope

Marketing consumes Platform's released catalogue versions. The gallery and named web readers support
optional PDF downloads, active page selections, exact content languages and named static exports.
Readers use approved product text and media, preserve chosen photo order, and apply frozen translated
copy and independent retail, wholesale, currency and quantity display options.

Architecture: [Public catalogue versions](../../../architecture/catalogue-web.md).

## Backend/public tests

Executed with Node 24.11.0 on PATH, from `engineering/codebase/ocharo-blog.backend-services`:

```sh
dotnet test tests/OcharoBlog.Tests/OcharoBlog.Tests.csproj --no-restore -v:q
```

**Result: 161 passed, 0 failed, 0 skipped.** The 11 cases in
[`WebCatalogueTests.cs`](../../../codebase/ocharo-blog.backend-services/tests/OcharoBlog.Tests/WebCatalogueTests.cs)
cover these boundaries:

| Case | Result |
|---|---|
| `Feed_parses_named_web_only_version_without_requiring_pdf_pages` | Passed |
| `Feed_drops_foreign_locale_unsafe_slug_and_unknown_product_pages` | Passed |
| `Feed_does_not_accept_unsafe_named_pdf_or_arbitrary_theme` | Passed |
| `Gallery_and_reader_use_product_text_and_media_without_pdf_dependency` | Passed |
| `Named_pdf_can_stand_alone_and_legacy_pinned_reader_stays_available` | Passed |
| `Named_routes_are_exact_and_withdrawn_or_wrong_locale_versions_disappear` | Passed |
| `Empty_product_pages_are_defensively_excluded_from_reader_and_exports` | Passed |
| `Static_export_freezes_named_pdf_and_keeps_slug_and_locale_exact` | Passed |
| `Frozen_web_settings_select_approved_hashes_and_confirmed_price_tiers` | Passed |
| `Reader_honors_empty_media_selection_hidden_prices_and_localized_copy` | Passed |
| `Reader_filters_retail_and_wholesale_independently_and_preserves_selected_media_order` | Passed |

## Responsive fixtures

Isolated headless Chrome rendered the generated catalogue reader/gallery HTML with the actual
publication CSS. External product media was replaced by a deterministic garment illustration;
external fonts and scripts were omitted. No application service was started or restarted.

| Fixture | Viewport | Horizontal overflow | Product columns | Checked action height |
|---|---|---|---|---|
| `reader-phone.png` | 390 × 844 | None | 1 | 44px |
| `gallery-phone.png` | 390 × 844 | None | Gallery | 44px |
| `reader-tablet.png` | 820 × 1180 | None | 2 | 44px |
| `gallery-tablet.png` | 820 × 1180 | None | Gallery | 44px |
| `reader-desktop.png` | 1440 × 900 | None | 2 | 44px |
| `gallery-desktop.png` | 1440 × 900 | None | Gallery | 44px |

**Result: six fixtures passed.** Reader phone and desktop screenshots were also visually inspected.
Action measurements cover collection section navigation and gallery “Explore online” links.
Session-local evidence is `ocharo-webcatalogue-layout/verification.json` alongside the named PNGs;
the checked outcomes are preserved in this receipt, while temporary screenshots are not repository assets.

## Owner review pending

- [ ] Review real released product images and translated text in the named web reader.
- [ ] Compare catalogue designer selections, price options and copy with the released reader.
- [ ] Review phone, tablet and desktop presentation with the actual fonts and buyer navigation.
- [ ] Open optional PDF downloads and a selected-language static export.

The automated fixtures verify layout mechanics and projection boundaries. They do not establish
real-media visual acceptance, live provider publication, hosted availability or production deployment.
