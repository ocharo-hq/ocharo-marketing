# File References

> **Lookup table only.** Do NOT read files proactively — only when the current task requires them.
> **Tracks docs outside `engineering/codebase/`.** Source files (`.cs`/`.tsx`) are navigated via
> `tree`/`find`/`grep`, never listed here.

| Question about | Read |
|---|---|
| Product — what / who / model / positioning | `product/product.md` |
| Product state, tasks, decisions | `product/context.md` |
| Features (per-feature specs) | `product/features/features.md` |
| User / product flows | `product/flows/flows.md` |
| Product milestones, roadmap | `product/planning/planning.md` |
| Marketing — GTM, channels, campaigns | `product/marketing/marketing.md` |
| Technical overview · stack · map | `engineering/engineering.md` |
| System / area architecture | `engineering/architecture/architecture.md` |
| What services live in the code | `engineering/codebase/codebase.md` |
| Backend dev guidelines | `engineering/development/backend-guidelines.md` |
| Frontend dev guidelines | `engineering/development/frontend-guidelines.md` |
| Iteration / version workflow | `engineering/development/iteration-guide.md` |
| Engineering roadmap, tracker, decisions | `engineering/planning/planning.md` |
| Engineering backlog | `engineering/planning/backlog.md` |
| Operational rules | `engineering/planning/rules.md` |
| Deploy / ops | `engineering/deployment/deployment.md` |
| Versioned iteration docs | `engineering/versions/versions.md` |
| Technical research dumps | `engineering/research/research.md` |
| Dev / ops scripts | `engineering/scripts/scripts.md` |

## Source projects (`engineering/codebase/`)

> Individual files NOT listed — use `tree`/`find`/`grep`. Describe projects only.

### `codebase/ocharo-blog.backend-services/`
| Project | What it is |
|---|---|
| `OcharoBlog.Api` | Host: admin API, public site + preview controllers, Studio SPA at `/studio/`, seed data |
| `OcharoBlog.Application` | `ArticleService` (lifecycle, revisions), topics, settings, `SiteRenderer` + `SiteService` (live pages, feed, sitemap, export) |
| `OcharoBlog.Domain` | `ArticleDocument`, `TopicDocument`, `SiteSettings`, records |
| `OcharoBlog.Infrastructure` | Media store, catalogue feed client, scheduled publishing worker, journal seeder |
| `OcharoBlog.Persistence` | EF Core SQLite + revision-checked repository |
| `tests/OcharoBlog.Tests` | Lifecycle, public site, export and hosting tests (in-memory host) |

### `codebase/ocharo-blog.frontend-services/`
| Project | What it is |
|---|---|
| `apps/web` | Vue 3 Studio (Uzbek), base `/studio/`: stories, structured editor with live preview, topics, site settings |
