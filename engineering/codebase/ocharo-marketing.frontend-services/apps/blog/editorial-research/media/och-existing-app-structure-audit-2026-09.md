# Existing Ocharo app structure audit

> Historical research snapshot. Repository paths, line references and proposals describe the original inspection; current ownership and entrypoints are in [Blog adoption](../../../../../../architecture/blog-adoption.md).

*Last updated: 2026-09-29 12:28 PM GMT+5*

## Scope and evidence

Read-only source inspection of the standalone blog and four existing sibling apps: Blog, Catalogue, Marketing, and Brand. This records the structures already present; it does not select which four public experiences the user intends, design the later blog merge, or claim that any runtime is currently running.

No app code, database, configuration or repository state was modified. No build, tests, server, HTTP probe, publishing command or Git mutation was run. Only this audit note was written. Historical test counts and prior successful launches were intentionally not treated as current runtime evidence.

## Repository boundaries

The following roots were verified with `git rev-parse --show-toplevel` on 2026-09-29:

| Surface | Source directory | Verified Git owner |
|---|---|---|
| Standalone blog | `/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/sites/och-journal` | `/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws` |
| Blog platform | `/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ventures/ventures.ocharo-blog` | That same independent repository |
| Catalogue | `/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ventures/ventures.ocharo-catalogue` | That same independent repository |
| Marketing | `/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ventures/ventures.ocharo-marketing` | That same independent repository |
| Brand | `/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ventures/ventures.ocharo-brand` | That same independent repository |

The WoW workspace is a meta-repository; it does not own its managed app repositories' source. Its registry lists the four sibling apps separately: [registry:125](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/.claude/repo-registry.md:125).

Applicable root and app instructions were read before code inspection. Brand Workspace depth is delegated to a separate audit; **Ocharo Brand** is the existing identity service and should not be confused with the general **Brand Workspace** product.

## Structure at a glance

| Implementation | Main job | Rendering and editing | Persistent source |
|---|---|---|---|
| Standalone blog | Editorial/design prototype | Node generator emits static HTML; plain document links | `och-content.mjs`, authored CSS and local assets |
| Blog platform | Edit and publish stories | Vue operator Studio; .NET renders public HTML; static ZIP export | SQLite documents/revisions and hashed media |
| Catalogue | Edit products, copy, prices, stock and collections | Vue operator UI and print sheets; public JSON feed | SQLite product/collection revisions and hashed media |
| Marketing | Turn products/stories into drafts and planned channel posts | Vue operator UI; deterministic HTML card rendering | SQLite drafts, jobs, schedules, publication evidence and media |
| Brand | Publish identity and logo releases | Vue operator UI; public kit, logo and CSS endpoints | SQLite release/appearance documents and hashed media |

The sibling apps use .NET 10 and Vue 3, with the shared WoW backend/frontend SDKs. Examples: [Blog target framework:8](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/Directory.Build.props:8), [Blog frontend dependencies:19](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.frontend-services/apps/web/package.json:19), [Catalogue SDK and persistence packages:14](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.backend-services/Directory.Packages.props:14).

## 1. Standalone blog

### Existing structure

- The build imports only `topics` and `stories` from `och-content.mjs`, validates their references, and writes beneath its own `dist` directory. [Builder:1](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/och-build.mjs:1)
- Three source topics are currently `pieces`, `style`, and `cloth`. They represent the original article library, not yet the proposed four editorial vectors. [Content:1](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/och-content.mjs:1)
- Public route generation covers the home page, archive, topic pages and individual `/journal/{slug}/` stories. Articles use a single optional cover plus text sections, lists, citations and related stories. [Renderer:53](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/och-build.mjs:53)
- Search indexing is disabled in generated HTML and robots rules. [HTML head:40](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/och-build.mjs:40), [robots output:77](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/och-build.mjs:77)
- The package defines a Node build and Python static preview on `127.0.0.1:8774`; it has no package dependencies. These are source commands, not proof of a running listener. [Package:1](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/package.json:1)

### Source authority and current research

`och-content.mjs` is the input the current builder consumes. The Markdown research, textile matrix, fabric-guide draft and two Uzbekistan story drafts are useful editorial assets but do not become pages merely by existing under `editorial-research`. The builder has no imports for these drafts.

The local documentation describes the original thirty-story library, directly authored `dist/och-style.css`, and assets in `dist/assets`: [source responsibilities:34](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ocharo-blog-static/och-journal.md:34). Its September 27 runtime and verification statements are historical notes, not a current status check.

### Reuse and missing structures

Reuse: established public layout, original prose, citation shape, provenance notes, styling and editorial research. Missing in the inspected generator: an operator editor, persistence lifecycle, language editions, RSS generation, fabric/place/pattern entities, multiple section images, and gallery/story relationships.

## 2. Blog platform

### Existing content and publishing model

The platform already has a structured `ArticleDocument`: title, topic, language, excerpt, introduction, format, reader promise, sections, sources, takeaway, closing question, related story slugs, cover, catalogue product IDs, SEO fields and translation grouping. [Document:4](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Domain/Blog/ArticleDocument.cs:4)

`ArticleSection` contains only a heading, paragraphs, optional list and citation indexes. `ArticleCover` contains one media hash, alt text, caption and presentation class. No section gallery, image set, video, map, textile record or maker record is represented by these types. [Section and cover:66](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Domain/Blog/ArticleDocument.cs:66)

The editor API includes draft creation, updating, publish, schedule, unpublish, archive, linked translation, revisions and restore. [Articles controller:10](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Api/Blog/Controllers/ArticlesController.cs:10)

Vue Studio has story list/editor, topics and site settings routes. [Studio router:4](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.frontend-services/apps/web/src/bootstrap/BlogRouter.ts:4)

### Public rendering and source authority

The .NET site service reads published database records, selects a language, fetches catalogue data only when stories reference products, and applies Brand identity. [Site model:30](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Application/Blog/Site/SiteService.cs:30)

The server renders `/`, `/journal/`, stories, topics, `/uz/` and `/ru/` editions, RSS, sitemap and robots. It also exports static site files and images. [Public routes:10](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Api/Blog/Controllers/PublicSiteController.cs:10), [static export:98](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Application/Blog/Site/SiteService.cs:98).

The standalone content is converted by an explicit script into a seed JSON with a source hash. [Import script:14](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/scripts/import-journal.mjs:14). Startup seeding returns immediately if any topics or articles already exist. Therefore, editing the standalone blog does **not** automatically update an existing platform database. [Seeder:20](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Infrastructure/Blog/BlogSeeder.cs:20)

### Media and integration boundaries

- SQLite stores records; media is addressed by SHA-256, with a WebP rendition. [Persistence:14](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Persistence/DependencyInjection.cs:14), [media:13](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Infrastructure/Blog/MediaStore.cs:13)
- Articles already point to Catalogue through `ProductIds`. The feed adapter currently requests English product copy even when article editions use other languages. [Catalogue adapter:25](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Infrastructure/Blog/CatalogueFeed.cs:25)
- Brand can supply logo and links; local Blog settings remain the fallback when it is unavailable. Static exports omit time-bounded event badges. [Brand application:29](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Application/Blog/Site/SiteService.cs:29)
- The current app rejects non-loopback requests. Authentication is required before changing that live deployment boundary; static export is the existing publication mechanism. [Pilot boundary:5](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-blog/engineering/codebase/ocharo-blog.backend-services/OcharoBlog.Api/Blog/LocalPilotBoundaryMiddleware.cs:5)

Reuse the content lifecycle, languages, renderer/export and references. The image-rich Uzbekistan atlas needs richer editorial media blocks and explicit material/place/pattern relationships; those are content-model gaps, not grounds to rebuild the existing editor wholesale.

## 3. Catalogue

Catalogue owns editable product records: stable product IDs, garment families, variants, design IDs, sizes, fabric and decoration codes, multilingual copy, pricing, ordered media, review issues, source provenance, stock and made-to-order status. `FabricCodes` is explicitly a free vocabulary until a registry exists. [Product document:7](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.backend-services/OcharoCatalogue.Domain/Catalogue/Models/ProductDocument.cs:7)

Collections already provide named, ordered product selections and a publication flag. [Collection document:4](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.backend-services/OcharoCatalogue.Domain/Catalogue/Models/CollectionDocument.cs:4)

The public contract includes active-product data, published collections, languages, prices, available sizes and image hashes/URLs. Exact stock counts are internal. [Public models:5](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.backend-services/OcharoCatalogue.Application/Catalogue/Models/PublicModels.cs:5), [feed route:8](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.backend-services/OcharoCatalogue.Api/Catalogue/Controllers/PublicCatalogueController.cs:8).

The importer reads portfolio product records without writing back, preserving source hashes. Reimport is explicit and records a new app revision. [Importer:15](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.backend-services/OcharoCatalogue.Infrastructure/Catalogue/WorkspaceCatalogueImporter.cs:15)

The frontend is an operator interface: products, copy, pricing, stock, collections, print and settings. Its routes do not constitute a finished public product gallery or shop. [Catalogue router:4](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-catalogue/engineering/codebase/ocharo-catalogue.frontend-services/apps/web/src/bootstrap/CatalogueRouter.ts:4)

**Reusable for a public catalogue/gallery:** product IDs, ordered photos, collections, copy and public feed. **Missing in the inspected contracts:** a historical textile object registry, pattern and place entities, general editorial gallery ownership, checkout and a public catalogue page router. A photograph attached to a product is not automatically a museum/research asset.

## 4. Marketing

Marketing imports Catalogue's feed and stores immutable, SHA-256-verified copies of product images. It records the catalogue revision in its product review data. [Sync service:13](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing/engineering/codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Automation/CatalogueSyncService.cs:13)

It already reads Blog RSS and creates a draft caption linking to a story. This is a concrete existing route from long-form editorial content to channels. [Blog story service:13](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing/engineering/codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Automation/BlogStoryService.cs:13)

Two constraints matter for cultural editorial work:

1. The adapter requests `/feed.xml`, which is the English-root feed; it does not select `/uz/feed.xml` or `/ru/feed.xml` from the requested caption locale. [RSS load:25](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing/engineering/codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Automation/BlogStoryService.cs:25)
2. Draft creation requires a product ID and uses its hero asset. A museum or fabric-history announcement cannot currently flow through this method as an independent story with its own cover. [Draft creation:44](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing/engineering/codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Automation/BlogStoryService.cs:44)

The value named `blog-announced` records the generated draft ID. It is not proof that the story was published to a channel. Delivery has separate connection verification, queueing, reconciliation and metrics endpoints. [Delivery controller:9](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing/engineering/codebase/ocharo-marketing.backend-services/OcharoMarketing.Api/Marketing/Controllers/DeliveryController.cs:9)

Source configuration defaults live publishing to false. This audit did not inspect runtime configuration overrides or provider accounts. [App configuration:13](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-marketing/engineering/codebase/ocharo-marketing.backend-services/OcharoMarketing.Api/appsettings.json:13)

Reuse the draft, scheduling, delivery-evidence and product-sync structures. Extend the source association when cultural posts need story-led media and language-specific links.

## 5. Ocharo Brand

The existing Brand service manages identity, logo versions, immutable releases and scheduled appearances. Its public contract already exposes the current kit, pinned releases, stable logo slots, immutable media and CSS tokens. [Public Brand controller:10](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-brand/engineering/codebase/ocharo-brand.backend-services/OcharoBrand.Api/Brand/Controllers/PublicBrandController.cs:10), [release and appearance records:62](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-brand/engineering/codebase/ocharo-brand.backend-services/OcharoBrand.Domain/Brand/BrandDocuments.cs:62).

This is useful shared identity infrastructure for whatever public app names are selected. It is not a replacement for article bodies, product inventory or a public culture atlas. Blog source already consumes its logo and links. Broad Brand Workspace ownership remains outside this audit.

## Existing data flow

```text
Standalone blog source --explicit conversion--> seed JSON --empty-store import--> Blog database
Portfolio product files --read-only import--> Catalogue database
Catalogue public feed --product references--> Blog public pages
Catalogue public feed --verified media copies--> Marketing products/drafts
Blog RSS --draft announcement with product image--> Marketing
Brand published kit --logo and links--> Blog
Marketing --separate delivery workflow--> configured channels
```

The arrows describe code paths found in source. They do not prove current data equality, active connections, successful publication or present runtime availability.

## Implications for the next structure discussion

1. Two Blog implementations exist, but the platform has already absorbed the original story schema and design as a seed/rendering path. The later merge should preserve each implementation's newer work; this audit does not execute or design that merge.
2. “Four editorial vectors” and “four applications” describe different things. Existing Blog topics can organise editorial vectors without requiring four databases or repositories. Exact public-app grouping remains the user's open decision.
3. Public story pages have a ready owner in Blog; product data and product photos have a ready owner in Catalogue; identity has a ready owner in Brand; channel preparation/delivery has a ready owner in Marketing.
4. A public gallery can reuse Catalogue collections when it displays products. A historical textile gallery requires editorial asset metadata and relationships beyond those product records. This distinction should precede naming a new app.
5. App ports in README/configuration are development settings. No production domain, deployment topology or currently running service was verified.
6. The immediate editorial gaps are specific: article-level image sequences, textile/place/pattern relationships, cultural story promotion without a mandatory product, and language-aware catalogue/feed consumption.

These findings support the next ownership decision while keeping the user-requested later merge deferred.
