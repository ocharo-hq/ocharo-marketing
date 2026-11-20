# ocharo-marketing

Ocharo's recognition, editorial publishing, public applications, distribution and lead platform.
The current marketing desk and both Blog implementations are preserved here.
Marketing owns the frontend workspace: `apps/web` (desk), `apps/editorial` (revisioned editor),
and `apps/{atlas,wear,blog,catalogue}` (independent published artifacts).
The adopted backend remains `ocharo-blog.backend-services`; the old frontend root is a command compatibility shim.
See `engineering/architecture/blog-adoption.md` for independent hosts and source boundaries.
Platform owns canonical brand/product facts; this repository owns editorial and campaign lifecycles.

## Interface language

- English only for every app's interface until the WoW2 SDKs' translation layer is improved (developer, 2026-11-20:
  "we gonna stick to english until we improve the translations layer in all sdks"; the current translations are
  "really not good") — the [Ocharo rule](../../CLAUDE.md) in full.
- Write new interface strings in English only; leave the existing uz and ru tables in place, untouched.
- Authored content languages (posts, stories, catalogues, campaigns) are content and keep their own settings.
- Not yet in code: the desk and the editor still start in Uzbek and offer a language choice; switching them to
  English is later work.

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
├── deployment/ · planning/ · research/ · scripts/
└── codebase/            ← THE CODE — only place code lives
    ├── ocharo-marketing.backend-services/    ← .NET Clean Arch — solution + OcharoMarketing.{Domain} projects (+ tests/)
    └── ocharo-marketing.frontend-services/   ← private pnpm workspace; Vue app in apps/web/
```

Workspace guidance: [Ocharo](../../CLAUDE.md) and [WoW2](../../../../../../../CLAUDE.md).
Conventions: [WoW2 convention index](../../../../../../../conventions/conventions.md).

## Canonical planning

Use `engineering/planning/backlog.md` and `engineering/planning/version-track/v0.1/v0.1.md`.
Previous-work research preserves evidence only. Working rules live in
`engineering/development/rules.md`; owner verification remains open until reviewed.
