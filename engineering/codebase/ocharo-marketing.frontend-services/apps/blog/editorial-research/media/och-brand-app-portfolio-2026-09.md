# Ocharo — public apps, operating portfolio and repository choices

> Historical research snapshot. Repository paths, line references and proposals describe the original inspection; current ownership and entrypoints are in [Blog adoption](../../../../../../architecture/blog-adoption.md).

*Last updated: 2026-09-29 12:42 PM GMT+5*

## Scope and recommendation

The user has selected **four genuinely separate public apps**: textile culture/gallery, wearing guides, Ocharo news and Ocharo catalogue. Each can have its own entry point, navigation, visual treatment, build and public release. This analysis preserves that decision; it does not reduce the four apps to tabs inside one blog.

The user will create Maftuna's new GitHub organisation. This report proposes product/repository boundaries only. It creates no organisation, repository, migration, account integration or application code. The merger of the two existing Blog implementations remains deferred.

**Recommended base topology: three product repositories** — the reusable Brand Workspace platform; one Ocharo public-web repository containing the four distinct public apps; and the separately owned Studio repository. This respects both a generic multi-brand platform and Ocharo's specific public publishing products. The simpler two-repository alternative remains viable when one team wants all platform and public surfaces released under one product owner.

This is a **base**, not an exhaustive organisation repository count. The completed [organization synthesis](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-organization-repositories.md) adds the verified Motion repository and optional later asset-library extraction, plus engineering/organization support repositories. It also incorporates the latest direction for contextual document ownership and shared assets. The current independently running Marketing, Catalogue, Blog and Brand implementations remain intact until an explicit transition is chosen.

## Sources and accepted boundaries

- [Current platform direction](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform.md) accepts Brand Workspace's Brand module as the canonical identity owner and retains Studio separately.
- [September 29 repository-boundary review](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-repository-integration-sweep.md) proposes two long-term product repositories; consolidation remains unadopted. This report evaluates an additional public-web boundary after the user explicitly selected four public apps.
- [Current Brand Workspace source audit](och-brand-workspace-structure-audit-2026-09.md) distinguishes implemented preparation/document editing from missing editorial/public-delivery capabilities. It is not repeated here.
- [Business capability map](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-capabilities.md) and [platform analysis](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-analysis.md) describe merchant, order, production and service opportunities as hypotheses, not adopted software workflows.
- [Editorial/content plan](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/research/och-res-uzbek-textile-editorial-2026-09.md) provides the four-vector content and brand-routing baseline.

The accepted Brand Workspace identity owner does not mean its currently minimal Brand record already provides the standalone Brand app's release/kit capabilities. Reconcile and reuse that implementation when authorised. Likewise, the current Catalogue app retains operational product-data authority pending explicit cutover; a target Catalogue module does not silently inherit its records now.

## App, module, runtime and repository

| Term | Meaning here | Consequence |
|---|---|---|
| Public app | A distinct reader/customer product with its own pages, navigation and design | Four are explicitly selected |
| Operator app | An authenticated workspace for the people running the brand | One shell can contain several complete operating modules |
| Domain module | One owner of business facts, commands and lifecycle rules | Identity, products, editorial and orders should not write each other's records freely |
| Runtime/build | An executable or deployable output | Each public app can build separately; workers can stop independently |
| Repository | A source-history, permissions and contribution boundary | Separate appearance or deployment does not automatically require separate Git history |

Four apps in one public-web repository remain four apps. Each has its own manifest, route space, tests, assets and build artifact. A shared repository should not force the same header, card style or typography everywhere. It should share useful contracts and selected interaction primitives.

## The four public apps

Names are functional working labels pending the separate naming review.

| Public app | What it owns for the reader | Content it consumes | Distinct style opportunity |
|---|---|---|---|
| **Culture/gallery** | Textile features, close inspection, regional entry points, fabric/process/pattern exploration | Editorial stories, credited imagery, taxonomy and maker/place references | Immersive image-led gallery and paced visual stories |
| **Wearing guides** | Situational outfit guidance, comparisons, garment movement and practical choice | Guide content and referenced catalogue items | Clear, useful comparisons, outfit sequences and illustrated decisions |
| **Ocharo news** | Actual collection, atelier, event and collaboration updates | Dated editorial updates and collection/event references | Timely editorial feed, strong dated headlines and concise dispatches |
| **Ocharo catalogue** | Collection/model discovery, factual product detail and inquiry routes | The single product catalogue's public projection | Precise, photo-led product presentation and legible buyer facts |

Their shared relationship is **by/with Ocharo**, common identity references and deliberate cross-links. They do not require a single visual template. The first three may share an Editorial content capability while keeping distinct schemas, navigation and public applications. Catalogue consumes product records rather than turning them into editorial posts.

A broad main Ocharo landing page can later direct readers to the four apps and current social channels. It is initially a page/entry point, not automatically a fifth public app or repository. No public domain or URL is selected by this report.

## Operating app portfolio

### First coherent operating scope

This is a proposed scope to support the four public apps and existing creative work, not a claim every row is implemented.

| App or work area | Minimum useful responsibility | Logical owner | Packaging recommendation |
|---|---|---|---|
| **Brand Workspace operator app** | Organisation/brand context, operator access, permissions, module navigation and attention views | Platform shell/access | One authenticated app, preserving its existing entry path |
| **Brand identity** | Official names, logo slots, released identity, public contacts/links, brand appearance policy | Brand Workspace Brand module | Module inside operator app; public kit is a projection, not a second independent writer |
| **Product catalogue data** | Product/model IDs, variants, multilingual facts, images, price tiers and stock where maintained | Current Catalogue implementation until explicit cutover; proposed platform Catalogue module thereafter | Operator module plus versioned public/read contracts; no duplicate product master |
| **Editorial desk** | Articles/guides/news, sources, credits, language revisions, preview and deliberate publication | Editorial module | One operating workspace serving three separate public apps |
| **Media library** | Asset identity, original/source files, rights/credits, renditions, product/story association | Shared media capability with domain-specific permissions | Shared service/module; no extra public media-management app |
| **Documents** | Catalogue editions, business cards, line sheets, crops/layouts, print/export snapshots | Documents module | Preserve existing editors; consume pinned product and identity revisions |
| **Marketing** | Campaigns, post compositions, approval, channel plans, supported delivery, receipts and recovery | Marketing module/product; current standalone app stays runnable | Operator experience plus separately controlled provider workers; permanent repo independence remains a separate product decision |
| **Studio** | Garment/material authoring, looks, creative documents and exports | Existing Studio product | Separate app and repository, as accepted |
| **Public delivery/measurement** | Approved public content, assets, app-specific discovery metadata, outbound-event evidence | Public delivery with Editorial/Catalogue/Brand contracts | Four public builds; instrumentation shares identifiers, not private operator access |

There is no need to create separate login applications named “Ocharo Brand,” “Ocharo Media,” “Ocharo Documents” and “Ocharo Editorial Admin” solely to make their module boundaries visible. They can be complete work areas in Brand Workspace. The user-facing public apps remain separate.

### Later operating modules

The module names below describe work, not a request to open more repositories now.

| Module | First useful workflow | Evidence/input needed | When a separate app would become justified |
|---|---|---|---|
| **Merchants/CRM** | One shop/contact record, visit notes, interests, follow-up and reorder history | Actual merchant records, owner workflow and access policy | A distinct merchant-facing portal or independent sales team journey |
| **Inquiries/customer service** | Received inquiry linked to product/story, owner, response and outcome | Verified inbox/channel path and response responsibility | Sustained support workload or external customer self-service |
| **Orders** | Agreed assortment, buyer, quantities, accepted price snapshot, status and payment/delivery facts | Actual order examples and agreed commercial rules | Customer/merchant self-service ordering, if genuinely needed |
| **Stock/inventory** | Variant quantities, locations, commitments and adjustments | Initial stock count and actual reservation process | Dedicated warehouse/scanning workflow with separate device constraints |
| **Production/workshop** | Cutting/sewing batch handoff, maker allocation, return, inspection and rework | Real batch examples, maker roles and accepted status transitions | A restricted maker-facing mobile app, when collaborators need it |
| **Materials/sourcing/costing** | Supplier/material purchases, quantities, garment cost sheet and actual rates | Material consumption, costs and supplier evidence | Separate purchasing operation or product with its own users |
| **Fulfilment** | Dispatch, carrier/reference, delivery evidence, balance and exception | Actual routes, delivery process and agreed responsibility | High-volume dispatch tooling or carrier-specific device integration |
| **Finance/receivables** | Paid/unpaid amounts and unit-margin inputs linked to orders | Accurate costs and payment records; chosen accounting boundary | Accounting ownership and requirements warrant a separate system |
| **Opportunities/portfolio** | Selected event/opportunity, deadline, approved work and submission packet | Actual opportunities and chosen commitments | Distinct external portfolio site beyond the current four public products |
| **Learning/experiments** | Compare campaign/content questions against observed inquiries and orders | Published content and reliable outcome data | Independent analytics product/customer base, not merely a dashboard |

Begin with complete manual workflows before adding unattended automation. An order module should not infer agreed terms; a production board should not invent capacity; a metrics screen should not turn missing observations into zero.

Materials and research need two distinguishable uses: culture/gallery holds public educational references; Studio and production hold actual usable materials and design/manufacturing records. Cross-links can connect them without making a museum object a purchasable fabric lot.

## Ownership rules across the portfolio

1. **Brand owns identity.** A public app can have its own editorial name and style while consuming the selected Ocharo identity and official destinations. A new logo release does not rewrite old print exports.
2. **Catalogue owns current product facts.** Public product pages, guide links, marketing drafts and document editions consume references or snapshots. An edition-specific text/price change is not silently a product update.
3. **Editorial owns stories.** Culture, guide and news experiences share publishing mechanics but retain their own content semantics. Marketing derivatives reference a story rather than becoming its master copy.
4. **Marketing owns provider work.** Publication intent, retries, uncertain outcomes and receipts belong there; it cannot overwrite catalogue or identity facts to make a post succeed.
5. **Orders own commitments.** An accepted order preserves its price/assortment snapshot even if the live catalogue changes.
6. **Studio owns creative documents.** Export/import or versioned contracts connect it to media/product records; its workspace identifiers are not platform authorisation.
7. **Each module owns its mutations.** A common repository or database does not grant arbitrary writes across module tables.

## Repository topology alternatives

Counts below exclude additional independently owned Motion/3D products pending their audit, upstream SDKs, private business records and optional organisation/workspace administration repositories.

| Base topology | Product repositories | Where four public apps live | Benefits | Costs / suitable condition |
|---|---:|---|---|---|
| **A. Platform plus Studio** | **2** | Four separate `apps/*` builds in the platform repo alongside the operator app | Shared changes reviewed together; few repository-level controls | Generic platform and Ocharo-specific publishing code share source access and product governance; best under one owner/team |
| **B. Platform, Ocharo public-web, Studio** | **3** | Four separate apps in a dedicated brand-specific public-web monorepo | Clean reusable-platform versus brand-publication boundary; independent public releases; still avoids four tiny repos | Versioned cross-repo contracts and consumer compatibility become deliberate work |
| **C. Platform, each public app, Studio** | **6** | Each public app has its own repository | Independent maintainers, access and product release ownership | Repeated build/CI/dependency coordination; justified by four independent product teams, not four visual styles |

**Recommend B as the base target for this clarified portfolio.** The reason is the product-scope boundary: reusable multi-brand operations versus Ocharo-specific public publishing. Independent visual style remains inside each public app. If the same team deliberately wants one product governance/release unit, A is a valid smaller target; it still fulfils all four separate-app requirements.

Marketing is the strongest further source-repository fork. Provider credentials and uncertain external outcomes require runtime/permission isolation regardless of Git layout. A permanently separate repository becomes useful when Marketing has independent customers, maintainers, source access or commercial release ownership. Its temporary separate build lane alone does not establish that permanent boundary.

## Proposed base repositories for the new organisation

The following are role labels and suggested repository names, not claims that the names or organisation already exist.

| Proposed repository | Role | Included apps/modules | Existing source to preserve/reconcile |
|---|---|---|---|
| `brand-workspace` | Reusable operating platform | Operator app, Brand, Catalogue, Editorial, Documents, Marketing, module APIs and workers; later operational modules | Existing Brand Workspace plus capabilities from current standalone apps through an explicit transition |
| `ocharo-web` | Ocharo's public product family | Culture/gallery, wearing guides, news, catalogue as four distinct frontend builds | Both current Blog implementations remain separate inputs until their merger decision; existing catalogue read contracts |
| `ocharo-studio` | Independent creative product | Studio and its creative/runtime responsibilities | Existing Studio repository and history |

An optional `.github` repository can hold organisation-facing profile/default community material if desired. An optional engineering-workspace repository can hold checkout manifests and shared coordination scripts. These are administrative choices, not additional products required to run the four public apps. Keep Maftuna's private business records private; do not transfer the entire business workspace merely to populate the organisation. Keep WoW2 SDK repositories upstream rather than copying them into brand-specific repos.

Motion/3D asset repositories are deliberately not assigned a final count or ownership here. Their independent engine, asset lifecycle, consumer contracts and users should determine whether they remain additional products/libraries or belong inside an existing product. The parent inventory will complete the organisation list.

## Public-web shape under the recommended topology

Illustrative future layout; none of these folders is created by this report:

```text
ocharo-web/
  engineering/codebase/ocharo-web.frontend-services/
    apps/
      culture/      # its own experience, assets, configuration and build
      wear/         # practical guide experience
      news/         # dated brand-publication experience
      catalogue/    # factual product-discovery experience
    packages/
      contracts/    # versioned public content/product/brand data contracts
      primitives/   # selected common interactions and accessibility basics
```

The operator stays in Brand Workspace's existing `apps/web` location. Shared packages must not import one app's source into another. Public contracts expose approved data; they do not expose operator credentials, private documents, draft articles or merchant terms. Public build outputs can use static delivery where appropriate, with independent release configuration for each app. A source repository count is not a server-process count.

Choose each app's navigation, art direction, content templates and release artifact independently. Share only the elements intentionally selected: data contracts, links, accessible interaction primitives and approved brand assets. The goal is four recognisable experiences with one reliable business behind them.

## Transition boundary

The target topology is a recommendation, not migration approval. Creating the GitHub organisation is the user's action. Preserve current repository histories, running apps and existing contracts until the relevant transfer/consolidation is authorised.

Do not start a second canonical Brand writer, duplicate Catalogue records into the public app, or create a third complete Blog while comparing the two existing implementations. The new four-app information architecture and distinct visual directions can be designed now without choosing which Blog code is moved later.

The immediate deliverable is this complete portfolio and topology comparison. Future implementation should begin from the selected architecture and verified existing capability owners, with the deferred Blog-merger question still explicitly deferred.
