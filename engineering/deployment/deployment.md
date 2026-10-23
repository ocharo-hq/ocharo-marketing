# Local Ocharo Marketing runtime

*Last updated: 2026-10-24*

## Supported pilot

The supported runtime is one local operator with SQLite and private files. API binds HTTPS 8276 (HTTP 8277 stays reserved); Vue binds HTTPS 8358. A local run — `Development` without `DEPLOY_ENVIRONMENT` — keeps the loopback pilot: a loopback peer and an approved local Host are required. A deployed host is public without sign-in until the Ocharo identity server lands ([Wheelhouse](#wheelhouse)); any other host refuses startup. Writes require the same origin or the explicit local Vite origin on port 8358; unrelated browser origins are rejected. Connected mode introspects Platform membership for private access and requires the configured organization binding.

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
`appsettings.Development.json` holds deployable defaults only: connected mode and an empty organization binding.
Machine values belong in the local file:

```json
{
  "PlatformIntegration": { "BaseUrl": "https://localhost:8264", "OrganizationId": "<Desk organization id>" },
  "Marketing": { "PlatformBaseUrl": "https://localhost:8264" }
}
```

Without it the run stays in unconfigured connected mode: health and technical status answer, private operations stay
unavailable, catalogue sync falls back to the retained Catalogue pilot (`Marketing:CatalogueBaseUrl`) and published
product verification for enquiries is unavailable. Desk SSO (`Identity:Sso:*`) and provider credentials stay in
environment or local secret configuration. A deployed target's settings mount as `appsettings.Target.json`.

On this host Node 24 lives at `/Users/max/.nvm/versions/node/v24.11.0/bin`. The system's default Node 20 does not meet the workspace engine requirement. Keep runtimes attached to managed sessions and inspect readiness before handing over their URLs.

## Build and package

The API's default `BuildSpa` target installs from the frozen lockfile, typechecks/builds Vue, copies the complete `apps/web/dist` to `wwwroot`, and refreshes publish content. Set `BuildSpa=false` only when another verified stage supplies matching SPA output. Backend-only tests may use that switch while frontend changes are tested independently.

The build removes the exact evaluated `wwwroot` content items before replacing Vite output, then includes the current files. This keeps removed asset hashes out of incremental build and publish manifests. Two consecutive stale-file regression builds and publishing passed with the existing `obj` cache; evidence is in `output/verification/incremental-spa-build.json`.

Fonts are embedded for deterministic offline rendering; their notice/license files accompany output. Playwright Chromium is required for PNG/PDF rendering. Provider calls are not needed to render existing product assets.

`deployment/Dockerfile` and `deployment/blog/Dockerfile` build the deployed images ([Wheelhouse](#wheelhouse)). Outside a deployment, Production startup still refuses unless `Marketing:PublicAccess` is true.

## Durable data

By default the API keeps its SQLite database and private blobs under `App_Data/`; neither belongs in source control or public `wwwroot`. Startup uses the SDK EF migration runner, imports missing source records, and only then starts the durable worker. Existing drafts and imported bytes are preserved on restart.

Draft revisions also preserve title, subtitle, fabric selection, scale and placement for reopening the same editable card. Approved export ZIPs are keyed by draft and revision. Editing invalidates current approval without changing previous revision contents or schedule snapshots. Local render jobs use frozen HTML; expired rendering leases may retry. Interrupted external image work becomes unknown and requires provider inspection before another paid operation.

## Providers and publishing

Provider configuration is server-side under `Providers:OpenAi`, `Providers:Gemini`, `Providers:Telegram`, `Providers:X`, `Providers:Instagram`, and `Providers:WhatsApp`. All provider options default disabled. Store credentials in environment or local secret configuration, never tracked files. HTTP provider logging and redirects are disabled.

Image and text preparation run only after explicit configuration and a user command. No live provider was invoked during implementation verification. Social adapters exist, but schedules remain manual planned entries in this pilot. `Marketing:EnableLivePublishing` defaults false and does not enable an unimplemented delivery worker. Configured credentials are not proof of provider connectivity or publication.

## Wheelhouse

`deploy.yml` (descriptor 2, `product: ocharo`) and `.github/workflows/publish-docker-image.yml` (the shared
`delivery.yml` of `wow-two-platform.pipelines`, pinned to `v0.10.0`, and the only CI file) put Marketing in Wheelhouse
beside Platform and Studio. The owner decided on 2026-10-24 to deploy both hosts publicly without sign-in until the
Ocharo identity server lands; their stores hold nothing valuable yet.

| Service | Image | Serves | Site | Volume at `/data` |
|---|---|---|---|---|
| `marketing` | `deployment/Dockerfile` | Marketing API and the operator desk (`apps/web`) | `marketing`, public | `marketing-data`: `ocharomarketing.db`, private blobs |
| `blog` | `deployment/blog/Dockerfile` | Editorial API: the public renderer (`/`, `/journal`, `/topics`, `/atlas`, `/wear`, `/catalogue`), the `/studio/` editor and its API | `blog`, public | `blog-data`: `ocharo-blog.db`, blobs |

- `Marketing:PublicAccess` and `Blog:PublicAccess` (boolean, `deploy.yml` default `true`) open each host's
  `LocalPilotBoundaryMiddleware` to every caller. Unset, a deployed host (`DEPLOY_ENVIRONMENT`) is public and a local run
  keeps loopback; Marketing refuses to start on a host that is neither.
- Both run as .NET Production, whose `PlatformIntegration:Mode` defaults to `local`: private routes and writes answer
  without sign-in. `connected` mode with `PlatformIntegration:BaseUrl` and `OrganizationId`, or Desk SSO, restores it.
- Cross-origin writes stay refused. Behind the ingress each host trusts forwarded headers from loopback and
  `Deployment:TrustedProxies` only.
- Wheelhouse derives the two required settings, `AllowedHosts` and `Deployment:TrustedProxies`. Optional `url` keys:
  `Marketing:CatalogueBaseUrl`, `Marketing:BlogBaseUrl`, `Blog:CatalogueBaseUrl`, `Blog:BrandBaseUrl`,
  `Blog:MarketingBaseUrl` and the `OcharoApps:*` menu links; unset, cross-app reads keep their `localhost` defaults and fail.
- The Marketing image has no browser, so card PNG/PDF rendering is unavailable on a deployed host; providers and live
  publishing stay off by default.
- Atlas, Wear, Blog and Catalogue static exports still come from a running Editorial (`scripts/build-public.py`); on a
  deployed host the Editorial's public renderer serves published revisions at runtime. CI builds them from fixtures.
- CI runs each backend's tests, both frontends' scripts, the fixture builds and the compose validation, each when its
  paths change; publication builds the changed images as `ghcr.io/ocharo-hq/ocharo-marketing/{marketing,blog}`, and a
  `main` build releases `vX.Y.Z`.
- Once the identity server lands, put both hosts behind sign-in before storing anything valuable. Noindex stays until
  the owner's launch decision.
