using OcharoBlog.Application;
using OcharoBlog.Infrastructure;
using OcharoBlog.Persistence;

namespace OcharoBlog.Api.Configurations;

/// <summary>Per-concern host registration extensions for the OcharoBlog API.</summary>
public static class HostConfigurationExtensions
{
    public static WebApplicationBuilder AddPersistenceLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddPersistence(builder.Configuration);
        return builder;
    }

    public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath,
            runScheduler: builder.Configuration.GetValue("Blog:RunScheduler", true));
        return builder;
    }

    public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();
        return builder;
    }

    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        return builder;
    }
}
