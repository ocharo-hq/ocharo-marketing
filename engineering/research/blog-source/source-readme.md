# Ocharo Blog

The Ocharo Blog as a platform: an Uzbek editor (Studio) for structured stories, and the public blog rendered live from published stories in the approved journal design.

## What it does

- **Studio** (`/studio/`) — stories with title, address, topic, format, excerpt, intro, ordered sections (paragraphs, lists, cited sources), takeaway, closing question, cover image, related stories, catalogue pieces and SEO fields; words and reading time as you type; revision history with restore.
- **Lifecycle** — draft → publish now / schedule (a background worker publishes on time; state survives restarts) → unpublish / archive. Publishing checks excerpt, intro, sections and cover text.
- **Public site** — `/`, `/journal/`, `/journal/{slug}/`, `/topics/{key}/`, `/feed.xml` (RSS), `/sitemap.xml`, `/robots.txt`; the approved journal CSS and markup.
- **Language editions** — English at `/`, Uzbek at `/uz/`, Russian at `/ru/` (a language appears once it has a published story); Studio creates a linked translation draft per language; pages carry `hreflang` alternates, a language switcher, localized interface strings and per-language RSS; topics have uz/ru labels.
- **Launch switch** — site-wide `noindex` stays on until the owner turns it off (robots.txt follows).
- **Previews** — `/preview/{id}` renders any story as the public page would, marked non-public.
- **Catalogue pieces** — stories can show "Pieces in this story" from the Ocharo Catalogue public feed (degrades to nothing when the catalogue is offline).
- **Static export** — `GET /api/blog/export/site.zip`: every published page, CSS, feed, sitemap, robots and referenced images for any static host.
- **Seed** — first boot imports the 30 approved journal stories, 3 topics and journal images (from `Blog:SeedAssetRoot`, hashes recorded).

## Run it (dev)

```bash
cd engineering/codebase/ocharo-blog.backend-services && dotnet run --project OcharoBlog.Api --launch-profile https
```

```bash
cd engineering/codebase/ocharo-blog.frontend-services && pnpm install --frozen-lockfile && pnpm dev
```

- API + public site `https://localhost:8280` / `http://localhost:8281`; Studio via Vite `https://localhost:8284/studio/`.
- Single host: `pnpm build && pnpm run deploy` copies the Studio into `wwwroot/studio/` (ignored by git) → `http://localhost:8281/studio/`.
- Catalogue pieces need the Catalogue API at `Blog:CatalogueBaseUrl` (default `http://localhost:8279`).

## Verify

```bash
cd engineering/codebase/ocharo-blog.backend-services && dotnet test OcharoBlog.BackendServices.slnx
```

```bash
cd engineering/codebase/ocharo-blog.frontend-services/apps/web && pnpm typecheck && pnpm lint && pnpm test
```

## Pointers

- Product: [product/product.md](product/product.md) · state: [product/context.md](product/context.md)
- Architecture: [engineering/architecture/architecture.md](engineering/architecture/architecture.md) · track: [engineering/planning/planning.md](engineering/planning/planning.md)
- Seed conversion: [engineering/scripts/import-journal.mjs](engineering/scripts/import-journal.mjs) (source: sibling `../ocharo-blog-static/och-content.mjs`)
- Siblings: `ventures.ocharo-brand` (logo + links for the header/footer), `ventures.ocharo-catalogue`, `ventures.ocharo-marketing`, `ventures.ocharo-studio`
