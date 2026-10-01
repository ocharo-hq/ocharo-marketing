# Brand Workspace — four-vector editorial structure audit

> Historical research snapshot. Repository paths, line references and proposals describe the original inspection; current ownership and entrypoints are in [Blog adoption](../../../../../../architecture/blog-adoption.md).

*Last updated: 2026-09-29 12:23 PM GMT+5*

## Scope and verdict

Read-only source audit of Brand Workspace for the four accepted editorial vectors: Uzbek textile stories; what to wear, where and when; Ocharo news; and catalogue. The user deferred merging the two existing Blog implementations. No app source, platform record, server, Git index, commit or public site was changed. This file is the audit deliverable.

**Brand Workspace currently provides operator preparation and document editing, not a public article CMS.** It can supply useful platform foundations, but choosing it as the immediate home for all four public experiences would require new editorial and public-delivery work. Four editorial vectors need distinct content structures and reader experiences; they do not inherently require four databases, backends or repositories.

The current business record accepts **Brand Workspace's Brand module as canonical identity owner**, keeps **Studio separate**, and leaves wider repository consolidation unadopted. This ownership decision is ahead of Brand Workspace's identity implementation. The existing standalone Brand app must eventually reconcile with that owner; it should not become a second lasting authority. [Current direction](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform.md:9).

This supersedes the earlier naming research's suggestion that the standalone Brand app should remain the canonical identity implementation. The naming vocabulary remains useful; implementation ownership follows the newer accepted direction.

## Repository and evidence boundary

- Verified Git root: `/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace`.
- Verified `HEAD`: `5f8e519`, `feat: added tenant-safe content preparation and manual exports`.
- The checkout contains existing staged editor/frontend/documentation work beyond that commit. This audit reads the current checkout, including those staged files, rather than presenting them as committed delivery.
- No unstaged or untracked changes were reported inside the target repository at the end of this inspection. The audit file lives in the enclosing business workspace.
- Read the Brand Platform instructions, target repository instructions and required upstream engineering conventions. External Studio and SDK repositories were not treated as editable scope.
- Inspected code, routes, domain contracts, frontend composition, worker composition, product context and architecture. No build, tests or browser/runtime verification was run.

The source paths below are exact current files with verified line references. “Implemented” means source is present and wired; runtime success is not asserted.

## What current code implements

| Capability | Verified source evidence | Consequence for the four vectors |
|---|---|---|
| Authenticated organisations and scoped business routes | [PreparationConfiguration.cs:26](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Api/Preparation/PreparationConfiguration.cs:26) requires authorization and applies organisation access filtering | Reusable operator boundary; public reading should receive a separate explicitly public projection |
| Minimal brand record | [Brand.cs:4](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Domain/Preparation/Models/Brand.cs:4) contains ID, organisation ID, name and timezone | Canonical identity ownership is accepted, but releases, logo slots and public kit contracts are still absent here |
| Private media upload and retrieval | [PreparationConfiguration.cs:34](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Api/Preparation/PreparationConfiguration.cs:34) places media behind the same authenticated organisation group | Existing assets cannot simply be inserted into anonymous articles using their private API addresses |
| Versioned caption drafts | [Draft.cs:4](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Domain/Preparation/Models/Draft.cs:4) stores title, caption, language, destination, source notes, media and review state | Useful campaign preparation; no article blocks, slug, author, place taxonomy or public revision model |
| Review and approval transitions | [PreparationConfiguration.cs:51](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Api/Preparation/PreparationConfiguration.cs:51) maps request review, approve, revoke and export actions | Can inform later editorial workflows; approval is not publication |
| Manual ZIP snapshots | [ManualPackageBuilder.cs:35](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Infrastructure/Preparation/Services/ManualPackageBuilder.cs:35) writes caption and manifest; receipt fields remain null | A package handoff, not a website or provider publishing workflow |
| Durable design documents and revisions | [DesignDocumentConfiguration.cs:34](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Api/DesignDocuments/DesignDocumentConfiguration.cs:34) maps create/read/save/archive/revisions | Suitable for saved print/card design editions; not automatically a product or article store |
| Catalogue and business-card document kinds | [DesignDocumentValidationService.cs:18](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Infrastructure/DesignDocuments/Services/DesignDocumentValidationService.cs:18) permits only those two kinds | Existing catalogue means a designed publication; it is not a public product catalogue API |
| Operator design/edit/export interface | [DesignEditorPage.vue:116](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.frontend-services/apps/web/src/presentation/workspace/DesignEditorPage.vue:116) imports JSON and exports JSON/PDF/PNG | Preserve this workflow when considering future public catalogue browsing |
| Static imported product and brand templates | [Templates.ts:2](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.frontend-services/apps/web/src/domain/workspace/Templates.ts:2) imports catalogue/card JSON, products and assets | These are template inputs, not a live shared catalogue or identity API |
| Current operator navigation | [WorkspacePage.vue:73](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.frontend-services/apps/web/src/presentation/workspace/WorkspacePage.vue:73) lists overview, catalogue, business cards, content, media and brands | No current public culture/guide/news reading sections |
| Technical worker foundation | [Worker HostConfiguration.Extensions.cs:49](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Worker/Configurations/HostConfiguration.Extensions.cs:49) registers the foundation queue, diagnostic probe and heartbeat | Durable job plumbing exists; article scheduling and provider delivery do not |

The current design-document contract is organisation-scoped and contains a JSON snapshot, revision and archive state. It has no explicit brand or identity-release binding. A later brand identity integration needs that association rather than guessing it from an Ocharo template. [DesignDocument.cs:6](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Domain/DesignDocuments/Models/DesignDocument.cs:6).

## What public editorial delivery still needs

The host composes foundation, access, preparation and design-document endpoints. [HostConfiguration.cs:15](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Api/Configurations/HostConfiguration.cs:15). Its anonymous routes cover technical status and the SPA fallback. [HostConfiguration.Extensions.cs:247](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.backend-services/BrandWorkspace.Api/Configurations/HostConfiguration.Extensions.cs:247).

Inspection of those endpoint maps, models and frontend routes found no wired article CMS, public article/product projection, story publication lifecycle, editorial taxonomy, sitemap or RSS implementation. The generic HTML `article` tag in an operator component is not an editorial model. The generic static-file/SPA fallback can serve built frontend files; it does not establish article rendering or search-indexable article HTML.

| Gap | Minimal logical responsibility |
|---|---|
| Article content and reusable formats | One Editorial module: story blocks, source references, rights/credits, authors, language revisions, related reading |
| Cultural discovery | Fabric/technique, motif, place and maker references attached to editorial content |
| Dressing-guide specifics | Occasion/activity, season/context, outfit references, product references and practical comparison blocks |
| News specifics | Actual event/collection references, dated updates and expiry/archive handling where relevant |
| Publication | A public revision distinct from draft/review state, with preview and deliberate publication/unpublication |
| Anonymous assets | Approved public renditions with stable identities and credits; private uploads remain private |
| Reader delivery | Public reading templates, stable URLs, language links, navigation, metadata, sitemap/RSS when selected |
| Product browsing | One catalogue/product owner for factual records; public pages consume it rather than copy facts into articles |
| Shared identity | Brand Workspace Brand module owns selected identity/releases and official destinations; existing standalone Brand capability is an integration/reconciliation input |

These are missing capabilities in this repository, not instructions to rebuild capabilities already present in the separate Blog/Catalogue apps. Their reuse and physical placement require the deferred implementation comparison.

## Four experiences, minimal ownership

| Accepted vector | Distinct reader structure | Minimal content owner | Brand Workspace implication |
|---|---|---|---|
| Uzbek textile stories | Visual feature; fabric/process guide; place/maker index; pattern comparison | Editorial, with textile-specific metadata | Add or adopt an editorial domain; do not store the story as a print-design JSON canvas |
| What to wear, where and when | Situation guide; outfit comparison; activity/occasion discovery | Same Editorial owner with guide-specific fields | Reuse article/media/localisation capabilities while giving guides their own template and filters |
| Ocharo news | Chronological updates; collection/event detail; archive | Same Editorial owner with news-specific references | Dates and actual updates need their own semantics; no separate generic CMS required |
| Ocharo catalogue | Collection/model browsing; factual product detail; inquiry route | Existing catalogue/product owner, to be resolved once during wider integration | Current print-design editor is a consumer, not sufficient evidence of that authoritative product store |

Brand identity is shared across the four experiences. Media/provenance can use shared contracts. Marketing owns campaign derivatives and social delivery. Studio owns creative garment work and remains separately usable. None of these boundaries requires one repository for each public vector.

If the user wants four distinct named sites, the same ownership still applies: separate visible entry points and design treatments can consume one editorial service and one product source. Separate services become necessary only when an observed isolation, deployment or operating requirement justifies them. This audit establishes no such requirement for the four vectors.

## Where a future build could live

The repository already has a pnpm workspace declaring `apps/*` and `packages/*`. [pnpm-workspace.yaml:1](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/codebase/brand-workspace.frontend-services/pnpm-workspace.yaml:1). The existing operator application lives at `engineering/codebase/brand-workspace.frontend-services/apps/web/`.

If Brand Workspace is later selected as the physical home, an additional public frontend can sit beside that operator app under the existing `apps/` convention. One such frontend can render the three editorial vectors and route into catalogue. This is a possible placement, not a newly created folder or accepted migration plan. Multiple named public frontends are also supported by the workspace shape, but they should not duplicate article or product ownership.

The .NET domain/application/infrastructure/persistence projects already separate responsibilities by module folders. An Editorial module could follow that pattern; the accepted Brand module can mature within the same product boundary. There is no need to create a new backend just because “Textile stories” becomes a navigation item.

For the present analysis, preserve the two existing Blog implementations and their content. Do not move their code, repoint their source, share their databases or start a third complete blog. Determine reusable content and contracts independently of the deferred merger choice. The four-vector information architecture can be decided and documented without resolving physical migration now.

## Source versus documentation

The product context records the newer `r0.3` catalogue/card editor increment and earlier test results. [product/context.md:7](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/product/context.md:7). This audit confirms those editor structures in current source but did not rerun the recorded tests.

The architecture document distinguishes the long-term product target from the current preparation increment and explicitly describes the worker as diagnostics only. [architecture.md:11](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/workbench/brand-platform-ws/workbench/brand-workspace/engineering/architecture/architecture.md:11). Its Connections, Publications and Observations table is a target ownership map, not proof those modules currently operate.

The current business platform hub accepts identity ownership and discusses two long-term product repositories. That recommendation is not an adopted consolidation. The repository still contains preparation/design implementations; the accepted canonical Brand owner does not yet imply canonical identity-release APIs exist here.

## Recommendation for the parent analysis

1. Present four reader-facing structures with a shared identity and consistent routes into Ocharo.
2. Treat culture, dressing and news as three editorial types supported by one common content capability.
3. Keep product facts in one catalogue owner; keep catalogue design/print documents as a separate consumer workflow.
4. Preserve the accepted Brand Workspace Brand ownership and separate Studio boundary.
5. Keep physical Blog consolidation deferred; no architecture decision is needed to complete the content structures now.

Only this research audit was written. No implementation or adoption is claimed.
