# Ocharo Blog — separate blog

*Last updated: 2026-09-27 12:15 AM*

## Status

- Standalone static blog, updated from the user's approved layout and editorial feedback.
- Thirty complete English article drafts: ten everyday-clothing stories, ten style stories, ten cloth/craft stories.
- 15,052 body words; individual posts contain 449–554 words, excluding headings, metadata, and bibliography.
- Thirty-five HTML pages: homepage, archive, three topic pages, thirty articles.
- Three agents drafted audience batches; the parent reviewed, polished, integrated, and verified the complete library.
- Current local preview: `http://127.0.0.1:8774/`; owned server session `7555` remains running.
- Editorial analysis and all thirty manuscript links: [editorial report](../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/research/och-res-editorial-posts-2026-09.md).
- Platform integration and the Studio merge remain deferred by the user.
- Local preview only. No cloud deployment, source push, account upload, or public release was performed.

## Editorial direction

The reading promise is clothes, curiosity, and everyday life. Each article offers a useful observation, experiment,
or answer without requiring a purchase. The library covers unworn favourites, fitting-room movements, pockets,
boutique displays, adapting an image, repeating outfits, pattern combinations, ikat edges, museum records,
care labels, and documented making processes.

The previous article-level contact panel is removed. Stories end with a takeaway, one specific question,
and two curated related articles. Existing Instagram and Telegram links remain in the footer.
There are no invented interviews, founder memories, customer testimonials, prices, stock claims, or publication dates.
Cloth/history/care claims have primary-source links near the relevant sections. Engagement remains unmeasured.

The approved hierarchy, script wordmark, ivory/charcoal palette, vermilion accents, and responsive grid remain.
Most cards have typographic covers; product visualisations are optional and labelled. The homepage retains existing collection imagery.
Outgoing-link icons use 20-pixel SVGs with a 2.6 stroke and rounded caps; internal navigation uses directional arrows.
The skip link uses clipped hiding until keyboard focus, preserving keyboard access without an offscreen block.

## Source and editing

| File | Responsibility |
|---|---|
| `och-content.mjs` | Canonical topic names, thirty articles, card prompts, and curated related-story addresses |
| `och-build.mjs` | Validates content references and generates the 35 static HTML routes |
| `dist/och-style.css` | Shared responsive theme; authored directly and retained by the generator |
| `dist/assets/` | Existing approved visual assets |
| `och-asset-provenance.json` | Source locations, image roles, and byte evidence |
| `editorial-work/` | Audience analysis, source budgets, and thirty readable Markdown review manuscripts |
| `och-editorial-counts.json` | Body counts for the integrated library |
| `qa/` | Before/after article screenshots from the actual preview |
| `package.json` | Build and preview commands; no package dependencies |
| `.openai/hosting.json` | Static output descriptor, without a hosted project identity |

Edit `och-content.mjs` and run `npm run build`. The generator resolves `dist/` relative to its own file,
so invocation from another directory still writes only to this site's output. Styles apply on browser reload without a build.
The audience JSON and Markdown files are review snapshots; the builder reads the canonical content module.

Each article contains `slug`, `topic`, `title`, `excerpt`, `intro`, `format`, `readerPromise`, `sections`,
`takeaway`, `closingQuestion`, and `sources`. Sections support paragraphs, optional lists, and zero-based `sourceIndexes`.
Optional images require descriptive `alt` text and a caption. `relatedSlugs` selects two existing articles.

Run `npm run preview` from this directory when the owned server is stopped. Reuse a running preview.
All article text and metadata are present in the initial HTML; navigation uses ordinary document links.

## Routes

```text
/
/journal/
/topics/pieces/
/topics/style/
/topics/cloth/
/journal/{article-slug}/
```

The three original article addresses are preserved with rewritten content:

```text
/journal/the-nimcha-in-a-new-light/
/journal/let-one-piece-lead/
/journal/pattern-placement-and-the-whole-piece/
```

The preview retains `noindex,nofollow` and a restrictive `robots.txt`.
The build covers reading and navigation; it adds no subscriptions, comments, accounts, CMS, order ledger, or analytics.

## Verification

- JavaScript syntax checks passed for the content module and generator.
- Build generated all 35 documents; each has one `h1` and descriptive image text.
- All 40 referenced local pages/assets returned HTTP 200.
- An unknown article returned HTTP 404.
- Thirty unique articles; ten per topic; all bodies exceed 400 words.
- Source indexes, topic keys, article addresses, and related-story references are validated during generation.
- Browser reviewed at 1280-pixel desktop width and 390-pixel phone width; no page overflow.
- Desktop homepage displays nine selected cards in three columns; phone archive displays all thirty in one column.
- Typographic covers fit within their cards at phone width; sampled images load.
- Keyboard activation navigated homepage → archive → textile article.
- The source disclosure expands and reveals the expected two source links on the ikat article.
- Rewritten feature displays its full text, takeaway, question, and two related stories; automatic article contact panels are absent.
- SVG icon weight verified in the rendered document; temporary viewport sizing was reset.
- Existing preview tab retained as the user-facing deliverable.
- Before/after screenshots saved in `qa/`; no public hosting or engagement evidence is implied.

## Deferred analysis

Platform/app ownership, Brand Workspace integration, and any Studio merge remain deferred.
The [previous implementation analysis](../../../../../../../../../../../../../../fam/mft-10x-ws/context/domains/external/work/ventures/ocharo/platform/och-platform-blog-implementation.md)
remains a proposal, rather than the architecture of this static journal.
