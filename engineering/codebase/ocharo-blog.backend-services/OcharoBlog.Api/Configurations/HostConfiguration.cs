using System.Reflection;
using OcharoBlog.Api.Blog;

namespace OcharoBlog.Api.Configurations;

/// <summary>Configures the OcharoBlog host: admin API, live public site, previews and the Studio SPA at /studio/.</summary>
public static class HostConfiguration
{
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("ocharo-inquiries", client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddHttpClient("ocharo-platform-tickets", client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        builder
            .AddPersistenceLayer()
            .AddInfrastructureLayer()
            .AddApplicationLayer()
            .AddApiServices();

        return builder;
    }

    public static WebApplication Configure(this WebApplication app)
    {
        app.UseMiddleware<BlogExceptionMiddleware>();
        app.UseMiddleware<LocalPilotBoundaryMiddleware>();
        app.UseMiddleware<IntegrationBoundaryMiddleware>();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        // Studio SPA (built by `pnpm run deploy` into wwwroot/studio); the public site is rendered by controllers.
        // Routing ignores trailing slashes, so the bare /studio → /studio/ redirect runs before endpoint matching.
        app.Use((context, next) =>
        {
            if (context.Request.Path.Value == "/studio")
            {
                context.Response.Redirect("/studio/");
                return Task.CompletedTask;
            }

            return next(context);
        });
        app.UseStaticFiles();

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OcharoBlog", mode = "local-pilot", version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev" }));
        app.MapGet("/api/system/status", () => Results.Ok(new { status = "healthy", service = "OcharoBlog", version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev" }));
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: 404, title: "API route not found"));
        app.MapFallbackToFile("/studio/{**path:nonfile}", "studio/index.html");

        return app;
    }
}
