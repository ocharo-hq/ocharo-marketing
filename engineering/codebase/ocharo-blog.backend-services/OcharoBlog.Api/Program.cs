using OcharoBlog.Api.Configurations;
using WoW.Two.Sdk.Beta.Be.Meta;

// Answer a verb (`migrate`, `config check`, `import-sqlite`), or build, configure and serve the host.
return await ServiceHost.RunAsync(args, builder => builder.Configure(), app => app.ConfigureAsync(),
    verbs => verbs.AddBlogVerbs());

public partial class Program;
