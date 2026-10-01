# Engineering

*Last updated: 2026-09-27*

One .NET API serves the built Vue app and owns the local SQLite data. A durable local queue handles card rendering and configured generation. Provider brokers contain neutral request/response mapping; product services own approval, snapshots, schedules, and budgets/policy. Shared contracts and lane ownership are in [architecture](architecture/marketing-contract.md). The [handoff](research/previous-work/handoff.md) records verification and active runtime state.
