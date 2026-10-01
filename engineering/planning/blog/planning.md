# ventures.ocharo-blog — Engineering Planning

*Last updated: 2026-09-28*

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| r0.1 — first build | Studio, lifecycle + scheduling, live public site, uz/ru editions (hreflang), RSS/sitemap/robots, preview, static export, catalogue pieces, journal seed | Built + verified locally 2026-09-28; owner review open |
| r0.2 — reach | Newsletter capture, inline section images, story → Marketing publish hook | Planned |
| r0.3 — hosting | Authentication for Studio/API, deployment, analytics decision | Planned |

## Component tracker

| Component | State | Notes |
|---|---|---|
| Backend | 18 tests green | seed (30 stories), lifecycle, scheduling, preview privacy + encoding, feed/sitemap/robots, export, studio hosting, language editions, brand kit (live + fallback, event badge live-only); opt-in live catalogue pieces |
| Frontend | typecheck, lint, 6 tests, build | Studio browser-checked: list, editor, create → publish → live |
| Catalogue pieces | wired | needs the Catalogue API running |

## Decisions

- 2026-09-28 — Server-rendered public site in the API host; Studio under `/studio/`.
- 2026-09-28 — Seeded stories are published inside the platform (matching the existing preview) while noindex stays on.
- 2026-09-28 — Logo and footer links come from Ocharo Brand when it answers; blog settings stay as the offline fallback.
