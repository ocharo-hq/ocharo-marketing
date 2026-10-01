# ventures.ocharo-blog

Ocharo Blog — Uzbek Studio for structured stories plus the live public blog (approved journal design), RSS/sitemap, scheduling and static export.

## Lazy loading

- Do NOT pre-read files at conversation start; open a file only when the task needs it.
- `.claude/rules/file-references.md` is a lookup table, not a reading list.
- Navigate code under `engineering/codebase/` with `tree`/`find`/`grep` — it is not listed in references.

## Structure

```
product/                 ← the WHAT/WHY (no code): product · context · features · flows · planning · marketing
engineering/             ← the HOW
├── architecture/        ← system + per-area design
├── development/         ← this repo's dev guidelines (defer to wow-two-ws/conventions/)
├── deployment/ · planning/ · versions/ · research/ · scripts/
└── codebase/            ← THE CODE — only place code lives
    ├── ocharo-blog.backend-services/    ← .NET Clean Arch — solution + OcharoBlog.{Domain} projects (+ tests/)
    └── ocharo-blog.frontend-services/   ← private pnpm workspace; Vue app in apps/web/
```

Workspace guidance: [Ocharo](../../CLAUDE.md) and [WoW2](../../../../../CLAUDE.md).
Conventions: [WoW2 convention index](../../../../../conventions/conventions.md).

## Local rules

- Local pilot: loopback only, no accounts; cross-origin writes rejected. Public hosting goes through the static export until authentication exists.
- `noindex` stays on until the owner launches; never flip it on her behalf.
- Seed content comes from the sibling [standalone Ocharo Blog](../ocharo-blog-static/); that folder is read-only input.
- Runtime data (`App_Data/`) and the built Studio (`wwwroot/*`) stay out of git. Every public value is HTML-encoded in `SiteRenderer`.
