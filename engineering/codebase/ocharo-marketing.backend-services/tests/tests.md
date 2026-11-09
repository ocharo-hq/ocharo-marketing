# Tests

*Last updated: 2026-11-09*

> Backend test projects for Ocharo Marketing: three tiers, one project each, over one shared support project.

## Prerequisites

- .NET 10 SDK.
- Docker for Integration and E2E: one PostgreSQL 18 container per run (Testcontainers), and a database per test copied from a template Marketing's own `migrate` verb brought current. The reaper removes the container when the run ends.
- Playwright's Chromium for the two browser card tests in Integration (`CardBrowserRenderingTests`): `pwsh tests/OcharoMarketing.Tests.Integration/bin/Release/net10.0/playwright.ps1 install chromium` after a build.
- No real accounts or providers: E2E replaces Identity sign-in with a cookie-driven test handler (`MarketingTestIdentity`) and WoW2 Files with an in-memory broker set (`TestFiles`); provider brokers answer from in-memory HTTP handlers.
- `LivePlatformContractTests` read running Catalogue and Blog feeds only when `OCHARO_CATALOGUE_URL` or `OCHARO_BLOG_URL` is set; otherwise they return at once.
- xUnit with AwesomeAssertions, `FakeTimeProvider` for controlled clocks and the be sdk's bounded polling (`PollingExtensions.UntilAsync`) for background work; versions are central in `Directory.Packages.props`.

## Tiers

| Project | Covers |
|---|---|
| `OcharoMarketing.Tests.Unit` | Pure logic with no I/O: card composition (`HtmlCardRenderingService` over an unused browser), provider brokers over in-memory HTTP, receipt reconciliation, the access middleware, the error catalogue's statuses and exception mapping |
| `OcharoMarketing.Tests.Integration` | Below HTTP, against real PostgreSQL: repositories, services and workers for posts, deliveries, operations, enquiries, source analysis and automation; the `migrate` and `import-sqlite` verbs; browser card renders |
| `OcharoMarketing.Tests.E2E` | Full HTTP flows through the host (`WebApplicationFactory<Program>`): sign-in and access rules, the pending-import gate, problem details, runtime config, system status, assets on WoW2 Files |
| `OcharoMarketing.Tests.Support` | Shared fixtures, no tests: the PostgreSQL template (`MarketingPostgres`, `MarketingDatabase`), the Identity stand-in set (`IdentityStub/`), `MarketingTestIdentity`, the earlier release's SQLite fixtures (`Import/Fixtures/`, `LegacyStore`), card fixtures and `ErrorStatus` |

- `MarketingHost`, `CapturedLogs` and `TestFiles` are product host wiring and live in the E2E project.
- Test names follow `{Unit}_Should{Expectation}_When{Condition}`.

## Run

From `engineering/codebase/ocharo-marketing.backend-services/`:

```bash
dotnet test OcharoMarketing.BackendServices.slnx -c Release -p:BuildSpa=false
```

- One tier: `dotnet test tests/OcharoMarketing.Tests.Unit -c Release -p:BuildSpa=false` (likewise `.Integration`, `.E2E`).
- `-p:BuildSpa=false` skips the frontend `pnpm` build; the backend tests do not need it.
