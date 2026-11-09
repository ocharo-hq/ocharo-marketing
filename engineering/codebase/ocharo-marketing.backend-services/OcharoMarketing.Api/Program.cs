using OcharoMarketing.Api.Configurations;
using WoW.Two.Sdk.Beta.Be.Meta;

// Answer a verb, or build, configure and serve the host. `migrate` and `config check` are the be sdk's own and
// `import-sqlite` Marketing's: each runs on the built host and exits with its code before any startup work runs.
return await ServiceHost.RunAsync(
    args, builder => builder.Configure(), app => app.ConfigureAsync(), verbs => verbs.AddMarketingVerbs());

public partial class Program;
