# Blog image editing

## Implemented behavior

- The editor reads an immutable media SHA and submits an explicit crop/rotate/flip/resize/output recipe.
- The existing SDK `IImageService` performs raster edits. Optional background removal runs first through Platform's private segmentation adapter.
- Preview returns bytes without creating assets, receipts, article revisions or publications.
- Save creates content-addressed output and an immutable receipt in one metadata transaction. Original bytes remain unchanged.
- The receipt records source/output SHA, normalized recipe, trusted organization/user, model and timestamp. Repeated identical transformations deduplicate receipts; model changes remain distinct.
- PNG/WebP retain transparency; JPEG flattens transparent regions onto white.
- Selected cover captions, rights and article publication remain the editor's responsibility; creating a media copy never publishes content.

## Private endpoints

| Route | Behavior |
|---|---|
| `GET /api/blog/media/editing-capabilities` | Supported operations, limits and actual optional worker readiness |
| `POST /api/blog/media/{sha}/edits/preview` | Binary encoded preview, width/height response headers |
| `POST /api/blog/media/{sha}/edits` | Saved `MediaView` |
| `GET /api/blog/media/{sha}/edits` | Private transformation receipts |
| Platform `GET /api/integrations/media/background-removal/capabilities` | Actual private worker readiness |
| Platform `POST /api/integrations/media/background-removal` | Raw validated raster bytes to PNG output |

Recipe JSON: `crop`, `rotate`, `flipHorizontal`, `flipVertical`, `resize`, `output`, `removeBackground`. Unknown fields fail validation. Crops use the EXIF-upright original coordinate system; resize dimensions are exact. Rotation accepts quarter turns only; output accepts png/jpeg/webp and quality 1–100.

## Data and access boundaries

- 20 MiB encoded, 20 megapixels decoded and 8192 pixels per side. Inputs decode completely before acceptance; single-frame PNG/JPEG/WebP only.
- Two simultaneous Editorial edit operations; excess work returns 429. Platform allows one simultaneous segmentation operation.
- Connected Editorial requests require current organization-bound Platform tickets and editor roles. The token is re-introspected before saving; Platform rechecks membership after processing.
- Actor identity comes from the authenticated boundary, never the recipe. Platform accepts explicit Marketing audience ticket headers; cookie sessions alone cannot invoke the processing endpoint.
- No URLs or paths are accepted for image input; the server chooses the configured worker origin. Worker credentials never reach the browser.
- Worker/model availability is optional and reported honestly. Unconfigured/local/disconnected/unready state leaves background removal unavailable; crop/rotate/resize remain usable.
- Processing does not change source media, Commerce approvals, saved catalogue editions or publication status.

## Persistence and rollout

`InitializeDatabaseAsync` adds `media_edits`, a target/timestamp index and update/delete rejection triggers to existing SQLite databases. Existing media and article tables remain unchanged. Receipt insertion and new media metadata insertion share a transaction; failed receipt writes leave no new indexed asset. Blob files may be orphaned after failed database commits and remain private/unreferenced.

Back up SQLite and the blob directory before the runtime update. Parent owns runtime restart and review authentication.

## Verification

- SDK background-removal focused suite: 10/10 passed; real Skia codecs with bounded fake HTTP worker responses.
- Editorial media and delegated-auth focused suites: 19/19 passed against published SDK `10.0.63-beta`.
- Full Editorial backend suite: 138/138 passed, including 15 text-formatting tests from the parallel lane.
- Cases cover immutable originals, preview non-persistence, exact crop/rotation, alpha, JPEG white flattening, model-specific receipts, deduplication, schema repeatability, transaction rollback, capacity rejection/recovery and revoked access before persistence.
- Commands: `dotnet test tests/OcharoBlog.Tests/OcharoBlog.Tests.csproj --no-restore -m:1`; focused filter `FullyQualifiedName~MediaEditingTests|FullyQualifiedName~EditorialImageWorkerTests`.
- Runner receipts: `/tmp/ocharo-editorial-media-tests.log`, `/tmp/ocharo-editorial-full-tests.log`, `/tmp/ocharo-sdk-background-tests.log`.
- Root verified actual U2Net inference and the authenticated browser workflow, including immutable save, unchanged originals/publications, mobile layout and revoked access. Evidence: workspace `docs/editor-tools-build.md` and ignored `.local/editor-tools-review/result.json`.
