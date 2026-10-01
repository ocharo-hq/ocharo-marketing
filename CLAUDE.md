# ocharo-marketing

Ocharo's recognition, editorial publishing, public applications, distribution and lead platform.
The current marketing desk and both Blog implementations are preserved here.
Marketing owns the frontend workspace: `apps/web` (desk), `apps/editorial` (revisioned editor),
and `apps/{atlas,wear,blog,catalogue}` (independent published artifacts).
The adopted backend remains `ocharo-blog.backend-services`; the old frontend root is a command compatibility shim.
See `engineering/architecture/blog-adoption.md` for independent hosts and source boundaries.
Platform owns canonical brand/product facts; this repository owns editorial and campaign lifecycles.

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
    ├── ocharo-marketing.backend-services/    ← .NET Clean Arch — solution + OcharoMarketing.{Domain} projects (+ tests/)
    └── ocharo-marketing.frontend-services/   ← private pnpm workspace; Vue app in apps/web/
```

Workspace guidance: [Ocharo](../../CLAUDE.md) and [WoW2](../../../../../CLAUDE.md).
Conventions: [WoW2 convention index](../../../../../conventions/conventions.md).
