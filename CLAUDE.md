# ventures.ocharo-marketing

Maftuna’s local marketing desk for product cards, copy, channel planning, and measured response.

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

Conventions: wow-two-ws/conventions/
