# Ocharo Marketing — operator experience and first build

*Last updated: 2026-10-19*

## Direction

Build a separate **Ocharo Marketing** venture for Maftuna's actual marketing work. Its first useful experience is an editorial atelier desk: prepare a recognisable product post, review its factual text and selected artwork, arrange its publication, and learn from evidence when channel access exists. The app should make the next useful action obvious without making Maftuna operate a generic SaaS administration console.

This direction supersedes the previous recommendation to place the card composer only inside Brand Workspace. Reusable modules remain useful, but an organization switcher, fixture tenant, public signup, agency billing, and a full general brand platform are not the initial product experience. The existing Brand Workspace work remains independently owned; reuse or integration requires its real contracts rather than assuming source presence means a shared runtime.

The user chose **atlas → simple cards**. The atlas is the design discussion and selection surface. Daily post creation uses the selected named presets. Family selection does not itself approve product facts or a particular generated output for publication.

## Evidence that shapes the interface

| Current evidence | UX consequence |
|---|---|
| Ocharo display spelling is fixed; current script wordmark assets exist | Use Ocharo in the app and preserve actual handles; reuse the logo without redrawing |
| Front view is the preferred cover; T01/T02 are feed candidates | Start the composer with a large front cutout, editorial bone/charcoal and half-fabric directions |
| Product cutouts exist across short Nimcha, long Abaya, and wide Kostyum silhouettes | Make actual products visible immediately; framing must use the garment's visible bounds |
| `nimcha-v06` has confirmed classification and draft localized names | Source readiness should distinguish image availability, fact confirmation, and wording review |
| T01's fabric image is a derived crop, not a separate swatch | Show a draft/study option and a specific matching-fabric requirement; never imply actual weave evidence |
| T03/T04 need genuine detail/worn evidence; T05 needs real alternatives | Atlas can expose the families without claiming every template is eligible for final export |
| External brand audit observed covers only | Do not turn those examples into claimed engagement benchmarks |
| Channel connections and credentials have not been verified | Show disconnected/incomplete states, not invented impressions, followers, or publishing success |

## Visual direction

Use warm ivory canvas, white paper surfaces, charcoal text, and one muted plum accent. Product textiles supply the stronger colours. An optional very restrained grid or paper treatment can make the atlas section feel like a journal; ordinary forms should remain quiet and legible. Rounded corners should be moderate, with fine borders and restrained shadows. Avoid a wall of identical metric boxes.

The app wordmark is the approved transparent Ocharo artwork with a small **Marketing** descriptor. UI text uses a clear sans serif; selected cards use their versioned typography. Titles can use a restrained editorial serif, but dense control labels and calendar entries use the sans serif. Do not add old all-caps brand spellings or unsupported luxury/made-to-order positioning.

At desktop width, use an approximately 216-pixel navigation rail and a spacious content area. The dashboard's dominant element is an actual next-post preview, not a promotional illustration. Keep a pinned primary action in the top bar: **Post yaratish**. A search affordance finds products and saved posts, not external accounts by default. The bottom of the rail holds Connections and Settings; account setup should not compete with today's work.

On small screens, keep Today, Posts, Create, Calendar, and More accessible. The composer becomes a step flow with the preview available throughout. Review should be practical on a phone. Complex atlas comparison can open as a focused two-candidate view rather than squeezing an A3 spread onto the screen.

## Navigation and vocabulary

Initial Uzbek labels below are proposed interface copy, not approved product translations. Keep all copy in a locale dictionary so native review can improve it without changing behavior. Provide Uzbek, Russian, and English display options; saved post language is independent of interface language.

| Destination | Proposed Uzbek label | Primary job |
|---|---|---|
| Today | Bugun | Continue preparation and resolve the next actionable issue |
| Posts | Postlar | Find drafts, reviewable posts, exported items, and actual publications |
| Products & Media | Mahsulotlar | Choose existing product facts and reusable source images |
| Atlas | Shablonlar | Compare candidate presets and record the selected directions |
| Calendar | Reja | Arrange drafts and distinguish planned from automatically scheduled work |
| Results | Natijalar | Inspect available engagement observations with their source and freshness |
| Connections | Ulanishlar | Configure channel accounts and generation providers |

Use ordinary task language: **Continue**, **Check the text**, **Choose a photo**, **Download**, **Plan**. Keep engineering words such as recipe hash, worker lease, DTO, and tenant outside the user flow. Technical provenance remains available in a secondary details view when it helps explain a result.

## Today: a useful home even before connections exist

### Dominant next-post panel

Show the most recently edited draft or a selected imported product. Use a clean card preview beside its short title, preparation state, destination intent, and the one next action. Examples: **Continue your post**, **Review this title**, **Choose a matching fabric photo**. If no draft exists, use an actual product card and **Create your first post**, rather than an empty illustration.

The preview should be recognisable before reading labels. It can use the actual Nimcha front cutout on the bone editorial preset. Keep source draft status outside the artwork in the interface; draft downloads can use a clear review mark.

### Compact work strip

Count actual local drafts, items needing review, planned posts, and exported packages. These are workflow counts computed from stored state. Show no reach/follower cards until a real observation source exists. Each count filters Posts or Calendar rather than being decorative.

### Supporting areas

1. **Coming up:** a short agenda with real planned items, or an empty agenda with **Add a post to the plan**.
2. **Product shelf:** several existing front cutouts with **Create post** actions, using current source labels and review badges.
3. **Needs attention:** specific actionable input issues, limited to a few items. Do not reproduce every unresolved source issue as a giant warning dashboard.
4. **Channel strip:** Instagram, Telegram, WhatsApp, and X with factual connected/disconnected state. Disconnected state offers **Set up**, while already usable manual export remains visible.

Only introduce a trend chart once observations exist. The disconnected Results panel should explain which connected account and supported observation path supplies data, then offer the appropriate connection action.

## Post creation and editing

### Bounded workflow

1. **Product:** select from the actual imported library; show the front cutout and concise source facts. A blank text-only or uploaded-photo post remains possible.
2. **Purpose and preset:** default to Product introduction. Choose from the selected editorial and half-fabric presets. Show a miniature of the actual chosen product in each candidate where feasible.
3. **Card and caption:** edit a short display title, optional material/order cue, caption, post language, and intended channel. Put caption editing beside the card, not inside its art layers.
4. **Review:** inspect full export and phone-size proof. Show only issues relevant to the facts and media actually used.
5. **Save/export/plan:** preserve the draft; download a card or publication package; add a planned date. Automatic scheduling is available only through a configured and implemented provider route.

The desktop composer uses a narrow product/preset panel, a generous card stage, and a caption/review panel. Below roughly tablet width it becomes one column. The preview wrapper scales a fixed-size composition; changing screen width must not reflow the post itself.

### Daily controls

- Product, preset, post language, display title, caption, and destination intent.
- A small optional information group: material, availability/order cue, price when relevant and known.
- **Adjust image** reveals bounded scale/position and reset, only when automatic framing needs correction.
- Half-fabric reveals selected fabric asset and crop focus. No swatch means a specific unavailable state for final use; editorial remains possible.

Avoid arbitrary font menus, layer selection, rotation, ornament drawers, and freeform drag-and-drop initially. The atlas records art direction once so the operator does not repeat that decision for every post.

### Text drafting

Offer **Use product facts** immediately: assemble a factual caption draft from the current imported record, skipping unknown values and distinguishing draft translations. AI assistance can later offer a brief introduction, A/B question, or short version from the selected facts. Suggested text is a candidate, never a silent replacement of the user's wording. Preserve the prior caption when applying a suggestion and provide undo/revert.

Never invent composition, stock, fabrication method, care instructions, or the price for the unrecorded 2–9-piece range. A hero without price can proceed without resolving every pricing field. Source approval and finished-post review are separate.

## Scanning and import

Interpret **scanning** as extracting/importing product/media/text and checking readiness. This is an explicit working interpretation; it does not promise social-account scraping or competitor monitoring.

The first useful scan is deterministic: read a selected record/import, check media existence and role, inspect wording status, and determine eligible presets. Present results as **Found**, **Needs your review**, and **Missing**, with a short explanation and direct correction action. Match the scan to the selected purpose; a matching swatch matters for half-fabric, a real worn photograph matters for worn context, and two real variants matter for an A/B design poll.

Image/document text extraction, when implemented through an actual configured provider, produces proposed fields next to the source preview. The operator accepts or corrects them before saving canonical display facts. Preserve original text and source identity. Repeated import should update deliberately and retain the existing product identity rather than create duplicate products.

## Atlas and simple cards

Atlas navigation opens an editorial comparison library with large cards, family names, named variant axes, and one short explanation of the job each family performs. Selected presets appear first; experimental families remain visibly marked as studies.

T01 compares fabric side while holding split constant, or compares split while holding side constant. T02 compares bone versus charcoal with the same front view. T03/T04 discuss image dominance and headline readability. T05 describes a genuine colour/design choice when alternatives exist; the existing front-versus-angle comparison is a presentation study. T06 is a readable facts slide, usually in a carousel.

Selection records a named preset and version. It does not expose every combinatorial setting in the daily composer. Comparison PDF export uses exact finished card PNGs, with its notes and candidate labels outside the artwork. Keep the PDF view separate from the social card download.

## Calendar, publication states, and metrics

Calendar supports a practical weekly agenda first, with an optional month view. A draft placed on a date is **Planned**. A real provider job accepted by the scheduling system is **Scheduled**. Exporting files is **Exported**. Only a provider receipt or explicitly recorded manual evidence establishes **Published**; manual publication evidence is visibly attributed.

A disconnected calendar item remains planned and offers download/manual handoff. It must not display **Scheduled automatically** because an operator chose a date. Rescheduling updates a plan freely; automatic work must follow its real API contract and current state.

Results should initially show an honest empty state and optional manual observation entry. When connected, each measurement displays provider, account, observed period, retrieval time, and whether it is current, delayed, unsupported, or unavailable. Unknown is not zero. Organic comparisons can describe differences; they do not establish that a template caused better sales or engagement.

## Channel and AI-provider connections

Use a consistent connection card for Instagram, Telegram, WhatsApp, and X. Each has service icon/name, account identity if known, connection state, actually supported actions, last verification, and one next action. Unsupported or unimplemented provider routes should describe the limitation and keep manual handoff usable; a button must not simulate a successful account connection.

Generation providers occupy a separate area of Connections: OpenAI and Gemini. Secrets stay in the backend configuration flow; the frontend shows only configured state and masked metadata. The Generation view retains reference thumbnails, prompt, purpose, provider, output history, and review state. Purpose can be product cutout, contextual styling concept, or background study. Text and card placement remain ordinary deterministic composition after a chosen image is saved.

With no usable key/provider implementation, explain **Configure a provider to generate images** and keep editing/exporting existing images available. Never substitute a source image and call it a newly generated result. Existing generated garment cutouts retain their recorded provenance and fidelity caveats.

## Initial implementable slice

Implement a polished local app with real product inputs, stored drafts, the Today dashboard, bounded card/caption editor, selected preset library, local readiness scan, clean export, and a planned-post agenda. Include honest Results/Connections views rather than fake engagement. The full channel and provider surfaces can be visible while capability-specific actions depend on implemented backend contracts and actual configuration.

The first practical demonstration should be:

**Open Today → choose an existing Nimcha → use the editorial preset → edit its Uzbek caption → inspect the phone-size card → save → export → assign a planned date.**

This gives Maftuna useful work without waiting for channel credentials. Local use does not settle hosted access timing, account authorization, or provider availability. The parent engineering owner selects the storage/render/API contracts; the interface must accurately reflect their capabilities.

## Proposed Vue boundaries

Names below are proposed responsibilities, not existing source components.

| Boundary | Responsibility |
|---|---|
| `MarketingShell` | Navigation, route title, Create action, language, responsive layout |
| `TodayPage` | Next post, actual workflow counts, agenda, product shelf, next actions |
| `PostsPage` / `PostListItem` | Search/filter stored drafts and exact workflow state |
| `PostComposerPage` | Coordinate product, preset, caption, review, save, export, and plan |
| `ProductPicker` / `ProductAssetPicker` | Choose factual source and explicitly typed image role |
| `CardComposition` | Fixed-size renderable visual; no navigation/editor chrome |
| `CardPreview` | Scale wrapper, phone-size view, authoritative output inspection |
| `CaptionEditor` | Source-fact insertion, draft suggestion, undo, locale |
| `ReadinessSummary` | Template-specific missing/draft facts and useful next action |
| `AtlasPage` / `PresetComparison` | Compare directions, select named presets, export journal |
| `CalendarPage` / `PlannedPostItem` | Weekly/monthly plan and honest execution state |
| `ResultsPage` | Observation/empty/manual-entry states |
| `ConnectionsPage` / `ConnectionCard` | Channel/provider configuration and actual capabilities |
| `GenerationPanel` | Reference/prompt/provider/result review for actual generation jobs |

Use typed API/view models and one shared service/store for products, posts, plans, connection capabilities, and generation jobs. Keep form draft state separate from persisted source data. Async completion must not overwrite newer edits. A failed save preserves the user's work and allows retry through the actual contract.

## SDK reuse and candidates

Existing Studio imports prove Vue SDK capabilities for persistent browser state, dirty-navigation protection, file handling, and toast feedback. Reuse those current exports when the chosen SDK version is available. Do not copy Studio's brand-specific canvas or state database.

Inspect the SDK for buttons, form fields, select/tabs, dialogs, status badges, empty states, file upload, and accessible feedback before inventing product duplicates. Potential future reusable additions are a constrained image-framing control, asynchronous-job status panel, channel connection state card, and date/agenda primitives. Extract a component when its behavior and independent use are demonstrated; keep Ocharo palette, product fields, preset artwork, and captions in the venture.

## Acceptance examples

1. **Real input, honest state.** Nimcha V06 loads its existing front cutout and source wording status. Editing saves a local draft. Editorial export is readable at phone size. No fabricated stock, material, reach, or publication status appears.
2. **Long and wide products.** Abaya V01 and a wide Kostyum keep the full intended silhouette visible using the same fixed-size card. Long names require reviewed short titles rather than silently shrinking essential text. Source pending review stays visible.
3. **Missing external access.** A planned Instagram item has no connected account. It remains Planned, downloads its exact card/caption package, and Results explains absent observations. Image generation with no configured provider shows a real setup state, not a synthetic success.
4. **Preset continuity.** The chosen preset's exact exported PNG is reused in an atlas spread. Candidate labels and discussion notes are absent from clean card bytes.
5. **State recovery.** Navigation with unsaved edits preserves work or asks through existing dirty-navigation behavior. A failed save or render shows an actionable state without silently discarding the caption.

## Sources inspected

- [Brand guidelines](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/och-brand-guidelines.md) — current display spelling and approved presentation direction.
- [Brand asset library](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/brand/och-brand-assets.md) — existing logo exports and selected profile.
- [Card system](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/assets/templates/och-product-cards/README.md) — six families, asset dependencies, and current proof limits.
- [Post-pattern benchmark](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/assets/templates/och-product-cards/och-post-pattern-benchmarks-2026-09-23.md) — observed covers, mobile proof, and family disposition.
- [Post automation proposal](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-post-automation.md) — prior bounded composer and shared-card/atlas model.
- [Brand Workspace source audit](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-brand-workspace-audit.md) — prior integration boundary and current missing card capabilities; does not establish this venture's implementation.
- [Nimcha V06 record](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/products/nimcha-v06/och-nimcha-v06.json) and [Abaya V01 record](/Users/max/Projects/10x-ws/workbench/fam/mft-10x-ws/context/domains/external/work/career/designer/portfolio/products/abaya-v01/och-abaya-v01.json) — real input facts and review states; both front cutouts were visually inspected.
- [Studio app](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/engineering/codebase/ocharo-studio.frontend-services/src/bootstrap/App.vue) and [UI tokens](/Users/max/Projects/10x-ws/workbench/career/engineering/wow-two/wow-two-ws/workbench/mfts-projects/ventures/ocharo-hq/ocharo-ws/workbench/ventures.ocharo-studio/engineering/codebase/ocharo-studio.frontend-services/src/bootstrap/styles/tokens.css) — actual SDK imports and existing compatible warm palette.

No source was edited, no new app was implemented, no account was connected, and no publication or provider generation occurred in this analysis. Official API capability details belong to the parent provider/engineering research lane.
