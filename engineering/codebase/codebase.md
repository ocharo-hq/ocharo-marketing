# codebase/

The code lives in `ocharo-marketing.backend-services/` (.NET) and `ocharo-marketing.frontend-services/` (Vue).

The frontend is a private pnpm workspace. Its product app is `apps/web/`; optional shared code uses `packages/`.
Installation and root `dev`, `typecheck`, `test`, `build`, and `deploy` commands run from that workspace root.
The app owns its dependencies, Vite and TypeScript configuration, five-layer `src/`, tests, and `dist/` output.
`scripts/deploy.mjs` copies `apps/web/dist/` to `ocharo-marketing.backend-services/OcharoMarketing.Api/wwwroot/`.

Rules: wow-two-ws/conventions/development/repo/structure/repo-structure.md
