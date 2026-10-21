# Architecture

*Last updated: 2026-10-21*

Use .NET Clean Architecture: Domain values, Application interfaces/models, Infrastructure behavior/brokers, Persistence row access, and API host composition. Use the five-layer Vue source arrangement and public wow-two Vue SDK exports. SQLite is an explicit single-operator pilot choice; durable jobs are database-backed, not an in-memory queue.

- [Marketing contract](marketing-contract.md): HTTP models, states, invariants, lane ownership.
- [SDK extraction](sdk-extraction.md): neutral capability inventory and exit criteria.
- [Provider research](../research/providers.md): official external contracts and current limitations.

No dependency references the moving Brand Workspace checkout. Secrets, assets, and provider operations stay behind the API. The local pilot enforces loopback/same-origin access; remote deployment requires an authenticated operator boundary and deployment review.

## Ocharo frame and app links

Marketing (`apps/web`) and Editorial (`apps/editorial`, served at `/studio/`) wear the suite frame Desk, Catalogue and
Studio carry ([agreement](../../../../docs/architecture.md#suite-frame-and-app-links)):

- One `AppLayout` in each app's `bootstrap/`, mounted through a layout route, built from the SDK `AppShell`: the glass
  top bar holds the `AppSwitcherMenu` (the "O" mark, subtitle "Ocharo") with the app version, the breadcrumb from route
  metadata, the app's primary action (New post and Refresh, or New story) and the SDK `AccountMenu` last.
- The sidebar is the SDK `SidebarMenu`, collapsible to an icon rail and remembered under
  `ocharo.marketing.sidebar-collapsed` / `ocharo.editorial.sidebar-collapsed`; tablets always show the rail and phones
  swap it for a `BottomNavMenu` with More. Marketing groups its sections as Create, Plan, Results and Settings;
  Editorial lists Stories, Topics, Site settings and the public site in a new tab.
- The SDK `themes.css` `theme-ocharo`, one `ColorModeProvider` and the generated `colorModeScript` pre-paint
  (`ocharo.marketing.theme`, `ocharo.editorial.theme`) replace the fixed light palettes; Editorial keeps Georgia
  (`--font-editorial`) for authored story content. The theme carries the approved frame glass: the top bar and sidebar
  use the SDK `glass` variants, the menus and phone sheets `glass-menu`, so no app copies glass values.
- Mounted editors stay inside a `<fieldset :disabled>` while signed out; Reconnect opens Desk sign-in in a new tab.
- The account menu names the person with the display name and email `GET /api/sso/session` returns, and names the
  session ("Signed in through Ocharo Desk") only when Desk sends neither. Ocharo account is a link row to Desk's origin
  in a new tab.
- The account menu signs out through `POST /api/sso/logout` with the in-memory CSRF token. Sign-out is held, with a
  notice, while an editor's existing unsaved-edit guard would warn or a save is in flight.

Each host serves `GET /api/runtime-config` (anonymous, `no-store`) from optional `OcharoApps:*` settings, mapped to
`OCHARO_{DESK,CATALOGUE,STUDIO,MARKETING,EDITORIAL,BRAND,ATLAS,WEAR,BLOG}_URL`. A value must be an absolute HTTPS address
without credentials, query or fragment; any other value is left out and the host logs one startup warning. Health,
status and runtime config share one technical-route list (`TechnicalRoutes`) that both the Ocharo SSO gate and the
integration boundary read — `GET`/`HEAD`, any letter case, with or without a trailing slash. The loopback pilot
boundary still applies. The SPAs resolve the apps menu through `packages/connect`: the served values re-keyed under
`VITE_`, then the app's own `importMetaEnvSource(import.meta.env)`, then the local-run defaults.
