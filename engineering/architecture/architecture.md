# Architecture

*Last updated: 2026-09-27*

Use .NET Clean Architecture: Domain values, Application interfaces/models, Infrastructure behavior/brokers, Persistence row access, and API host composition. Use the five-layer Vue source arrangement and public wow-two Vue SDK exports. SQLite is an explicit single-operator pilot choice; durable jobs are database-backed, not an in-memory queue.

- [Marketing contract](marketing-contract.md): HTTP models, states, invariants, lane ownership.
- [SDK extraction](sdk-extraction.md): neutral capability inventory and exit criteria.
- [Provider research](../research/providers.md): official external contracts and current limitations.

No dependency references the moving Brand Workspace checkout. Secrets, assets, and provider operations stay behind the API. The local pilot enforces loopback/same-origin access; remote deployment requires an authenticated operator boundary and deployment review.
