# Local Ocharo Marketing runtime

*Last updated: 2026-10-17*

## Supported pilot

The supported runtime is one local operator with SQLite and private files. API binds HTTPS 8276 and HTTP 8277; Vue binds HTTPS 8358. Development mode, a loopback peer and an approved local Host are required. Writes require the same origin or the explicit local Vite origin on port 8358; unrelated browser origins are rejected. Connected mode introspects Platform membership for private access and requires the configured organization binding. Remote hosting remains outside the current loopback review boundary.

Run the backend from its solution directory in a managed terminal:

```bash
dotnet watch --non-interactive --project OcharoMarketing.Api/OcharoMarketing.Api.csproj run --launch-profile https
```

Run the frontend from `ocharo-marketing.frontend-services/` with Node 24.11.0 and pnpm 10.33.2 on PATH:

```bash
pnpm dev
```

On this host Node 24 lives at `/Users/max/.nvm/versions/node/v24.11.0/bin`. The system's default Node 20 does not meet the workspace engine requirement. Keep runtimes attached to managed sessions and inspect readiness before handing over their URLs.

## Build and package

The API's default `BuildSpa` target installs from the frozen lockfile, typechecks/builds Vue, copies the complete `apps/web/dist` to `wwwroot`, and refreshes publish content. Set `BuildSpa=false` only when another verified stage supplies matching SPA output. Backend-only tests may use that switch while frontend changes are tested independently.

The build removes the exact evaluated `wwwroot` content items before replacing Vite output, then includes the current files. This keeps removed asset hashes out of incremental build and publish manifests. Two consecutive stale-file regression builds and publishing passed with the existing `obj` cache; evidence is in `output/verification/incremental-spa-build.json`.

Fonts are embedded for deterministic offline rendering; their notice/license files accompany output. Playwright Chromium is required for PNG/PDF rendering. Provider calls are not needed to render existing product assets.

The scaffold Docker recipe is an unverified packaging reference. This local pilot deliberately rejects Production startup; do not treat that recipe as a remote deployment path. Remote hosting requires authentication, persisted data-protection keys, secure callbacks and explicit delivery configuration first.

## Durable data

By default the API keeps its SQLite database and private blobs under `App_Data/`; neither belongs in source control or public `wwwroot`. Startup uses the SDK EF migration runner, imports missing source records, and only then starts the durable worker. Existing drafts and imported bytes are preserved on restart.

Draft revisions also preserve title, subtitle, fabric selection, scale and placement for reopening the same editable card. Approved export ZIPs are keyed by draft and revision. Editing invalidates current approval without changing previous revision contents or schedule snapshots. Local render jobs use frozen HTML; expired rendering leases may retry. Interrupted external image work becomes unknown and requires provider inspection before another paid operation.

## Providers and publishing

Provider configuration is server-side under `Providers:OpenAi`, `Providers:Gemini`, `Providers:Telegram`, `Providers:X`, `Providers:Instagram`, and `Providers:WhatsApp`. All provider options default disabled. Store credentials in environment or local secret configuration, never tracked files. HTTP provider logging and redirects are disabled.

Image and text preparation run only after explicit configuration and a user command. No live provider was invoked during implementation verification. Social adapters exist, but schedules remain manual planned entries in this pilot. `Marketing:EnableLivePublishing` defaults false and does not enable an unimplemented delivery worker. Configured credentials are not proof of provider connectivity or publication.

## Wheelhouse

Marketing has no `deploy.yml` and no `publish-docker-image.yml`, so Wheelhouse neither builds nor deploys it
(`deploy: none` in the WoW2 catalog). Each blocker below must clear before a descriptor is added:

- The Marketing API refuses startup outside Development; both it and the Blog API reject non-loopback requests.
- Atlas, Wear, Blog and Catalogue are exported from a running Editorial (`scripts/build-public.py`), so CI cannot
  build them from source; a deployable image serves published revisions at runtime instead.
- Public launch of Blog and the public apps remains the owner's decision.
