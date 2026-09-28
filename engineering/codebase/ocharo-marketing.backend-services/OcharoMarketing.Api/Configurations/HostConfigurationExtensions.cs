using OcharoMarketing.Application;
using OcharoMarketing.Infrastructure.Marketing.BackgroundServices;
using OcharoMarketing.Infrastructure;
using OcharoMarketing.Persistence;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Per-concern host registration extensions for the OcharoMarketing API.</summary>
public static class HostConfigurationExtensions
{
    /// <summary>Configures the persistence layer (EF Core SQLite context).</summary>
    public static WebApplicationBuilder AddPersistenceLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddPersistence(builder.Configuration);
        return builder;
    }

    /// <summary>Configures the infrastructure layer (clock and other abstraction implementations).</summary>
    public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder;
    }

    /// <summary>Configures the application layer (kit mediator + handlers).</summary>
    public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();
        return builder;
    }

    /// <summary>Configures API services — controllers and OpenAPI.</summary>
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<SeedImportHostedService>();
        builder.Services.AddHostedService<GenerationWorker>();
        builder.Services.AddHostedService<PublicationWorker>();
        builder.Services.AddHostedService<OcharoMarketing.Infrastructure.Automation.AutomationWorker>();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        return builder;
    }
}
