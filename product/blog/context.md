# Ocharo Blog — Product Context

*Last updated: 2026-09-28*

## Current state

v0.1 built 2026-09-28: seeded with the 30 approved journal stories (published inside the platform; site-wide noindex on). Browser-checked locally: public home and story pages, Studio list and editor, create → save → publish → live page (the QA story was archived afterwards). No public release.

## Open questions

- Launch domain and hosting (static export target vs authenticated live host)?
- Uzbek/Russian editions: translate existing stories or write new ones per language?
- Which catalogue pieces belong under which stories?

## Decisions

- 2026-09-28 — Separate platform repo (user: "different platforms"); the standalone journal becomes the seed, not a dependency.
- 2026-09-28 — Server-rendered public pages (SEO, no JS needed) + static export for hosting.
