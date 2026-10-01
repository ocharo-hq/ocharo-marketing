# Ocharo — four public apps and GitHub organization review

> Historical research snapshot. Repository paths, line references and proposals describe the original inspection; current ownership and entrypoints are in [Blog adoption](../../../../../../architecture/blog-adoption.md).

*Last updated: 2026-09-29 12:40 PM (GMT+5)*

## Status and recommendation

Independent design proposal responding to the clarified scope: **four genuinely separate public applications**, with presentation suited to each purpose. The repository names and product names below are working proposals. No organization name, domain, or trademark availability has been checked; no account, repository, transfer, or publication action was performed.

Recommend one independently deployable repository per public application under the future organization. Shared technical contracts should make the applications cooperate while allowing different screen structures and visual styles. This is an architecture recommendation, not a claim that four new GitHub repositories already exist or that existing histories must be discarded.

**Synthesis outcome:** this report preserves the independent-repository alternative. The [parent repository plan](../../../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-organization-repositories.md) recommends four separately built and deployed apps in one `ocharo-web` repository, retaining distinct designs while sharing consumer contracts. The application separation is confirmed; the final repository topology remains a proposal.

## Four applications

| Public application | Reader purpose | Main screens | Visual direction | Working name / repository |
|---|---|---|---|---|
| **Uzbekistan culture and textile gallery** | Discover cloth, patterns, places, and people through objects and stories | Cultural front page; visual gallery; textile/object detail; narrative story; curated place/type views | Image-led cultural publication: generous images, clear captions, zoom, calm reading, distinctive regional photography | **Uzbek Textile Atlas — by Ocharo** / `ocharo-culture` |
| **Wearing guides** | Decide what to wear for a real place, occasion, weather, or activity | Occasion/need discovery; guide; outfit comparison; saved/shareable outfit or checklist where useful | Contemporary service magazine: energetic outfit imagery, concise recommendations, side-by-side options, practical labels | **Ocharo Style** / `ocharo-style` |
| **Ocharo news** | Follow actual brand developments and collections | Latest news; dated announcement; collection story; event/update archive | Brand newsroom: recognisable Ocharo identity, chronological clarity, strong collection photography, compact updates | **Ocharo News** / `ocharo-news` |
| **Ocharo catalogue** | Find a current item and understand fit, options, price, and purchase/contact route | Collection/product listing; product detail; size/fit help; inquiry/purchase flow | Product-first catalogue: consistent photography, accurate colour presentation, stable filters, clear commercial information | **Ocharo Catalogue** / `ocharo-catalogue` |

“Independent” means each app has its own entry point, navigation, content model, release process, and design suited to its job. Sharing a host, content service, or component package does not negate that separation. Public URLs can be chosen separately from repository ownership.

### Naming choice

Use an **endorsed cultural name** for the culture/gallery app because the reader promise is larger than brand updates. The proposed “Uzbek Textile Atlas” is deliberately descriptive; “by Ocharo” identifies its publisher. Before public launch, shortlist and test names rather than treating this working title as accepted or available.

Use **Ocharo + clear function** for style, news, and catalogue. Their connection to the brand helps readers understand what they are opening. The catalogue should never need a separate invented name before a buyer understands that it contains Ocharo products.

Keep the existing **Ocharo Blog** label until its existing content's role has been decided. The deferred blog-merger decision is not silently resolved by these proposals. The proposed four-app map defines destinations; it does not automatically rename, merge, or delete the present blog.

The future GitHub organization is an **ownership namespace**, not a public product name. A Maftuna-oriented organization name can own Ocharo-branded products without requiring that name in each website masthead. All proposed repository paths should be written as `{chosen-org}/ocharo-culture`, etc., until the user supplies an actual organization.

## What should be shared, and what should vary

| Shared technical contract | Why it belongs across apps | What remains app-specific |
|---|---|---|
| Stable product/model IDs and product links | A wearing guide and news story should refer to the same catalogue item | Catalogue filters, guide layouts, news cards |
| Media reference, credit, rights, dimensions, and alt-text fields | The same asset needs traceable attribution and reliable rendering | Image treatment, crops, gallery density, narrative sequencing |
| Locale codes, translated routes, and canonical link conventions | Cross-app navigation should land in a predictable language | Editorial voice, reading length, display typography |
| Content reference IDs and relationship types | Culture objects, stories, guides, collections, and products can link deliberately | Each app's authoring fields and publishing cadence |
| Named destinations and outbound-link attribution | Each application can route to the real main Ocharo channel consistently | The invitation's timing and wording |
| Accessibility behaviours and basic controls | Keyboard navigation, focus, and dialogs should behave reliably | Palette, layout, image proportions, motion, editorial hierarchy |
| CI/deployment interfaces and observability conventions | Similar operations can be reused without coupling releases | Build configuration and release schedule per app |

Do not share one giant article schema just because three apps publish text. A cultural story connects objects, people, and sources; a wearing guide needs occasion and outfit information; an announcement needs date and collection/event relationships. Small shared fields can coexist with different document types.

Likewise, shared ownership does not require a shared login or a four-app account system. Introduce accounts only where a specific reader feature warrants them. Begin with direct links and independently useful public pages.

A thin “by Ocharo” identity and a consistent link to the brand can connect distinct designs. Forcing the same hero, card layout, and typography everywhere would weaken the user's requested separation.

## Cross-app paths

| Starting point | Useful onward path | Appropriate primary invitation |
|---|---|---|
| Culture story about a textile | Related wearing guide; relevant current catalogue items when the relationship is real | “Explore more textile stories” or “See how to wear it” |
| Occasion-based wearing guide | The exact catalogue items used in a look | “View this look” or “Ask about fit” |
| Collection announcement | That collection's catalogue page | “Explore the collection” |
| Product page | Relevant fit guide or care guidance; main brand contact | A concrete inquiry or purchase action |

Use the confirmed main Ocharo channel as the destination for brand following/contact. Its exact URL must come from the existing brand record; this note does not invent one. Readers need a clear invitation, not a forced redirect away from the content they opened.

## Repository organization

Suggested public-app repositories are `ocharo-culture`, `ocharo-style`, `ocharo-news`, and `ocharo-catalogue`. Existing internal tools, Studio, marketing tools, and Brand Workspace remain separately accountable systems in the wider inventory; four public apps do not mean four repositories for the entire Ocharo operation.

Keep generic reusable infrastructure with its real shared owner. A new Maftuna/Ocharo organization need not copy a multi-brand platform merely to collect its consumer applications under one namespace. Extract a common contract package only when at least two apps genuinely consume it; do not create an empty platform repository for an anticipated need.

For existing standalone repositories, a GitHub transfer can preserve their identity and history. A folder currently living inside a broader workspace needs a scoped extraction plan; transferring that entire broader workspace would be a different action. Repository creation, extraction, and transfer therefore need separate mappings in the parent inventory.

## Official GitHub facts relevant to this plan

Checked against official GitHub documentation on 29 September 2026.

### Organization setup

A new organization starts without repositories; existing repositories can be transferred into it. The user's plan to create an organization is not evidence that it currently exists. [GitHub: creating an organization](https://docs.github.com/en/organizations/collaborating-with-groups-in-organizations/creating-a-new-organization-from-scratch).

A **public** `.github` repository can supply supported default contribution/support/security files and issue/PR templates where an individual repository does not supply its own. These defaults do not become files in each repository's clone or Git history. A license cannot be supplied this way. Treat `.github` as an optional small shared-conventions repository, not an application. [GitHub: default community health files](https://docs.github.com/en/communities/setting-up-your-project-for-healthy-contributions/creating-a-default-community-health-file).

### What a repository transfer preserves

GitHub preserves commit information, issues, pull requests, and repository-associated secrets, webhooks, and deploy keys. Transfer requires repository administration and permission to create repositories in the target organization. Ordinary repository links and Git operations redirect; GitHub Pages sites do not. Reusing the old repository location removes its redirect. Update local remotes and explicit references to the new location. These are documented transfer behaviours, not a verification that any Ocharo repository has completed transfer. [GitHub: transferring a repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/transferring-a-repository).

### Dependencies that need deliberate updating

**Actions and reusable workflows:** GitHub Actions does not follow repository redirects for action or reusable-workflow references. A changed owner/repository name can break callers still using the old name. Update those references as part of a transfer. [GitHub: workflow configuration reference](https://docs.github.com/en/actions/reference/workflows-and-actions/reusing-workflow-configurations).

**Packages:** container, npm, NuGet, and RubyGems packages support account/organization scopes; their owning account does not automatically change with a repository transfer. Repository links and inherited access can be lost. Maven/Gradle repository-scoped packages transfer with their repositories. Check only the registries actually used by the affected apps, then restore access and adjust package references where required. [GitHub: package permissions and transfers](https://docs.github.com/en/packages/learn-github-packages/about-permissions-for-github-packages).

**Secrets:** repository secrets surviving transfer does not imply inherited organization secrets will exist in the destination. Organization secrets have repository-access policies. Confirm the particular secrets each build/deployment uses, without reading their values into logs. On GitHub Free, organization-level secrets are unavailable to private repositories, so the organization plan affects the chosen setup. [GitHub: secrets](https://docs.github.com/en/actions/concepts/security/secrets); [GitHub: using secrets](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets).

The practical migration check is small: map each source repository or source folder, identify its real package/workflow/deployment dependencies, move the approved unit, update those references, and run that app's existing build and deployment verification. This note authorizes none of those account actions.

## Conclusion for parent synthesis

Recommend **four separate public app repositories**, different visual systems suited to each reader task, and a small shared contract surface. Recommend an endorsed cultural name and direct Ocharo names for the other three. Keep the current Ocharo Blog name and the blog-merger question pending. The user-created GitHub organization can become the ownership home after its actual identity and the existing repository/folder inventory are known.
