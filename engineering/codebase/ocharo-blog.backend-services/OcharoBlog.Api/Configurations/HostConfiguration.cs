using WoW.Two.Sdk.Beta.Be.Foundation.Configuration;

namespace OcharoBlog.Api.Configurations;

/// <summary>Extends the host builder and the web application for startup wiring.</summary>
public static partial class HostConfiguration
{
    /// <summary>Configures the application builder (services).</summary>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        builder
            .AddSettingsFiles()
            .AddIntegrationBoundary()
            .AddInquiries()
            .AddSqliteDatabase()
            .AddClock()
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
        // Preserve schema creation and seeding before the host accepts requests.
        await app.InitializeBlogAsync();
        return app.UsePipeline().MapEndpoints();
    }
}
