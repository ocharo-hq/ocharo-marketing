# Ocharo organization repository inventory

> Historical research snapshot. Repository paths, line references and proposals describe the original inspection; current ownership and entrypoints are in [Blog adoption](../../../../../../architecture/blog-adoption.md).

*Last updated: 2026-09-29 12:41 PM GMT+5*

## Scope

The user confirmed four separate public apps: culture/gallery, wear guides, news, and catalogue, with different visual styles. This note inventories additional existing repositories and reusable source assets for a future Maftuna GitHub organization. Repository names below are proposals, not created resources.

This was a bounded, read-only source audit. Git roots and configured origins were inspected locally. Remote names are credential-free; remote availability, GitHub rename redirects, membership, ownership transfer eligibility, and runtime state were not checked. No repository was created, transferred, renamed, merged, built, served, or published. Only this note was written.

The earlier [app structure audit](och-existing-app-structure-audit-2026-09.md) covers Blog, Catalogue, Marketing, and Brand contracts in detail. The two existing Blog implementations remain separate; their merger is deferred.

## Verified repository boundaries

The six `ventures.ocharo-*` directories below each resolve to their own independent Git root. Their parent `wow-two-ws` is a workspace, not the owner of those Git histories.

| Existing repository | Verified root | Configured origin, sanitized | Inventory status |
|---|---|---|---|
| Blog | [ventures.ocharo-blog](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog) | `github.com/sulton-max/ventures.ocharo-blog.git` | Independent local repository; origin configured |
| Catalogue | [ventures.ocharo-catalogue](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue) | `github.com/sulton-max/ventures.ocharo-catalogue.git` | Independent local repository; origin configured |
| Marketing | [ventures.ocharo-marketing](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing) | `github.com/sulton-max/ventures.ocharo-marketing.git` | Independent local repository; origin configured |
| Brand | [ventures.ocharo-brand](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-brand) | No `origin` configured | Independent local repository; GitHub existence not established here |
| Studio | [ventures.ocharo-studio](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio) | `github.com/sulton-max/ventures.uzbek-studio.git` | Independent local repository; origin uses old name |
| Motion | [ventures.ocharo-motion](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-motion) | No `origin` configured | Independent local repository; GitHub existence not established here |

Studio's [README:85](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/README.md:85) says the GitHub repository was renamed to `sulton-max/ventures.ocharo-studio`. That is a documented rename, while `ventures.uzbek-studio` is the current local origin. Verify the actual GitHub repository before proposing a transfer command.

The following sources are directories inside [mft-10x-ws](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws), whose sanitized origin is `github.com/sulton-max/mft-10x-ws.git`; they are not independent repositories:

| Directory | Role | Boundary |
|---|---|---|
| [sites/och-journal](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static) | Existing standalone Blog prototype and editorial research | Keep intact pending separately authorized Blog consolidation |
| [assets/3d/ocharo-garments](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-garments) | Canonical authored garment/figure library | Selected Ocharo source extraction candidate |
| [portfolio/products](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/products) | Product records, supplied references, generated derivatives | Business source records; distinct from Catalogue's edited database |
| [portfolio/brand](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/brand) | Selected identity masters, exports, studies and builders | Preserve selected masters and provenance; do not publish every trial |
| [portfolio/catalogue](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/catalogue) | Print/editorial catalogue sources and exports | Reuse selected layouts and source records; historical PDFs are outputs |
| [sites/och-shawl-sourcing](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-shawl-sourcing) | Internal supplier research viewer | Private operational tool; outside the four public apps |

The workspace [boundary:13](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/CLAUDE.md:13) covers Maftuna's work records. A product organization should receive selected product sources, not an undifferentiated copy of this workspace.

## Additions beyond the earlier four-app audit

### Ocharo Studio — retain independently; organization move candidate

Studio owns garment/material editing, saved looks, and visual exports. Its [README:9](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/README.md:9) describes five garment families and per-part placement; [README:14](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/README.md:14) describes SQLite persistence, offline recovery, JSON and PNG exports. Those are source-documented capabilities, not a current browser verification.

The stack is Vue 3, TypeScript, Vite, Three.js and a .NET API. The current [package manifest:26](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/engineering/codebase/ocharo-studio.frontend-services/package.json:26) pins its Vue SDK to a bundled tarball. The [README:74](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/README.md:74) explains that the workspace UUID is not account authentication. Therefore the existing editor should not be treated as a ready shared customer account service.

**Recommendation:** retain `ocharo-studio` as its own app/repository and proposed organization transfer candidate. Its editor and WebGL payload do not belong inside a culture article's initial page load. Public Catalogue or wear guides can link a specific look or later embed an exported preview without owning the editor source.

### Ocharo Motion — retain as research; defer public launch

Motion is already intentionally separate. The [product brief:5](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-motion/product/product.md:5) defines rigged character and garment motion work; [brief:8](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-motion/product/product.md:8) explains its experimental scope.

Current [architecture:5](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-motion/engineering/architecture/architecture.md:5) records Blender-generated avatar assets, a framework-independent `@ocharomotion/engine`, and a Vue lab for clips, views, speed and turntable controls. The .NET host is explicitly a scaffold with no motion API yet. The [manifest:19](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-motion/engineering/codebase/ocharo-motion.frontend-services/apps/web/package.json:19) confirms Vue, Three.js, the engine workspace dependency, and the shared Vue SDK.

**Recommendation:** retain a private `ocharo-motion` research repository; creating its organization remote can wait for the organization plan. Motion can supply clips and reusable runtime packages later. It is not a required fifth public app or a reason to delay the four confirmed public experiences.

### Ocharo assets — split selected reusable source ownership

The [3D library guide:10](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-garments/och-3d-garments.md:10) records seven GLBs, an editable Blender scene, Python build source, a hash manifest, and review renders. These sources are reused by Studio; they are more than one app's generated output directory.

Current Studio [sync source:11](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/engineering/codebase/ocharo-studio.frontend-services/scripts/sync-garments.mjs:11) imports both silhouette and catalogue model, plus five garments. It imposes per-asset limits at [line 24](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/engineering/codebase/ocharo-studio.frontend-services/scripts/sync-garments.mjs:24). The library guide's statement at line 80 that the model is not synced is stale relative to this code. Do not carry that older exclusion into the organization plan.

**Recommendation:** a future private `ocharo-assets` source repository can hold authoring code, manifests, selected brand masters and asset licenses/provenance. Keep distinct areas for brand identity, original product media, authored 3D, and editorial reference media. Publish versioned delivery bundles or object-store derivatives for consumers. This is an ownership split proposal, not permission to extract files now.

The Catalogue service should retain product/pricing/stock authority. Asset storage should not create a second editable catalogue. The [product guide:5](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/products/och-metadata-guide.md:5) and [line 52](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/products/och-metadata-guide.md:52) document the existing source JSON/provenance, while the earlier app audit describes Catalogue's import and revision boundary.

### Sourcing viewer — retain privately; defer another repository

The [viewer README:5](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-shawl-sourcing/README.md:5) identifies an existing Vue 3/TypeScript TSX research app. Its [package manifest:6](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-shawl-sourcing/package.json:6) uses Vite and local-only preview bindings. It compares sourcing leads and supports quotation preparation; it is an internal operational view.

**Recommendation:** retain this work privately in the business workspace for now. If sourcing becomes a repeating business workflow, extract a purchasing/sourcing module or private app then. Its present existence does not justify another public Ocharo site.

## Payload implications

Filesystem metadata measured during this audit, using decimal units; these are current working-tree sizes, not Git history, LFS billing, or deployment bundle measurements:

| Area | Files | Current bytes | Meaning |
|---|---:|---:|---|
| 3D garment library | 29 | 10,444,525 (~10.4 MB) | Small selected source library; Blender scene alone is ~4.43 MB |
| Entire designer portfolio | 799 | 2,309,605,021 (~2.31 GB) | Includes repeated PDF exports, media, trials and source records; unsuitable for wholesale app copying |

Studio already tracks original PNG textures and the bundled SDK tarball in [Git LFS attributes](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/.gitattributes:1). Its [README:28](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/README.md:28) documents LFS and compressed WebP runtime textures. A future move must include those LFS objects and preserve licenses/provenance, rather than assuming a Git source clone is the complete asset payload.

Apply the workspace [large-file rule:18](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/conventions/development/repo/version-control/git.md:18): keep regeneration sources in normal Git; choose LFS for required non-regenerable or ready-to-run binary sources; use release/object storage for large deliverables. No current GitHub storage allowances are asserted here.

## Organization disposition

These are recommended categories, not executed transfers or a final naming decision.

| Category | Sources/apps | Recommendation |
|---|---|---|
| Move existing Ocharo product ownership | Catalogue, Marketing, Studio | Preserve independent repositories; make them candidates for Maftuna's organization |
| Retain pending Blog decision | Platform Blog and standalone Blog prototype | Keep both intact; decide source consolidation later, as requested |
| Separate public experiences | Culture/gallery, wear guides, news, catalogue | Give each an independent presentation/deployment boundary and visual style; avoid duplicating product data or asset masters |
| Split selected source assets | Authored 3D, approved brand masters, selected product media/provenance | Proposed private `ocharo-assets`; versioned derivatives for apps |
| Retain research; defer launch | Motion | Independent private research repository; consume its outputs later |
| Retain operational records privately | Business workspace, sourcing viewer, historical print exports | Do not mix the entire workspace into public application repositories |
| Hold identity service decision | Existing Ocharo Brand | Inventory current code, then follow the separately agreed Brand Workspace ownership decision; avoid creating a second canonical identity owner |
| Keep upstream | WoW2 backend SDK, `@wow-two-beta/ui-vue`, general Brand Workspace product | Reuse their package/service contracts; organization placement of Ocharo apps does not transfer shared framework ownership |

Concrete upstream dependency evidence is in Motion's [backend package declaration:13](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-motion/engineering/codebase/ocharo-motion.backend-services/Directory.Packages.props:13) and the Studio/Motion frontend manifests linked above. SDK tarballs inside an app are dependency artifacts, not evidence that the SDK source belongs to Ocharo. General Brand Workspace ownership is handled in the parallel architecture analysis; it was not re-audited here.

Four public apps therefore do not imply transferring every related folder or creating a separate backend for every site. The immediate organization inventory is six existing Ocharo Git roots, two relevant local site directories, selected shared asset sources, and external upstream dependencies. The public app split, Blog merger, GitHub transfer, and asset extraction remain separate decisions and actions.
