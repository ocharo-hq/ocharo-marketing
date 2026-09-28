using OcharoMarketing.Api.Marketing;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Composes the local marketing API and its production-built Vue shell.</summary>
public static class HostConfiguration
{
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment() || !builder.Configuration.GetValue("Marketing:LocalPilot", true))
            throw new InvalidOperationException("This pilot requires Development and local-only access. Remote hosting requires authenticated deployment configuration.");
        builder.AddPersistenceLayer().AddInfrastructureLayer().AddApplicationLayer().AddApiServices();
        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        app.UseMiddleware<MarketingExceptionMiddleware>();
        app.UseMiddleware<LocalPilotBoundaryMiddleware>();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OcharoMarketing", mode = "local-pilot" }));
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: 404, title: "API route not found"));
        app.MapFallbackToFile("index.html");
        return app;
    }
}
