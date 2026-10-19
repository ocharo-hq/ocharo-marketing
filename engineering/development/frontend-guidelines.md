# ventures.ocharo-marketing — Frontend Development Guidelines

*Last updated: 2026-10-19*

Defer to `wow-two-ws/conventions/development/frontend/`.

## Workspace

- Frontend root: `engineering/codebase/ocharo-marketing.frontend-services/`.
- Product app: private `@ocharomarketing/web` under `apps/web/`.
- Runtime: Node `24.11.0` from `.nvmrc`; package manager: `pnpm@10.33.2`.
- Shared packages use `packages/*` only when there is shared code; no runtime federation is configured.
- App source uses `bootstrap`, `presentation`, `application`, `domain`, and `integration` layers.
- The marketing API returns camelCase domain DTOs and safe ProblemDetails failures; integration helpers validate and preserve the useful error detail.
- A 401 never becomes an error toast. The first 401 or signed-out status read of a signed-in session publishes the SDK's one neutral `feedbackSessionExpiry` notice through `subscribeIntegrationExpired` (`packages/connect`); the page and its unsaved edits stay, and Reconnect opens Desk sign-in in a new tab returning to the page where the session expired.
- Contract source: `engineering/architecture/marketing-contract.md`. Provider flags, configured accounts and observed metrics are server-owned.

## Commands

Run from the frontend root:

```sh
nvm use
pnpm install --frozen-lockfile
pnpm dev
```

The Vite app uses HTTPS on `8358` and proxies `/api` to the backend HTTPS profile on `8276`.
Development and browser checks use trusted HTTPS; configure `VITE_API_CA_PATH` for the backend certificate.
`VITE_API_TARGET` can select a running local backend for those checks; the default is `https://localhost:8276`.

```sh
pnpm typecheck
pnpm test
pnpm format:check
pnpm lint
pnpm build
```

Typecheck includes the Vue SFC compiler. Tests cover marketing API errors, composer revisions and asynchronous edits,
explicit queue confirmation, and architecture boundaries. Browser evidence lives in `evidence/`; it distinguishes
local UI behavior from fake-provider tests and actual external acceptance.

## Delivery

`pnpm build` produces `apps/web/dist/`. `pnpm deploy` rebuilds and replaces the generated API `wwwroot/` artifact.
Run `deploy` only when replacing that generated frontend is intended.
Docker installs from the workspace lockfile and copies the same `apps/web/dist/` artifact into the .NET image.
API calls remain same-origin in development and production.
