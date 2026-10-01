# Ocharo Marketing

Ocharo's recognition, editorial publishing, distribution and lead platform.

Marketing contains the operator desk, the revisioned editorial editor, and four independently styled public applications: Atlas, Wear, Blog and Catalogue. Both adopted Blog sources now use the same revisioned publication lifecycle. The original thirty authored stories and routes remain available; research imports enter as drafts.

Platform owns canonical released identity and product facts. Studio packages enter Marketing as integrity-checked creative drafts; source approval does not approve product claims or publishing. Inbound consented enquiries persist in the Marketing SQLite inbox.

## Development

Use Node 24, pnpm 10.33.2 and .NET 10. All frontend source is under `engineering/codebase/ocharo-marketing.frontend-services`:

| App | Command | Artifact / route |
|---|---|---|
| Marketing desk | `pnpm dev` / `pnpm build` | `apps/web/dist`, HTTPS 8358 |
| Editorial desk | `pnpm dev:editorial` / `pnpm build:editorial` | `apps/editorial/dist`, HTTPS 8284 `/studio/` |
| Atlas | `pnpm build:atlas` | `apps/atlas/dist` |
| Wear | `pnpm build:wear` | `apps/wear/dist` |
| Blog | `pnpm build:blog` | `apps/blog/dist` |
| Catalogue | `pnpm build:catalogue` | `apps/catalogue/dist` |

Public builds consume authenticated published exports. They accept `OCHARO_EXPORT_ARCHIVE`, or `OCHARO_EDITORIAL_URL`, a current `OCHARO_EXPORT_TICKET`, and optional `OCHARO_CA_FILE`. The old static manuscript compiler remains `apps/blog` → `pnpm build:archive` for archival comparison only.

The APIs remain separate local processes: Marketing HTTPS 8276 and Editorial HTTPS 8280. Configure `PlatformIntegration:Mode=connected`, an HTTPS `BaseUrl`, and the owning `OrganizationId` on each. Missing or mismatched bindings fail private operations. Explicit `local` mode is confined to loopback. The current local host boundary is retained; enabling hosted deployment remains a separate decision.

Use `pnpm install --frozen-lockfile`, each app's build/test command, and `dotnet test` in each backend root. Local TLS can use an existing trusted pair through `OCHARO_TLS_CERT` and `OCHARO_TLS_KEY`; proxies verify TLS and Node trusts the supplied CA through `NODE_EXTRA_CA_CERTS`. Native trust changes require user action.

[Connected build and review guide](engineering/planning/connected-build.md) records configuration, contracts and verification. [Workspace ownership](../../docs/architecture.md) defines repository boundaries.

## Git LFS

Images, PDFs, ZIPs and fonts are tracked with Git LFS (`.gitattributes`). Install git-lfs before cloning or they check out as pointer text.

## Planning and contracts

- [Local app review](engineering/planning/review-guide.md)
- [Exact setup and acceptance actions](engineering/planning/external-setup.md)
- [Product](product/product.md)
- [Shared API and lane contract](engineering/architecture/marketing-contract.md)
- [Provider research](engineering/research/providers.md)
- [Dashboard and creation workflow](engineering/research/operator-experience.md)
- [SDK extraction inventory](engineering/architecture/sdk-extraction.md)

## Provider setup

Credentials remain server-side. The app distinguishes not-configured, configured, and verified evidence. Live publishing defaults off; credentials alone do not make an old plan executable. No paid generation or external publishing is performed by tests. See provider research for official contracts and access requirements.

## Assets

The three seed product images are existing Ocharo generated front cutouts, copied with SHA-256 provenance for local review. Canonical business records remain in Maftuna's workspace. Bundled Cormorant Garamond and Manrope fonts retain their SIL Open Font License files and source hashes under the card renderer's Assets folder.
