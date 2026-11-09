# Tests

*Last updated: 2026-11-09*

> Backend test projects for Ocharo Editorial (the Blog backend): three tiers, one project each, and one shared
> support library.

## Prerequisites

- .NET 10 SDK.
- Docker: Integration and E2E run PostgreSQL in one Testcontainers container per test run (`BlogDatabase`); Unit needs
  no container.
- No Identity, WoW2 Files, Desk or network: E2E replaces the BFF sign-in with a header-driven handler (`EditorialSignIn`),
  Files with an in-memory broker (`TestFiles`) and Identity's token endpoint with the SDK's fake issuer.
- xUnit with AwesomeAssertions; time-dependent paths run on `FakeTimeProvider`. Versions are central in
  `Directory.Packages.props`.

## Projects

| Project | Covers |
|---|---|
| `OcharoBlog.Tests.Unit` | Pure logic with no I/O: Markdown rendering and its policy, the image worker's session checks, the error statuses, the lost save race rule |
| `OcharoBlog.Tests.Integration` | Below HTTP, through the host's services on a real PostgreSQL database: the story lifecycle, revisions and formatted saves |
| `OcharoBlog.Tests.E2E` | Full HTTP flows through the host: access and service tokens, problem contracts, the public site and apps, catalogue, brand, media and Files, editing, the SQLite import, runtime config |
| `OcharoBlog.Tests.Support` | Shared fixtures, not a test project: the PostgreSQL template (`BlogDatabase`) with its Identity stand-in (`IdentityStub/`), the host (`BlogHarness`), sign-in (`EditorialSignIn`), an earlier release's SQLite store (`LegacyStore`) and result reads (`Refusals`) |

- Test names follow `{Unit}_Should{Expectation}_When{Condition}`.
- E2E-only host wiring stays in E2E: `EditorialHost`, `TestFiles`, `IdentitySignalSender`, `RacingBlogRepository`.

## Run

From `engineering/codebase/ocharo-blog.backend-services/`:

```bash
dotnet test OcharoBlog.BackendServices.slnx -c Release -p:BuildSpa=false
```

- One tier: `dotnet test tests/OcharoBlog.Tests.Unit -c Release -p:BuildSpa=false` (likewise `.Integration`, `.E2E`).
- `-p:BuildSpa=false` skips the Studio's `pnpm` build; the backend tests do not need it.
