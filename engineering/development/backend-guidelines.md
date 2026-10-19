# Backend development

*Last updated: 2026-10-19*

Follow wow-two .NET conventions. Application remains contract-only; Infrastructure owns service behavior; Persistence owns database access. Brokers speak neutral capability models and use fake HTTP handlers in tests. Keep logs free of provider tokens and raw payloads. Card tests include real Chromium exports; install the matching browser before running that category on a new machine.

Both hosts read `appsettings.Target.json` and the machine's `appsettings.Local.json` through the SDK's `AddSettingsFiles`; the `https` launch profiles set `LOCAL_SETTINGS=true`. `appsettings.Development.json` holds no loopback address, local path or local switch. Local-only behavior keys on the SDK's `IsLocalRun`, never `IsDevelopment()` alone; the loopback request boundary stays on for every run. Required local values: [Marketing](../deployment/deployment.md#local-settings), [Editorial](../deployment/blog/deployment.md#local-settings).
