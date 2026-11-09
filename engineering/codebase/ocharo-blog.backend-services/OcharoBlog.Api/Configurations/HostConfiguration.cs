using OcharoBlog.Api.Blog;
using WoW.Two.Sdk.Beta.Be.Data;
using WoW.Two.Sdk.Beta.Be.Meta;

namespace OcharoBlog.Api.Configurations;

/// <summary>Extends the host builder and the web application for startup wiring.</summary>
public static partial class HostConfiguration
{
    /// <summary>Configures the application builder (services).</summary>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        builder
            .AddHostDefaults()
            .AddPostgresDatabase()
            .AddAccess()
            .AddInquiries()
            .AddBlog()
            .AddMedia()
            .AddCatalogue()
            .AddBrand()
            .AddSites()
            .AddMediator()
            .AddRuntimeConfig()
            .AddControllers();

        return builder;
    }

    /// <summary>Runs startup tasks, then configures middleware and endpoints.</summary>
    public static async Task<WebApplication> ConfigureAsync(this WebApplication app)
    {
        // A local run creates the database if missing and applies the sets; a deployed boot only verifies their levels,
        // since the deploy's migrate step (dotnet OcharoBlog.Api.dll migrate) applied them before it.
        await app.Services.MigrateBespokeOnStartupAsync(app.Lifetime.ApplicationStopping);
        // Fill Files ids and seed an empty blog before the host accepts requests.
        await app.InitializeBlogAsync();
        return app.UsePipeline().MapEndpoints();
    }

    /// <summary>Adds Editorial's own verb beside the be sdk's <c>migrate</c> and <c>config check</c>:
    /// <c>import-sqlite [--dry-run]</c>, which copies an earlier release's SQLite store into the <c>blog</c> schema
    /// once. It runs on the built host without serving.</summary>
    /// <param name="verbs">The service host's verbs.</param>
    public static ServiceVerbs AddBlogVerbs(this ServiceVerbs verbs)
    {
        ArgumentNullException.ThrowIfNull(verbs);
        return verbs.AddHostVerb(SqliteImportCommand.Name, (services, verb, ct) =>
            SqliteImportCommand.RunAsync(services, verb.Arguments, verb.Output, verb.Error, ct));
    }
}
