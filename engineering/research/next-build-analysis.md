# Ocharo Marketing — what to build next

*Last updated: 2026-09-29*

## Recommendation

Build a product content-pack workflow and close the manual publishing loop. A useful next result is:
choose a product → prepare a small coordinated set of posts → review the exact outputs → export to the SMM operator
→ record what was posted → record enquiries. Complete Instagram delivery alongside this once its account and hosting choices exist.

This is an analysis proposal, not a new implementation track, deployment decision, or authorization for external actions.
It updates the assessment against the September 28 platform integrations. No application code or provider settings changed.

## What already exists

| Capability | Current implementation | Consequence |
|---|---|---|
| Product inputs | Catalogue sync, localized facts, prices, availability, hashed cutout/model imports | Extend the existing sync; do not build another catalogue |
| Draft production | Bulk caption templates, blog announcements, optional new-product drafts | Add content variety and source review rather than another bulk-draft button |
| Card production | T01 half-fabric; T02 bone/charcoal; deterministic HTML preview and PNG rendering | Preserve these layouts and rendering components |
| Review and export | Revision approval, immutable media, caption/media ZIP, atlas PDF | Extend approval and package semantics to ordered content sets |
| Planning | Weekly Tashkent slots and optional unattended sync/planning | Improve reliability and editorial selection before adding automatic queue consent |
| Delivery | Telegram/X orchestration; separate Instagram/WhatsApp brokers | A broker is not a complete operator delivery workflow |
| Results | Bounded, account-owned X observations and receipt reconciliation | Add manual/Instagram evidence; do not replace the existing measurement model |

The current implementation evidence is in [r0.1](previous-work/rough-track/r0.1/r0.1.md) and
[handoff](previous-work/handoff.md). Their older verification sections describe earlier increments.
This analysis inspected source; it did not rerun test suites or establish current provider connectivity.

## Proposed build order

### 1. Product content packs and manual handoff

One pack groups related posts, source evidence, channel copies, and review state. It is a workflow grouping,
not a new service or repository. Keep two concepts separate: a pack contains several deliverables;
a carousel is one deliverable containing ordered slides.

Start with an existing T01/T02 hero, a readable T06 facts card, and a 9:16 Story adaptation.
Add T03 detail, T04 worn, and T05 A/B through the same composition contract when suitable assets exist.
Reuse the current ordered ZIP export before extending automatic carousel delivery.

| Format | What it brings | Required input and pattern |
|---|---|---|
| Front hero | Fast recognition of the garment | Existing cutout, short title, restrained brand treatment; T01/T02 |
| Facts | Answers purchase questions | Reviewed sizes, material, availability and optional price; one clear ordering cue; T06 |
| Detail | Explains material and workmanship | Genuine fabric/detail photograph, one short annotation; T03 |
| Worn | Shows silhouette and styling | Typed model/worn asset; distinguish photographed evidence from generated visualization; T04 |
| A/B choice | Collects preference signals | Two identified available or explicitly conceptual alternatives, matched framing, one question; T05 |
| Story | Creates a phone-oriented adaptation | Intentional reflow, readable text, operator-placed interaction/link cues; not a stretched feed image |
| Process video | Shows making and transformation | Actual workshop clips; separate later motion increment |

The atlas becomes the review contact sheet for these exact outputs. It should show format, intended purpose,
asset origin, selected variant, and notes outside the post artwork. It need not become a separate large publishing product.

Acceptance:

- A product creates a coordinated hero, facts card and Story, with missing inputs shown explicitly.
- An ordered slide strip supports add/remove/reorder; every slide has a phone-size preview.
- Copy fits without shrinking essential information into unreadable footnotes.
- Asset role and origin are separate: front/fabric/detail/worn versus photographed/generated/derived/unknown.
- Imported model images do not automatically establish genuine fit, texture, or real-world wear evidence.
- Editing a slide, caption, destination, or approved rendition requires review of the affected publication revision.
- Exports preserve order, locale, captions, media hashes, product references and pack/format identifiers.
- Manual publication records the approved revision, intended account, permalink or other evidence, and actual time.
- Operator-recorded publication remains distinct from API-verified publication; exporting alone proves neither.
- Duplicate completion submissions converge; recording a manual post never triggers provider delivery.

Source: only three presets render in [HtmlCardRenderingService.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Cards/Services/HtmlCardRenderingService.cs).
T03–T06 are study labels in [AtlasPage.vue](../codebase/ocharo-marketing.frontend-services/apps/web/src/presentation/marketing/AtlasPage.vue).
[LocalAssetService.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Marketing/Services/LocalAssetService.cs)
already exports ordered media, caption and manifest. [ResultsPage.vue](../codebase/ocharo-marketing.frontend-services/apps/web/src/presentation/marketing/ResultsPage.vue)
has no corresponding manual-publication record workflow.

### 2. Source freshness and reliable automatic planning

Implement the minimum source checks alongside content packs; complete retry/concurrency handling before expanding unattended work.
Approved artifact immutability already works. The missing capability is a separate assessment of whether its source facts remain current.

Capture product revision and the facts used by a draft. Show changes to price, availability, wording and selected media.
For affected future work, request re-review or explicit acceptance of the change. Never rewrite approved artwork or historical captions.
An editorial garment post without a stock claim and an availability promotion need different eligibility rules.

Acceptance:

- A complete authoritative sync can identify withdrawn products; partial, failed or offline syncs cannot withdraw them.
- A changed source produces a visible diff while existing approved exports remain byte-identical.
- Future publication checks the accepted source state, including queued work immediately before dispatch.
- A transient sync outage leaves cached work usable and marks freshness unknown rather than claiming withdrawal.
- Manual and worker slot-fill calls cannot reserve duplicate slots or duplicate the same intended draft/channel placement.
- Interrupted new-product drafting resumes per-product actions without duplicating or permanently losing work.

Source-identified risks, requiring targeted reproduction tests before fixes:

- [CatalogueSyncService.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Automation/CatalogueSyncService.cs)
  only upserts returned products; it does not reconcile upstream withdrawal.
- [DraftModel.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Application/Marketing/Models/DraftModel.cs)
  records a product ID but not the source revision or used-fact snapshot.
- [AutomationService.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Automation/AutomationService.cs)
  derives free slots from an in-memory snapshot; concurrent reservation needs durable enforcement.
- The new-product rule operates after sync persistence; a retry needs durable per-product progress rather than only newly inserted IDs.

### 3. Instagram delivery and explicit connection onboarding

Prioritize Instagram with Telegram as the secondary distribution channel. Retain X capability, but expand it when actual usage warrants it.
WhatsApp should serve an agreed customer-enquiry workflow; it does not need feed-post parity.

Start with single-image Instagram delivery. Add carousels through the same durable lifecycle after the format and approval contracts work.
Manual carousel export can precede automatic carousel publication.

Engineering work:

- Create a reviewed JPEG rendition and freeze the exact bytes used for delivery.
- Add controlled provider-readable media URLs with sufficient lifetime, explicit exposure and cleanup ownership.
- Verify and bind the account using the selected Instagram login mode; explain missing capabilities in Connections.
- Persist container creation and processing checkpoints; resume known containers through bounded status checks.
- Distinguish processing from unknown publication outcomes; never retry an ambiguous publish as a new post.
- Exercise expired URLs, processing delays, restart, token/account changes and lost final responses with fake transports.

The current [Instagram broker](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Providers/Brokers/InstagramSocialPublishingBroker.cs)
supports a single public JPEG and container checkpoints.
[PublicationService.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Infrastructure/Marketing/Services/PublicationService.cs)
currently enables Telegram/X and treats nonterminal broker outcomes as unknown; adding an Instagram configuration flag is insufficient.

External setup remains separate: chosen professional account, login mode, permissions, hosting, secrets and an explicitly authorized live canary.
Meta documents distinct [Instagram Login](https://www.postman.com/meta/instagram/folder/1z5vxzu/instagram-api-with-instagram-login)
and [Facebook Login](https://www.postman.com/meta/instagram/folder/u4g5a2a/instagram-api-with-facebook-login) paths.
The latter requires a linked Facebook Page. Pin the chosen API version and verify its publishing/Insights contract during implementation;
this proposal does not claim every format, interaction sticker or account capability is available through either API.

### 4. Brand integration and SMM access

Consume the existing published Ocharo Brand kit: logo, palette, typography and exact social destinations.
Marketing owns selection and local caching; Brand owns editing and releases.
Snapshot release/appearance identity, asset hashes and relevant tokens into render provenance.
New branding may be adopted explicitly; old approved cards must remain reproducible during a Brand outage or release change.

Marketing currently retains a small local [BrandModel](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Application/Marketing/Models/BrandModel.cs)
and hardcoded renderer styling. The sibling Brand app already owns the required public contract.

For collaboration, add a focused review inbox: needs review → changes requested → approved → ready to post.
Comments refer to an exact revision. Use owner/editor/reviewer capabilities through existing SDK identity/access primitives where applicable.
Do not add agency billing, tenant onboarding or a general workflow builder.

Remote SMM/phone access requires authenticated hosting, private assets, session protection, audited approvals and tested backup/restore.
The current [LocalPilotBoundaryMiddleware.cs](../codebase/ocharo-marketing.backend-services/OcharoMarketing.Api/Marketing/LocalPilotBoundaryMiddleware.cs)
intentionally limits the unauthenticated app to loopback. Removing that guard is not a hosting implementation.
Portable exports and local handoff records can be delivered before remote hosting is chosen.

### 5. Enquiries and comparable results

Give each pack a purpose, content format, product/variant, campaign identifier and approved destination.
Record manual observations immediately; add account-authorized Instagram Insights through a separate broker later.
Keep provider measurements, operator observations and business outcomes distinguishable.

Acceptance:

- Manually published posts appear in Calendar and Results with their actual publication evidence.
- Saves, comments, A/B replies and enquiries can be recorded with observation time, source and comparable window.
- Enquiry, fitting request and order reference are separate outcomes; absent data remains unknown rather than zero.
- Product links target a canonical destination owned by the public-site/Catalogue lane.
- Tagged links identify campaigns; clicks require measured events, and sales require recorded order evidence.
- A/B posts collect audience preferences; unmatched organic audiences do not establish a causal winner.

Dashboard emphasis: review queue, source changes, this week's content mix, ready-to-post items, delivery exceptions and enquiries.
Only add charts when comparable observations exist. The current Results placeholder ideas should become filters over real post metadata.

### 6. Bounded process-video production

Start with one recipe: fabric → cutting/sewing → detail → finished garment.
Provide a shot checklist, real clip upload, trim/order, short overlays, cover selection and 9:16 export.
Reuse durable jobs, provenance, approval and export packaging. Start with original or properly supplied audio;
the operator can finish platform-specific audio/interactions in Instagram where appropriate.

Current uploads are image-only. This requires video media validation, storage, rendering and review,
not another static template. Trend inspiration and a shot list can ship ahead of a full video editor.
Measure whether the workflow gets usable footage and finished posts before adding trend scraping or automated video generation.

## Ownership and reusable boundaries

| Owner | Responsibility |
|---|---|
| Catalogue | Product truth, availability, multilingual source facts and source media |
| Brand | Approved identity, releases, scheduled appearances and official destinations |
| Blog/public site | Stories and customer-facing product destinations |
| Marketing | Content packs, channel adaptations, review, planning, delivery and marketing evidence |
| SDK candidates | Rendering/renditions, typed media, immutable review snapshots, kit caching, durable work and provider adapters |

Use [the existing SDK extraction inventory](../architecture/sdk-extraction.md). Preserve product-specific recipes in Marketing.
Complete reusable capability vectors in their dedicated extraction pass rather than creating duplicate SDK infrastructure here.

## First useful increment

Use three existing garments with different silhouettes to verify the same content-pack workflow.
Deliver hero/facts/Story outputs, an ordered manual carousel package, revision review, and a recorded manual publication.
Include source-diff warnings and preserve generated-versus-photographed provenance.
Run deterministic rendering, stale-review, export-order/hash, idempotent manual-record and responsive preview checks.

This increment can be built without paid image generation, account credentials or live publication.
Add real detail/worn variants when their evidence exists; the system should explain missing inputs instead of fabricating them.
Track preparation time, corrections needed, completed manual posts and recorded enquiries during operator use.
Set numerical targets from that baseline rather than inventing performance promises.

Defer a generalized marketing SaaS, new social networks, full CRM, autonomous posting consent and an unrestricted design/video editor.
The immediate return comes from finishing the creation-to-publication loop for Ocharo.
