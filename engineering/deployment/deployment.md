# Local Ocharo Marketing runtime

*Last updated: 2026-11-04*

## Supported pilot

The supported runtime is PostgreSQL and private files behind sign-in. API binds HTTPS 8276 (HTTP 8277 stays reserved); Vue binds HTTPS 8358. Marketing signs in through Ocharo's Identity as BFF client `marketing`, on this machine the instance at `https://localhost:8404`. Private routes need membership of `Marketing:OrganizationId`, writes `marketing:edit` and publication actions `marketing:approve`; `/api/public/*` and the technical routes stay anonymous. Writes require the same origin or the explicit local Vite origin on port 8358; unrelated browser origins are rejected.

Run the backend from its solution directory in a managed terminal:

```bash
dotnet watch --non-interactive --project OcharoMarketing.Api/OcharoMarketing.Api.csproj run --launch-profile https
```

Run the frontend from `ocharo-marketing.frontend-services/` with Node 24.11.0 and pnpm 10.33.2 on PATH:

```bash
pnpm dev
```

## Local settings

The `https` launch profile sets `ASPNETCORE_ENVIRONMENT=Development` and `LOCAL_SETTINGS=true`, so the run reads
`OcharoMarketing.Api/appsettings.Local.json` after `appsettings.Development.json` through the SDK's `AddSettingsFiles`.
The file is gitignored and excluded from image builds; tests, review scripts and containers never read it.
`appsettings.Development.json` holds deployable defaults only.
Machine values belong in the local file:

```json
{
  "Identity": {
    "Client": {
      "Authority": "https://localhost:8404",
      "ClientId": "marketing",
      "ClientSecret": "<the marketing client's secret>",
      "CookieName": "__Host-ocharo-marketing",
      "PostSignOutRedirectPath": "/"
    }
  },
  "Marketing": {
    "OrganizationId": "<Ocharo's organization id in that Identity>",
    "PlatformBaseUrl": "https://localhost:8264"
  }
}
```

Startup refuses without the client block or `Marketing:OrganizationId`. Without `Marketing:PlatformBaseUrl`, catalogue
sync falls back to the retained Catalogue pilot (`Marketing:CatalogueBaseUrl`) and published product verification for
enquiries is unavailable. Provider credentials stay in environment or local secret configuration. A deployed target's
settings mount as `appsettings.Target.json`.

On this host Node 24 lives at `/Users/max/.nvm/versions/node/v24.11.0/bin`. The system's default Node 20 does not meet the workspace engine requirement. Keep runtimes attached to managed sessions and inspect readiness before handing over their URLs.

## Build and package

The API's default `BuildSpa` target installs from the frozen lockfile, typechecks/builds Vue, copies the complete `apps/web/dist` to `wwwroot`, and refreshes publish content. Set `BuildSpa=false` only when another verified stage supplies matching SPA output. Backend-only tests may use that switch while frontend changes are tested independently.

The build removes the exact evaluated `wwwroot` content items before replacing Vite output, then includes the current files. This keeps removed asset hashes out of incremental build and publish manifests. Two consecutive stale-file regression builds and publishing passed with the existing `obj` cache; evidence is in `output/verification/incremental-spa-build.json`.

Fonts are embedded for deterministic offline rendering; their notice/license files accompany output. Playwright Chromium is required for PNG/PDF rendering. Provider calls are not needed to render existing product assets.

`deployment/Dockerfile` and `deployment/blog/Dockerfile` build the deployed images ([Wheelhouse](#wheelhouse)). Startup needs `Identity:Client:*` and `Marketing:OrganizationId`, which `deployment/docker-compose.yml` does not set.

## Durable data

Both APIs keep their state in PostgreSQL: Marketing in schema `marketing`, Blog in `blog`, each with its sign-in sessions in `{app}_cache` and its Data Protection key ring in its own schema. Bespoke SQL sets own both schemas (`Persistence/Migrations/`); a local run applies them at boot, a deployed boot only checks levels, and the deploy's `migrate` step applies them. A local run needs `DatabaseSettings:ConnectionString` in `appsettings.Local.json` and Identity's set in the same database, since both sets key into Identity's organizations and people. Private blobs stored before WoW2 Files stay under `App_Data/` locally and `/data` deployed; neither belongs in source control or public `wwwroot`.

Draft revisions also preserve title, subtitle, fabric selection, scale and placement for reopening the same editable card. Approved export ZIPs are keyed by draft and revision. Editing invalidates current approval without changing previous revision contents or schedule snapshots. Local render jobs use frozen HTML; expired rendering leases may retry. Interrupted external image work becomes unknown and requires provider inspection before another paid operation.

## Providers and publishing

Provider configuration is server-side under `Providers:OpenAi`, `Providers:Gemini`, `Providers:Telegram`, `Providers:X`, `Providers:Instagram`, and `Providers:WhatsApp`. All provider options default disabled. Store credentials in environment or local secret configuration, never tracked files. HTTP provider logging and redirects are disabled.

Image and text preparation run only after explicit configuration and a user command. No live provider was invoked during implementation verification. Social adapters exist, but schedules remain manual planned entries in this pilot. `Marketing:EnableLivePublishing` defaults false and does not enable an unimplemented delivery worker. Configured credentials are not proof of provider connectivity or publication.

## Wheelhouse

`deploy.yml` (descriptor 2, `product: ocharo`) and `.github/workflows/publish-docker-image.yml` (the shared
`delivery.yml` of `wow-two-platform.pipelines`, pinned to commit `e134a47`, and the only CI file) put Marketing in Wheelhouse
beside Platform and Studio. Both hosts sign in through Ocharo's Identity, which `ocharo-platform`'s descriptor deploys.

| Service | Image | Serves | Site | Volume at `/data` |
|---|---|---|---|---|
| `marketing` | `deployment/Dockerfile` | Marketing API and the operator desk (`apps/web`) | `marketing`, behind sign-in | `marketing-data`: blobs stored before Files; the SQLite store until imported |
| `blog` | `deployment/blog/Dockerfile` | Editorial API: the public renderer (`/`, `/journal`, `/topics`, `/atlas`, `/wear`, `/catalogue`), the `/studio/` editor and its API | `blog`: the renderer public, the editor behind sign-in | `blog-data`: blobs stored before Files; the SQLite store until imported |

- Marketing is Identity client 3 and Editorial client 4, at fixed indexes in `ocharo-platform`'s `deploy.yml`. Each host
  links `Identity:Client:Authority` and `ClientSecret` with `from` to keys that descriptor declares.
- `blog` links `Identity:Resource:Uri` to its own site, the audience of its export tokens, and `Blog:DeskBaseUrl` to Desk's site.
- Owner keys, per environment: `Marketing:OrganizationId` and `Blog:OrganizationId`, Ocharo's organization id in that
  environment's Identity; on `identity`, each client's `RedirectUris:0` `{site}/api/identity/callback` and
  `PostSignOutRedirectUris:0` `{site}/`. A deploy refuses without them.
- Private routes need membership of the bound organization, writes `{marketing,editorial}:edit`, publication actions
  `{marketing,editorial}:approve`. `/api/public/*`, the technical routes and the Blog's published pages and media stay
  anonymous; `/preview` and unpublished media stay private.
- Both services `need` PostgreSQL; Wheelhouse fills `DatabaseSettings:ConnectionString` and, for the `migrate` step,
  `Migrations:ConnectionString` from the target's `ocharo_{env}`. Sessions and key rings live there, so a restart or
  deploy keeps everyone signed in.
- Revocation signals: each host links `Identity:Client:SignalUrl`, and `blog` also `Identity:Resource:SignalUrl`, with
  `from` to the owner keys `ocharo-platform`'s `identity` declares. Marketing's generation jobs record who asked: losing
  access stops one, and signing out leaves it running. Editorial's token deny list lives in memory, so a restart forgets it
  and a token's own lifetime stays the limit.
- Editorial calls Desk's background removal as itself with an Identity service token (`desk.remove-background`), having
  checked the person's `editorial:edit` first. `Blog:DeskBaseUrl` links to Desk's site; unset, removal reads unavailable.
- Cross-origin writes stay refused. Behind the ingress each host trusts forwarded headers from loopback and
  `Deployment:TrustedProxies` only.
- Wheelhouse derives the two required settings, `AllowedHosts` and `Deployment:TrustedProxies`. The `OcharoApps:*` menu
  links are the optional `url` keys. The cross-app origins link with `from` to each app's site: `Marketing:CatalogueBaseUrl`,
  `Marketing:BlogBaseUrl`, `Blog:CatalogueBaseUrl`, `Blog:BrandBaseUrl` and `Blog:MarketingBaseUrl`.
- The Marketing image has no browser, so card PNG/PDF rendering is unavailable on a deployed host; providers and live
  publishing stay off by default.
- Atlas, Wear, Blog and Catalogue static builds take a published export: an archive a signed-in editor downloaded
  (`OCHARO_EXPORT_ARCHIVE`), or a live fetch with the `public-build` client's service token (`editorial.export`,
  `OCHARO_EXPORT_CLIENT_SECRET`), which nothing stores in CI. On a deployed host the Editorial's public renderer serves
  published revisions at runtime. CI builds them from fixtures.
- CI runs each backend's tests, both frontends' scripts, the fixture builds and the compose validation, each when its
  paths change; publication builds the changed images as `ghcr.io/ocharo-hq/ocharo-marketing/{marketing,blog}`, and a
  `main` build releases `vX.Y.Z`.
- Noindex stays until the owner's launch decision.

## PostgreSQL move

Each environment moves its SQLite data once, after the first deploy that brings PostgreSQL (`v0.1.11` on dev). The
deploy's `migrate` step creates the empty schemas; until the import runs, each app answers its data routes and the
public Blog 503 `import_pending`, and its seeds and workers skip their work. Run per service, from that release's image,
with the service's rendered settings and its `/data` volume mounted:

| Step | Command | Expect |
|---|---|---|
| 1 | `dotnet OcharoMarketing.Api.dll import-sqlite --dry-run` | exit 0, `import-result` outcome `verified`, every table `matches` |
| 2 | `dotnet OcharoMarketing.Api.dll import-sqlite` | exit 0, outcome `imported`, the store renamed `*.imported-<UTC>.db` |
| 3 | `dotnet OcharoBlog.Api.dll import-sqlite --dry-run` | exit 0, `verified` |
| 4 | `dotnet OcharoBlog.Api.dll import-sqlite` | exit 0, `imported` |
| 5 | restart both services | the Files backfill runs; data routes answer |

- The verb copies every table in one transaction, compares per-table row counts and SHA-256s of the canonical rows
  read back from PostgreSQL, and commits only on a full match; its last line `import-result {json}` carries counts and
  hashes, never row data.
- Exit `0` done, nothing to import, or already imported; `1` refused (a non-empty target holding other rows); `2`
  failed, rolled back (a mismatch, or an organization or person id Identity does not hold, named by table, column and
  count).
- The retired store stays on the volume as the rollback copy until the volume is dropped after the Files migration.
- Writes the old release accepts between the `migrate` step and the restart stay only in the SQLite store; the import
  reads the store after the restart, so they are copied too.
- Prod runs the same steps after its deploy, with the same verbs.
