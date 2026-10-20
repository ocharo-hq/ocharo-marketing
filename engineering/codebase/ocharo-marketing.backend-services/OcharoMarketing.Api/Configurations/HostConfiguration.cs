using WoW.Two.Sdk.Beta.Be.Foundation.Configuration;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Extends the host builder and the web application for startup wiring.</summary>
public static partial class HostConfiguration
{
    /// <summary>Configures the application builder (services).</summary>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        builder
            .AddSettingsFiles()
            .AddPilotBoundary()
            .AddIntegrationBoundary()
            .AddSettings()
            .AddSqliteDatabase()
            .AddBlobStorage()
            .AddCards()
            .AddMarketing()
            .AddInquiries()
            .AddOperations()
            .AddDelivery()
            .AddAutomation()
            .AddSourceAnalysis()
            .AddOpenAi()
            .AddGemini()
            .AddTelegram()
            .AddX()
            .AddInstagram()
            .AddWhatsApp()
            .AddMediator()
            .AddRuntimeConfig()
            .AddControllers();

        return builder;
    }

    /// <summary>Runs startup tasks, then configures middleware and endpoints.</summary>
    public static WebApplication Configure(this WebApplication app)
    {
        return app.UsePipeline().MapEndpoints();
    }
}
