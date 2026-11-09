# Convention sweep

*Last updated: 2026-11-09*

Inventory of where `ocharo-marketing` departs from the WoW2 conventions (`wow-two-ws/conventions/conventions.md` and the files it indexes), found on 2026-11-09. Read-only agents compared the repository with the conventions; recorded exceptions are excluded and listed below. The themed work lives in the [backlog](../planning/backlog.md#convention-sweep).

## Summary

| Area | Rows | S | M | L | Changes behaviour or data (yes / no) |
|---|---|---|---|---|---|
| Deployment and CI (M-1 to M-10) | 10 | 8 | 2 | 0 | 1 / 9 |
| Backend host and service shape (M-11 to M-22) | 12 | 4 | 6 | 2 | 1 / 11 |
| Persistence (M-23, M-24) | 2 | 0 | 0 | 2 | 2 / 0 |
| Backend tests (M-25 to M-28) | 4 | 0 | 3 | 1 | 0 / 4 |
| Frontend (M-29 to M-35) | 7 | 4 | 3 | 0 | 4 / 3 |
| Repository layout, docs and planning (M-36 to M-43) | 8 | 6 | 2 | 0 | 0 / 8 |
| Total | 43 | 22 | 16 | 5 | 8 / 35 |

## Scope and notation

Scope: `workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/ocharo-marketing` (read-only, 2026-11-09).
`where` paths are repo-relative to `ocharo-marketing`; `rule` paths are relative to the WoW2 `conventions/` root.
`B` = `engineering/codebase/ocharo-marketing.backend-services`, `L` = `engineering/codebase/ocharo-blog.backend-services`,
`F` = `engineering/codebase/ocharo-marketing.frontend-services`. Size: S < 1 h, M <= 1 day, L > 1 day.
"known" = already listed in `ocharo-ws/docs/conventions-and-review.md` or the repo's own docs.

## Excluded as recorded exceptions (not breaks)

- HTTPS-only local transport: one `https` profile with one URL, no `VITE_HTTPS=false` fallback (`ocharo-ws/docs/local-development.md`).
- Blog backend names `OcharoBlog.*` / `ocharo-blog.backend-services`, and the `ocharo-blog.frontend-services` shim (`blog-adoption.md`, `CLAUDE.md`).
- `apps/{atlas,wear,blog,catalogue}` as generated publication artifacts, not Vue apps (`conventions-and-review.md` "Public frontend architecture").
- Session handling in `packages/connect` instead of the SDK auth bridge (`architecture.md` "Sign-in and session expiry").
- Skipped per brief: lock files, Files adoption (`FilesMediaStore`, `FilesAssetService`, `files-migration.yml`), untracked `output/promo-15s/`.

## Deployment and CI

| id | where | rule | break | size | changes behaviour or data | fix |
|---|---|---|---|---|---|---|
| M-1 | `engineering/deployment/deploy.yml` (whole file) | `deployment/descriptor/deploy-descriptor.md:46` | No `migrate:` item for either image that hosts a schema set. | M | no | Add `migrate:` items once sets are bespoke SQL (M-24); ship `app migrate`. |
| M-2 | `engineering/deployment/deploy.yml` (whole file) | `deployment/descriptor/deploy-descriptor.md:47` | No `configCheck:` item and no `config check` verb on either service. | M | no | Add the verb per config-check.md; declare `configCheck:` for both services. |
| M-3 | `engineering/deployment/deploy.yml:12`, `product/product.json:1` | `deployment/descriptor/deploy-descriptor.md:20`; `development/repo/structure/product-conformance.md` Design rows | No `name`/`description` identity; retired `product/product.json` still present. | S | no | Put identity in `deploy.yml`; delete `product.json`. |
| M-4 | `engineering/deployment/deploy.yml:12` | `deployment/descriptor/deploy-descriptor.md:53` | `dotnetEnvironment: true` not set; dev does not run as `Development`. | S | yes | Set the key once `appsettings.Development.json` is deploy-safe (it is); check dev config. |
| M-5 | `engineering/deployment/deploy.yml:12` | `deployment/descriptor/deploy-descriptor.md:55` | No `verify` check although releases auto-deploy to dev then promote. | S | no | Add a `verify` check hitting `/health` and one anonymous route. |
| M-6 | `.github/workflows/publish-docker-image.yml:13,40` | `deployment/descriptor/deploy-descriptor.md:634` | Shared `delivery.yml` pinned by SHA, not a pipelines tag (known: file comment, untagged commit). | S | no | Re-pin to the first tag containing `e134a47`. |
| M-7 | `.github/` (no `renovate.json`) | `development/repo/structure/repo-structure.md:32` | No `renovate.json` extending the pipelines preset. | S | no | Add `.github/renovate.json` from the product template. |
| M-8 | `engineering/codebase/ocharo-marketing.frontend-services/apps/{atlas,wear,blog,catalogue}/package.json:8` | `deployment/hosting/ports.md` (ledger) | Preview ports 8772-8775 are absent from the port ledger. | S | no | Add the four rows to `ports.md`. |
| M-9 | `B/OcharoMarketing.Api/appsettings.json:9,19,20`; `L/OcharoBlog.Api/appsettings.json:10,17,18,20` | `deployment/hosting/environment-files.md:10,14` | Loopback URLs and `AllowedHosts` sit in base settings, not `Development` or `Local`. | S | no | Move loopback values to `appsettings.Development.json`/`Local.json`. |
| M-10 | `engineering/deployment/blog/Dockerfile:5-6,9` | `development/repo/structure/repo-structure.md:147` | Header comment names `ocharo-blog.frontend-services`; the build copies the marketing workspace. | S | no | Correct the comment and the sample run line. |

## Backend host and service shape

| id | where | rule | break | size | changes behaviour or data | fix |
|---|---|---|---|---|---|---|
| M-11 | `B/OcharoMarketing.Api/Configurations/HostConfiguration.cs:11`; `L/OcharoBlog.Api/Configurations/HostConfiguration.cs:12` (2 hosts) | `development/backend/dotnet/shapes/service/platform/startup/startup-defaults.md:9` | Hosts hand-wire pipeline; no `AddApiDefaults()` / `UseApiDefaults()`. | L | no | Adopt the boot bundle; delete hand-wired proxy, problem-details, OpenAPI, status code. |
| M-12 | `B/.../HostConfiguration.Extensions.cs:351`; `L/.../HostConfiguration.Extensions.cs:300` | `development/backend/dotnet/shapes/service/platform/startup/startup-defaults.md:46-47` | Hand-written `MapGet("/health")` with no per-dependency readiness check. | S | no | Take `/health` from the bundle; add database readiness check. |
| M-13 | `B/.../HostConfiguration.Extensions.cs:339-340,366`; `L/.../HostConfiguration.Extensions.cs:289,315` | `deployment/hosting/single-host-serving.md:43` | SPA served by hand (`UseStaticFiles`, `MapFallbackToFile`), not `SpaHosting`. | M | no | Set `ApiDefaultsOptions.SpaHosting`; Blog keeps the `/studio/` prefix. |
| M-14 | `B/.../HostConfiguration.Extensions.cs:326`; `L/.../HostConfiguration.Extensions.cs:248` | `development/backend/dotnet/shapes/service/platform/responses/serialization.md:24` | `AddControllers()` instead of `AddControllersWithSdkJson()`. | M | yes | Switch; retype nulls and `TimeSpan` in both frontends, then run `vue-tsc`. |
| M-15 | `B/OcharoMarketing.Api/Program.cs:3-11`; `L/OcharoBlog.Api/Program.cs:3-11` | `development/backend/dotnet/shapes/service/platform/startup/host-configuration.md:33,43` | `Program.cs` builds and runs the host; `args` never reach `ServiceHost.RunAsync`. | M | no | Hand `args` to `ServiceHost.RunAsync`; prerequisite for M-1, M-2. |
| M-16 | `B/OcharoMarketing.Infrastructure/Inquiries/PublishedProductVerifier.cs:6`; `.../Marketing/Services/DeliveryPolicy.cs:10`; `L/OcharoBlog.Api/Blog/Controllers/PublicInquiriesController.cs:7` | `development/backend/dotnet/shapes/service/platform/startup/host-configuration.md:12-13` | Collaborators take `IConfiguration` and read keys inside the service. | S | no | Bind option records in the host; inject them. |
| M-17 | `B/OcharoMarketing.Infrastructure/Automation/AutomationWorker.cs:14`; `L/OcharoBlog.Infrastructure/Blog/ScheduledPublishingWorker.cs:14` | `development/backend/dotnet/shapes/service/platform/startup/host-configuration.md:113` | Interval work uses a hand-written `PeriodicTimer` loop, not `AddPeriodicJob`. | S | no | Move each pass into a scoped service method; register `AddPeriodicJob`. |
| M-18 | `L/OcharoBlog.Application/Abstractions/IClock.cs:4` (10 files use it); `B/OcharoMarketing.Application/Abstractions/IClock.cs:4`, `B/.../Time/SystemClock.cs:8` (unused) | `development/backend/dotnet/core/mla/components/time.md:21,38` | Own `IClock`/`SystemClock` over `DateTimeOffset.UtcNow`, not `TimeProvider`. | M | no | Delete both; inject `TimeProvider` (already registered in Marketing). |
| M-19 | `B/.../GenerationWorker.cs:29`; `.../MarketingService.cs:44,64,86`; `B/OcharoMarketing.Api/Marketing/Controllers/MarketingController.cs:200`, `CreativeImportsController.cs:78`; `L/OcharoBlog.Application/Blog/Site/PublicationSiteService.cs:241` (7 reads) | `development/backend/dotnet/core/mla/components/time.md:23` | Production code reads `DateTimeOffset.UtcNow` directly. | S | no | Inject `TimeProvider`; call `GetUtcNow()`. |
| M-20 | `B/OcharoMarketing.Api/Marketing/MarketingExceptionMiddleware.cs:12-28`; `HostConfiguration.Extensions.cs:302` (0 `AppResult` uses, 0 handlers, both services) | `development/backend/dotnet/shapes/service/platform/responses/problem-details.md:9`; `development/repo/structure/product-conformance.md` Backend rows | Services throw `MarketingException`; middleware maps it; mediator registered with no handlers. | L | no | Return `AppResult` from handlers; drop exception middleware and unused `AddMediator`. |
| M-21 | `B/OcharoMarketing.Application/Marketing/` (30 models), `B/OcharoMarketing.Domain/Marketing/Entities/` | `development/backend/dotnet/shapes/service/architecture/clean/domain-structuring.md:10-11` | One catch-all `Marketing/` domain spans all projects; no subdomains or `Core/`. | M | no | Split into plural-subject domains (Posts, Deliveries, Assets...) with `Core/`. |
| M-22 | `B/**` 728 lines in 86 files; `L/**` 622 lines in 46 files (worst: `L/OcharoBlog.Application/Blog/Site/PublicationSiteService.cs`, 52) | `development/backend/dotnet/core/lla/notation/style/style.md:162` | Lines over the 120-character hard limit, many one-line method bodies. | M | no | Run the formatter with a 120 limit; break long statements. |

## Persistence

| id | where | rule | break | size | changes behaviour or data | fix |
|---|---|---|---|---|---|---|
| M-23 | `B/OcharoMarketing.Persistence/OcharoMarketing.Persistence.csproj`; `B/.../HostConfiguration.Extensions.cs:66-73`; `L/.../HostConfiguration.Extensions.cs:56-70`; `engineering/deployment/Dockerfile:46`; `deploy.yml:100,117` | `development/backend/dotnet/core/mla/domains/persistence/database/database.md:15` | Both products run on SQLite volumes; PostgreSQL is mandatory for products. | L | yes | Move both stores to PostgreSQL (`needs`), import SQLite data, drop `.db` volumes. |
| M-24 | `B/OcharoMarketing.Persistence/Marketing/Migrations/` (EF, 3 migrations); `L/OcharoBlog.Persistence/BlogDatabaseInitialization.cs:18-41` (`EnsureCreated` + raw DDL) | `development/backend/dotnet/core/mla/domains/persistence/migrations/migrations.md:41-42,45` | EF migrations and `EnsureCreated` instead of bespoke SQL sets (known: `conventions-and-review.md:79`). | L | yes | Convert both to bespoke SQL sets with `app migrate`; adopt existing data first. |

## Backend tests

| id | where | rule | break | size | changes behaviour or data | fix |
|---|---|---|---|---|---|---|
| M-25 | `B/tests/OcharoMarketing.Tests/`, `L/tests/OcharoBlog.Tests/` | `development/backend/dotnet/shapes/service/architecture/clean/testing.md:41-44` | Bare `{Product}.Tests` project holds every tier. | M | no | Split into `.Unit`, `.Integration`, `.E2E` projects; update CI paths. |
| M-26 | `B/tests/**`, `L/tests/**` (~1,450 `Assert.*` calls) | `development/backend/dotnet/shapes/service/architecture/clean/testing.md:22` | xUnit `Assert` instead of AwesomeAssertions. | L | no | Add AwesomeAssertions; convert assertions mechanically, per file. |
| M-27 | `L/tests/OcharoBlog.Tests/EditorialServiceTokenTests.cs:534`; `B/.../ProviderIncrementTests.cs:448`; `B/.../DeliveryWorkflowTests.cs:367` (3 hand-made clocks); `Task.Delay` waits at `EditorialAccessTests.cs:445`, `MarketingServiceTests.cs:166,201` | `development/backend/dotnet/shapes/service/architecture/clean/testing.md:23,33` | No `FakeTimeProvider`; fixed `Task.Delay` waits. | M | no | Use `FakeTimeProvider`; poll with a bound. |
| M-28 | `L/tests/OcharoBlog.Tests/**` (1 of 159 follows form); `B/tests/OcharoMarketing.Tests/**` (30 of 185) | `development/backend/dotnet/shapes/service/architecture/clean/testing.md:56` | Test names skip `{Unit}_Should{Expectation}_When{Condition}`. | M | no | Rename tests; Blog's snake_case names first. |

## Frontend

| id | where | rule | break | size | changes behaviour or data | fix |
|---|---|---|---|---|---|---|
| M-29 | `F/apps/web/package.json:20`, `F/apps/editorial/package.json:21`, `F/packages/connect/package.json:22` | `development/repo/repo-conventions.md#frontend-standard` | `@wow-two-beta/fe` pinned at 0.0.32; SDK checkout is at 0.0.41. | M | yes | Re-pin to the newest published version; follow `MIGRATION.md`; run typecheck. |
| M-30 | `F/apps/web/index.html:4-12`, `F/apps/editorial/index.html:4-12`; `public/` holds only `boot-splash.css` | `development/frontend/shapes/app/platform/document.md:25-31` | No favicon and no 180 px `apple-touch-icon.png`. | S | yes | Export icons from the Ocharo symbol into `public/`; link in the head. |
| M-31 | `F/apps/web/index.html`, `F/apps/editorial/index.html` | `development/frontend/shapes/app/platform/document.md:15` | Private apps lack the `noindex, nofollow` robots meta. | S | yes | Add `<meta name="robots" content="noindex, nofollow" />`. |
| M-32 | `F/apps/web/index.html:14`, `F/apps/editorial/index.html:14` | `development/frontend/shapes/app/platform/document.md:46-49` | `#root` is empty; no splash twin, and `src/` has no mounted splash. | S | yes | Add the static splash twin styled by `boot-splash.css`; mount a matching splash. |
| M-33 | `F/apps/web/src/presentation/marketing/compatibility/{ActionButton,MediaPicker,PickerTrigger}.vue`, `.../MarketingSelect.vue` | `development/sdk-extraction.md#remove` | App-local wrappers over SDK primitives (known for 2 of 4: `backlog.md:34-35`). | M | no | Fix the SDK gaps, re-pin, delete the wrappers; list `ActionButton` and `MarketingSelect` in the backlog gaps. |
| M-34 | `F/packages/connect/src/session.ts:114,131,197,291` | `development/frontend/core/mla/domains/data/state-and-data.md#transport` | Expected request failures throw `Error` instead of returning a `Result`. | S | no | Return `Result` from the four paths; update callers and tests. |
| M-35 | `F/apps/web/src/presentation/marketing/` (25 pages), `F/apps/web/src/application/marketing/` | `development/frontend/shapes/app/architecture/architecture.md` (layers sliced by domain) | Whole desk sits in one `marketing` slice (weak: boundaries still lint). | M | no | Cut slices by area (posts, delivery, connections, inquiries). |

## Repository layout, docs and planning

| id | where | rule | break | size | changes behaviour or data | fix |
|---|---|---|---|---|---|---|
| M-36 | `output/verification/` (17 tracked files at the repo top level) | `development/repo/structure/repo-structure.md:107`; `development/repo/structure/product-conformance.md` Repo row 1 | Tracked top-level `output/` folder beside `product/` and `engineering/`. | S | no | Move to `engineering/operations/` with `operations.md`, or untrack and regenerate. |
| M-37 | `output/verification/t02-bone.html`, `t02-charcoal.html` (4.0 MB each); `.gitattributes:1-8` | `development/repo/structure/product-conformance.md` Repo row 6; `development/repo/version-control/git.md:32` | Two 4 MB HTML files are neither ignored, regenerated nor in LFS. | S | no | Untrack and ignore, or add `*.html` evidence to LFS tracking. |
| M-38 | `F/evidence/` (49 tracked PNG/JSON files) | `development/repo/structure/repo-structure.md:65` | Verification records sit in the frontend workspace root, not `engineering/operations/`. | S | no | Move to `engineering/operations/` with a lead doc. |
| M-39 | `engineering/development/blog/` (4 md), `engineering/research/previous-work/` (17), `.../previous-work/blog/` (4), `.../previous-version-notes/` (2), `.../blog-source/` (5), `product/blog/` (5), `F/docs/` (1); plus 14 smaller `blog/` and note folders | `development/repo/structure/repo-structure.md:90` | About 8 doc folders with 2+ docs have no `{folder}.md` lead doc (known: relocation pending, `conventions-and-review.md:78`). | M | no | Add lead docs; merge `blog/` mirrors into their parents. |
| M-40 | `engineering/research/previous-work/handoff.md:1` | `development/repo/structure/product-conformance.md` Planning row 4 | A loaded `handoff.md` is left behind. | S | no | Delete after folding facts into `product/context.md`. |
| M-41 | `engineering/planning/version-track/v0.1/v0.1.md:3-106` | `planning/version-track/version-track.md:66,91-98,149` | v0.1 spans 40 days and 7 iterations; Verification is iteration 3, not last; tasks opened wrongly. | M | no | Split the version; keep Verification last; reword tasks to `Ability to`. |
| M-42 | `engineering/planning/backlog.md:16-82` | `planning/version-track/version-track.md:32-33` | Tables and prose sections do not follow `Item · Type · Notes` rows. | S | no | Rewrite the backlog groups into `Item · Type · Notes` tables. |
| M-43 | `engineering/planning/version-track/v0.1/catalogue-web-verification.md:1` | `planning/version-track/version-track.md:17,205` | A verification receipt sits in the version folder as a second planning file. | S | no | Move to `engineering/operations/`; link from the version doc. |

## Not checked

- Frontend slice linting beyond layers, per-file doc-comment rules, design spec and logo/icon rows (`product/brand/` absent; owned by ocharo-platform), `appsettings.Local.json` (never opened).

## Resolution

Each row's outcome: fixed (with the commit), excepted (with the reason) or waiting (on what).

| id | outcome | evidence |
|---|---|---|
| M-6 | waiting | a pipelines release containing `e134a47`; the orchestrator took the tag to the developer (2026-11-09) |
| M-8 | fixed | the four preview ports in the WoW2 port ledger, `wow-two-ws` `59d018c` |
| M-36 | fixed | `output/verification/` moved to `engineering/operations/verification/` |
| M-37 | fixed | `engineering/operations/**/*.html` in Git LFS; both renders stored as pointers |
| M-38 | fixed | the frontend `evidence/` moved to `engineering/operations/frontend-evidence/` |
| M-39 | fixed outside `codebase/` | lead docs added, `blog/` stubs folded, `engineering/versions/` removed; the `apps/blog/editorial-*` folders wait for the frontend pass |
| M-40 | fixed | binding facts moved to `product/context.md` § Delivery boundaries; `handoff.md` deleted |
| M-41 | fixed, split excepted | Verification last, prose cut, tasks reworded; splitting v0.1 means closing it, which needs the developer's word (version-track § Lifecycle), so the sweep's iterations sit in v0.1 |
| M-42 | fixed | every backlog group is an `Item · Type · Notes` table; the session-expiry decision moved to `architecture.md` |
| M-43 | fixed | the receipt moved to `engineering/operations/catalogue-web-verification.md` |
| M-7 | fixed | `.github/renovate.json` extends the pipelines `renovate-private` preset |
| M-10 | fixed | the Blog Dockerfile header names `ocharo-marketing.frontend-services` and the ledger port |
| M-29 | fixed | `@wow-two-beta/fe` 0.0.41 in `web`, `editorial` and `connect`; its breaking change renames helpers the product never imports |
| M-30 | fixed | favicons and a 180 px `apple-touch-icon.png` from the Ocharo symbol in both private apps |
| M-31 | fixed | `noindex, nofollow` robots meta in both private apps |
| M-32 | fixed | static splash twin in `#root` and a mounted `BootSplash` in both private apps |
| M-33 | fixed where the SDK allows | `ActionButton` deleted; `MediaPicker`, `PickerTrigger` and `MarketingSelect` wait on open SDK gaps, listed in the backlog's SDK gaps |
| M-34 | fixed | `packages/connect/src/session.ts` returns `Result` on its four expected failures |
