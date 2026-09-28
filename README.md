# Ocharo Marketing

Maftuna's marketing desk for product cards, localized copy, channel planning, and observed response.

## Current build

Independent temporary venture; no Brand Workspace runtime dependency. The first pilot is local and single-operator. Implementation and verification are tracked in [r0.1](engineering/planning/rough-track/r0.1/r0.1.md) and [handoff](engineering/planning/handoff.md).

Vue 3 / TypeScript / Tailwind frontend, .NET 10 Clean Architecture backend, SQLite persistence, durable background work, deterministic HTML card rendering, and separately bounded provider brokers. Existing product cutouts are imported as draft-review inputs; no synthetic engagement is presented as real.

## Development

Use Node 24 and pnpm 10.33.2. The frontend workspace is `engineering/codebase/ocharo-marketing.frontend-services`, with the app in `apps/web`. Run `pnpm install --frozen-lockfile`, `pnpm typecheck`, `pnpm test`, and `pnpm build` from that workspace. Backend source is in `engineering/codebase/ocharo-marketing.backend-services`; use its `OcharoMarketing.BackendServices.slnx` solution.

Allocated local ports: API HTTPS 8276 / HTTP 8277; frontend HTTPS 8242. Runtime startup and actual verification evidence are recorded in [handoff](engineering/planning/handoff.md), not inferred from this README. The renderer uses Microsoft.Playwright 1.63.0 and its matching Chromium; install browser dependencies before browser tests on another machine.

## Ocharo platform links

- **Ocharo Catalogue** (`ventures.ocharo-catalogue`) is the product source: *Avtomatika → Katalogdan yangilash* (or *Mahsulotlar → Ocharo Katalogdan yangilash*) imports active products, copy, prices and cutouts from `Marketing:CatalogueBaseUrl` (default `http://localhost:8279`).
- **Ocharo Blog** (`ventures.ocharo-blog`) stories can be announced as draft posts from its RSS (`Marketing:BlogBaseUrl`, default `http://localhost:8281`).
- **Posting slots** plan approved drafts into weekly times; they never publish by themselves.

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
