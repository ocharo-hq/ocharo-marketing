# Blog source adoption

*Updated: 2026-09-29*

Both existing Blog implementations now belong to Marketing. The thirty-story manuscript seed,
revisioned editorial storage and public exports now converge in the adopted Editorial API.
Public deployment remains a separate decision.
The public name remains **Ocharo Blog**.

## Entrypoints

All paths below are relative to this repository.

| Source | Current path | Local runtime |
|---|---|---|
| Static Blog | `engineering/codebase/ocharo-marketing.frontend-services/apps/blog` | `pnpm build:blog`, then `pnpm preview:blog` from Marketing frontend root; loopback 8774 |
| Blog API and public renderer | `engineering/codebase/ocharo-blog.backend-services` | `dotnet run --project OcharoBlog.Api --launch-profile https`; HTTPS 8280, HTTP 8281 |
| Blog editor | `engineering/codebase/ocharo-marketing.frontend-services/apps/editorial` | `pnpm install --frozen-lockfile`, then `pnpm dev:editorial`; HTTPS 8284, `/studio/` |
| Marketing operator | `engineering/codebase/ocharo-marketing.frontend-services/apps/web` | Existing root commands; HTTPS 8358, API 8276/8277 |

The adopted Blog backend solution/project names remain a migration exception.
The old frontend root forwards commands to Marketing; it owns no application source.
Marketing frontend commands still target its existing operator app unless `:blog` is explicit.

## Source and content

- Static authored CSS and five images now live in `apps/blog/public/`; its build copies them to ignored `dist/`.
- The 30 stories, three topics, route structure and original editorial material remain preserved.
- `apps/blog/editorial-research/` and `editorial-work/` stay separate from generated output.
- Research illustrations retain their attribution manifests and unpublished status.
- Modern Blog seed JSON remains a historical import; its database may contain newer edits.
- `engineering/scripts/blog/import-journal.mjs` is an explicit seed conversion command, never startup migration.
- `product/blog/` and each engineering area's `blog/` preserve donor documentation.
- `engineering/research/blog-source/` preserves donor root/instruction/codebase snapshots as historical evidence.

## Data and deployment boundaries

The copied `ocharo-blog.backend-services/OcharoBlog.Api/App_Data/` remains ignored, as do all
SQLite files, generated host output and local configuration. Both donor and destination data
are preserved before archival; do not reseed, merge or publish these stores.
Development seed assets resolve relative to the Blog API content root and use the tracked
static `public/assets/` directory. Existing absolute overrides continue to work.

The modern Blog host remains loopback-only; connected private routes require current Platform
tickets and an exact organization binding. Cross-origin writes remain rejected. Public builds
consume published revisions; draft previews require the same private boundary. Noindex remains
until the owner's launch decision. Editorial research imports stay drafts.

Marketing's deployment script replaces its API's entire `wwwroot`. Never deploy either Blog
there. Blog's existing companion deployment script targets only its own `wwwroot/studio/`.
`engineering/deployment/blog/deploy.source.yml` is an inactive source descriptor retained for
future deployment integration; it is not registered in Marketing's active deployment descriptor.
No donor CI or hosting automation is activated by this adoption.

## Verification

Check source/data checksums, generated static route parity, and exclusion of local data before
committing. Modern Blog backend tests and editor typecheck/tests exercise relocated imports.
Current full-build evidence and commands are in [connected build](../planning/connected-build.md).
Public launch remains separate from successful builds.
