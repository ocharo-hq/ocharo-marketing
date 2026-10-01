using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OcharoMarketing.Api.Marketing;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Composes the local marketing API and its production-built Vue shell.</summary>
public static class HostConfiguration
{
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("ocharo-platform-tickets", client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder.Services.AddRateLimiter(options => { options.RejectionStatusCode=429;
            options.AddPolicy("public-inquiries",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"local",
                _=>new FixedWindowRateLimiterOptions{PermitLimit=8,Window=TimeSpan.FromMinutes(1),QueueLimit=0})); });
        if (!builder.Environment.IsDevelopment() || !builder.Configuration.GetValue("Marketing:LocalPilot", true))
            throw new InvalidOperationException("This pilot requires Development and local-only access. Remote hosting requires authenticated deployment configuration.");
        builder.AddPersistenceLayer().AddInfrastructureLayer().AddApplicationLayer().AddApiServices();
        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        app.UseMiddleware<MarketingExceptionMiddleware>();
        app.UseMiddleware<LocalPilotBoundaryMiddleware>();
        app.UseMiddleware<IntegrationBoundaryMiddleware>();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRateLimiter();
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OcharoMarketing", mode = "local-pilot", version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev" }));
        app.MapGet("/api/system/status", () => Results.Ok(new { status = "healthy", service = "OcharoMarketing", version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev" }));
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: 404, title: "API route not found"));
        app.MapFallbackToFile("index.html");
        return app;
    }
}
