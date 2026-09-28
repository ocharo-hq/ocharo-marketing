# Ocharo Marketing foundation plan

*Last updated: 2026-09-27*

## Direction

Create the independent temporary venture `ventures.ocharo-marketing`, brand `OcharoMarketing`, under wow-two's `workbench/ventures/`. Start with Maftuna's single-operator local workflow. Parent's latest direction selects SQLite instead of the original PostgreSQL request. Keep Brand Workspace read-only and outside both build and runtime dependencies.

## Verified foundation

- The canonical `create-repo` skill is `.claude/skills/create-repo/SKILL.md` in `wow-two-ws`; its script is `.claude/skills/create-repo/scaffold.sh`.
- Resolved command: `./.claude/skills/create-repo/scaffold.sh ventures.ocharo-marketing OcharoMarketing ventures single`.
- Source template: `workbench/wow-two-sdk-beta/wow-two-sdk-beta.product-template/`; inspected HEAD `bf6dfacee965e1fafd6cdf84fb0f4167d49f5120`.
- Template supplies `.NET 10` Domain, Application, Infrastructure, Persistence, Api and Tests, SQLite, Vue 3, TypeScript, Vite, Tailwind 4, pnpm workspace, Docker and single-host serving.
- Template frontend is already `apps/web/`, consumes `@wow-two-beta/ui-vue@0.0.7`, and pins `pnpm@10.33.2` with Node `>=24.11.0 <25`.
- Node `v24.11.0` is available at `/Users/max/.nvm/versions/node/v24.11.0/bin/node`. Default `/usr/local/bin/node` is `v20.10.0`; set PATH deliberately for installation/build/runtime commands.
- Template backend pins old SDK `10.0.21-beta`. Use `WoW2.Sdk.Backend.Beta@10.0.59-beta`; cached NuGet package metadata records release SHA `6924fa6fbce151d45a1089fb0632176f93691134`. Do not consume similarly named `.local.*` versions.
- Brand Workspace's registered `workbench/ventures/ventures.brand-workspace` directory is absent at audit time; `active.sh` and repo registry still reference it. No mutable Brand Workspace code was used.
- Scaffold creates Git state without committing and updates `scripts/active.sh` and the port ledger. No worktrees.

## Required scaffold corrections

1. Upgrade central SDK package version; align direct dependency versions with the published package's floors, including `Microsoft.AspNetCore.OpenApi >=10.0.12`.
2. Convert classic scaffold solution to `.slnx` and group `/Services/`, `/Tests/` per current architecture convention; fix active.sh path accordingly. Skill's classic solution description is older than current architecture rules.
3. Remove greeting-only sample behavior as the product vertical replaces it; preserve current layer direction.
4. Move handlers/behavior into Infrastructure. Current clean-architecture owner says Application contains contracts/commands/queries; the template's greeting handler in Application is stale precedent.
5. Replace `EnsureCreated` with a chosen migration strategy for durable evolving app data. Use EF-owned migrations for this EF/SQLite app; do not add unmanaged schema edits.
6. Verify host serves built SPA while unknown `/api/*` routes return JSON 404, not index.html.

## Ports

- Ledger previously declared next backend pair `8276` HTTPS / `8277` HTTP.
- Parent scaffold appeared during audit with those exact API ports.
- Candidate scan found no other launch/Vite/Compose references to `8278`, `8280`, or `8282` and no listeners on candidates at audit time.
- Let parent own final allocation and ledger changes; allocation is not proof of runtime readiness.
- Template Vite source is `8224`, matching scaffold substitution. Ledger's template row says `8225`; this is ledger drift, not an established script bug.
- For local-first mode, bind API and Vite to loopback. Avoid Docker's all-interface port publishing defaults if introducing Compose.

## Reuse package boundaries

| Capability | Reuse now | Product ownership |
|---|---|---|
| Request/result infrastructure | SDK mediator, `Result<T>`, `DomainError`, API error helpers | Product requests, mapping, decisions |
| Frontend primitives | `@wow-two-beta/ui-vue` public exports | Product routes, compositions, review workflow |
| Asset bytes | SDK `IBlobRepository` and `AddLocalBlobStorage` | Asset catalog, approved revisions, ownership, MIME/size validation |
| Authentication primitives | SDK `AddCookieAuthentication`, `AddDefaultDenyAuthorization` | Actual account/bootstrap/login and app authorization policy |
| Database testing | SDK `RelationalTestDb<TContext>`, `WebApiTestHost<T>` | App fixtures, SQLite behavior and API contracts |
| Provider calls | SDK HTTP safety/resilience where compatible | Capability-named brokers and provider adapters |
| Durable work, later PostgreSQL | SDK `AddPostgresHangfireJobs` | Workflow status and idempotence |

Published package XML confirms the auth/storage/Hangfire methods above exist in `10.0.59-beta`; this is package API evidence, not provider runtime verification.

## Local durable worker

For the selected SQLite direction, do not use SDK `AddInMemoryHangfireJobs`: its jobs disappear on restart. SDK currently supplies a PostgreSQL persistent preset, not an SQLite preset.

Recommended smallest product implementation:

- A persisted job row includes stable ID, input revision, kind, state, attempts, timestamps, lease owner/expiry, and output/error metadata.
- Create the queued row transactionally with the requested work/revision; return its ID immediately.
- A single hosted polling worker creates a dependency-injection scope per claimed job, never captures a scoped DbContext in a singleton.
- Claim by a conditional update inside a transaction. Completion checks the same claim identity. An abandoned lease can be recovered after restart.
- Work is retryable with stable output identities. Write asset bytes to a temporary filename and publish atomically; record completion only after bytes exist.
- Restart recovery must reconcile persisted job state rather than clearing it. Never silently regenerate approved immutable revisions.
- Cancellation and transient failures produce visible states. Keep failure messages sanitized; provider secrets stay in settings.
- The queue persists one product's workflow; do not build a generic workflow platform during this seed.

At migration to multi-instance PostgreSQL, preserve product job IDs and revision semantics while replacing dispatch with SDK Hangfire. Job engine reuse does not itself guarantee exactly-once effects.

## Auth and exposure

Local-first mode must be explicit: loopback binding, known Host checks and same-origin write checks. A localhost listener alone does not protect writes against cross-origin browser requests. No provider operations run merely because the host starts.

SDK cookie helpers are primitives, not a completed login system. Before non-local exposure, implement authenticated owner access, persistent data-protection keys, secure cookies, CSRF defense and default-deny API authorization. Keep exported private assets behind authorized delivery. Do not expose a job dashboard or filesystem root as anonymous static content.

## Code placement and provider seam

- Domain: entities/value types with no provider implementation references.
- Application: request/query models, broker/repository/service interfaces and product contracts.
- Infrastructure: handlers, FoundationServices/ProcessingServices, scoped worker and external provider brokers.
- Persistence: DbContext, entity configuration, migrations and row access.
- Api: domain-grouped controllers/HTTP DTOs and host composition.
- Broker interface names the owned capability, e.g. `IImageGenerationBroker`; implementation names provider, e.g. `OpenAiImageGenerationBroker`.
- Provider clients own wire models, URLs, headers and transport resilience. Brokers translate to app models and own degradation decisions.
- Parent owns cross-lane contracts; backend implementation must not race the provider agent's broker files.

## First verification

- Build against published SDK and declared dependencies, with Node24 selected.
- Check Vue typecheck, targeted component tests and production build.
- Check backend unit/API behavior and SQLite queue claiming, persistence, lease recovery, and idempotent completion.
- Verify unknown API route JSON 404, loopback restriction, cross-origin write rejection, and assets outside static wwwroot.
- Restart the managed host with a pending persisted job and demonstrate recovery.
- Keep runtime sessions managed and verify readiness before handing parent a URL.

## Evidence and limits

Read wow-two root instructions, convention index, repo/frontend standard, create-repo skill, port ledger, clean architecture, broker/integration rules, persistence/test database rules, frontend workspace, single-host serving, SDK instructions and package registry. No repository writes, builds, live provider operations or asset generation were performed by this audit. The only authored file is this plan.

Memory was used only for discovery: `MEMORY.md:1320-1329` pointed to published SDK `10.0.59-beta`; local cached package metadata and published package XML then verified the APIs and version. Rollout ID: `01a086b9-bca7-7612-a697-980ed3fe1b87`.
