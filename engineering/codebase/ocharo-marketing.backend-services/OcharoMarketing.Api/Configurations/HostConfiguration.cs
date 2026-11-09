using OcharoMarketing.Api.Import;
using WoW.Two.Sdk.Beta.Be.Data;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Meta;
using WoW.Two.Sdk.Beta.Be.Web.ProblemDetails;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Extends the host builder and the web application for startup wiring.</summary>
public static partial class HostConfiguration
{
    /// <summary>Configures the application builder (services).</summary>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        // Lay the SDK boot floor before any product seam. It reads the settings file a target mounts
        // (appsettings.Target.json) and, on a local run, this machine's (appsettings.Local.json) before the
        // registrations below read them. The desk SPA is served from this host. Behind the TLS-terminating ingress,
        // the same-origin write check and the BFF need the forwarded scheme: loopback and Deployment:TrustedProxies
        // are trusted.
        builder.AddApiDefaults(options =>
        {
            options.ServiceName = "ocharo-marketing";
            options.EnableOtlpExporters = false;
            options.EnableHttpsRedirection = !builder.Environment.IsDevelopment();
            options.SpaHosting = _ => { };
            options.TrustProxiesFrom(builder.Configuration.GetSection("Deployment"));
        });

        builder
            .AddSettings()
            .AddPostgresDatabase()
            .AddImportGate()
            .AddAccess()
            .AddBlobStorage()
            .AddAssetStorage()
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
    public static async Task<WebApplication> ConfigureAsync(this WebApplication app)
    {
        app.WarnAboutRejectedRuntimeConfig();
        // A local run applies the `marketing` and `distributed_cache` sets; a deployed boot only checks their levels,
        // which the deploy's migrate step (dotnet OcharoMarketing.Api.dll migrate) brought current.
        await app.Services.MigrateBespokeOnStartupAsync(app.Lifetime.ApplicationStopping);
        app.WarnAboutPendingImport();

        // Outermost: every error status, MVC error result and thrown exception leaves as a ProblemDetails document.
        app.UseHttpProblemDetails();

        // SDK pipeline: forwarded headers, secure headers, compression and the desk SPA before routing; Marketing's
        // boundary, the pending-import gate and sign-in after routing; then the rate limiter (the public inquiry
        // budget), /health with its database check and the SPA fallback, which answers an unmatched /api/* with a
        // JSON 404.
        app.UseApiDefaults(pipeline => pipeline.UseMarketingBoundary().UseImportGate().UseAccess());

        app.MapControllers();
        app.MapIdentityBffEndpoints();
        app.MapSystemStatus();
        return app;
    }

    /// <summary>Adds Marketing's own verb beside the be sdk's <c>migrate</c> and <c>config check</c>:
    /// <c>import-sqlite [--dry-run]</c>, which copies an earlier release's SQLite store once and runs on the built host
    /// without serving.</summary>
    /// <param name="verbs">The service host's verbs.</param>
    public static ServiceVerbs AddMarketingVerbs(this ServiceVerbs verbs)
    {
        ArgumentNullException.ThrowIfNull(verbs);
        return verbs.AddHostVerb(ImportSqliteCommand.Name, (services, verb, ct) =>
            ImportSqliteCommand.RunAsync(services, verb.Arguments, verb.Output, verb.Error, ct));
    }
}
