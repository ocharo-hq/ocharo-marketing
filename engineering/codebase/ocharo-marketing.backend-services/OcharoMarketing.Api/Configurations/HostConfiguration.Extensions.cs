using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Api.Marketing;
using OcharoMarketing.Api.RuntimeConfig.Settings;
using OcharoMarketing.Application;
using OcharoMarketing.Application.Cards.Brokers;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Infrastructure.Automation;
using OcharoMarketing.Infrastructure.Cards.Brokers;
using OcharoMarketing.Infrastructure.Cards.Services;
using OcharoMarketing.Infrastructure.Cards.Settings;
using OcharoMarketing.Infrastructure.Inquiries;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.BackgroundServices;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using OcharoMarketing.Infrastructure.Scanning;
using OcharoMarketing.Persistence;
using OcharoMarketing.Persistence.Inquiries;
using OcharoMarketing.Persistence.Marketing.Repositories;
using OcharoMarketing.Persistence.Scanning;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Ef;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Foundation.Configuration.ConfigCheck;
using WoW.Two.Sdk.Beta.Be.Foundation.Options;
using WoW.Two.Sdk.Beta.Be.Jobs.Core;
using WoW.Two.Sdk.Beta.Be.Jobs.Core.PersonOperations;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Observability.HealthChecks;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Web.Json;
using WoW.Two.Sdk.Beta.Be.Web.ProblemDetails;

namespace OcharoMarketing.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Registers the host-wide <c>Marketing</c> settings and declares the section Marketing's own root, so
    /// <c>config check</c> and every boot refuse an invalid value or a key no record binds.</summary>
    private static WebApplicationBuilder AddSettings(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<MarketingOptions>(
            builder.Configuration, MarketingOptions.SectionName, rules => rules
            .ValidateSetting(nameof(MarketingOptions.DataDirectory), "required",
                options => !string.IsNullOrWhiteSpace(options.DataDirectory), "must name the data folder")
            .ValidateSetting(nameof(MarketingOptions.MaximumAssetBytes), "range",
                options => options.MaximumAssetBytes > 0, "must be above zero")
            .ValidateSetting(nameof(MarketingOptions.MaximumMetricReadsPerHour), "range",
                options => options.MaximumMetricReadsPerHour is >= 0 and <= 100, "must be from 0 to 100")
            .ValidateSetting(nameof(MarketingOptions.MetricRefreshCooldownSeconds), "range",
                options => options.MetricRefreshCooldownSeconds >= 1, "must be at least 1")
            .ValidateSetting(nameof(MarketingOptions.PlatformBaseUrl), "url",
                options => IsOrigin(options.PlatformBaseUrl), "must be an absolute http(s) address")
            .ValidateSetting(nameof(MarketingOptions.CatalogueBaseUrl), "url",
                options => IsOrigin(options.CatalogueBaseUrl), "must be an absolute http(s) address")
            .ValidateSetting(nameof(MarketingOptions.BlogBaseUrl), "url",
                options => IsOrigin(options.BlogBaseUrl), "must be an absolute http(s) address"));
        builder.Services.AddSettingsRoot(MarketingOptions.SectionName);
        return builder;
    }

    /// <summary>Tells whether an optional origin setting is unset or an absolute HTTP(S) address.</summary>
    private static bool IsOrigin(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    /// <summary>Registers the SQLite database, its migration runner and its readiness check on
    /// <c>/health</c>.</summary>
    private static WebApplicationBuilder AddSqliteDatabase(this WebApplicationBuilder builder)
    {
        var dataRoot = Path.GetFullPath(builder.Configuration["Marketing:DataDirectory"] ?? "App_Data");
        Directory.CreateDirectory(dataRoot);
        var connectionString = builder.Configuration["ConnectionStrings:OcharoMarketing"]
            ?? $"Data Source={Path.Combine(dataRoot, "marketing.db")};Default Timeout=30";
        builder.Services.AddDbContext<OcharoMarketingDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddEfMigrationsRunner<OcharoMarketingDbContext>(options => options.MaxConnectAttempts = 1);
        builder.Services.AddHealthChecks().AddDatabaseReadinessCheck<OcharoMarketingDbContext>();
        return builder;
    }

    /// <summary>Registers the shared local blob storage: the whole asset store in a run without WoW2 Files, and the assets and
    /// exports made before Files in a run with it.</summary>
    private static WebApplicationBuilder AddBlobStorage(this WebApplicationBuilder builder)
    {
        var dataRoot = Path.GetFullPath(builder.Configuration["Marketing:DataDirectory"] ?? "App_Data");
        builder.Services.AddLocalBlobStorage(Path.Combine(dataRoot, "blobs"));
        return builder;
    }

    /// <summary>Holds the root URL of the WoW2 Files instance Marketing stores its assets and draft exports in; unset, they stay
    /// in the local blob folder.</summary>
    private const string FilesBaseUrlKey = "Files:BaseUrl";

    /// <summary>Registers the asset store: WoW2 Files over HTTP when <see cref="FilesBaseUrlKey"/> is set, by tokens of
    /// Marketing's own Identity client (a person's exchanged within their session, Marketing's own otherwise), else the local
    /// blob folder. Both read at resolve time, so a test host's settings decide. The boot backfill runs after the migrations
    /// runner registered before it.</summary>
    private static WebApplicationBuilder AddAssetStorage(this WebApplicationBuilder builder)
    {
        builder.Services.AddFilesClient(_ => { });
        builder.Services.AddOptions<FilesClientOptions>().Configure<IConfiguration>((files, configuration) =>
        {
            if (configuration[FilesBaseUrlKey] is not { Length: > 0 } url)
                return;
            files.BaseAddress = new Uri(url);
            if (!Uri.TryCreate(configuration["Identity:Client:Authority"], UriKind.Absolute, out var authority))
                return;
            files.Identity = new FilesClientIdentityOptions
            {
                TokenEndpoint = new Uri(authority, "connect/token"),
                ClientId = configuration["Identity:Client:ClientId"] ?? string.Empty,
                ClientSecret = configuration["Identity:Client:ClientSecret"] ?? string.Empty,
            };
        });
        builder.Services.AddScoped<LocalAssetService>();
        builder.Services.AddScoped<FilesAssetService>();
        builder.Services.AddScoped<FilesAssetBackfill>();
        builder.Services.AddScoped<IAssetService>(provider => UsesFiles(provider)
            ? provider.GetRequiredService<FilesAssetService>()
            : provider.GetRequiredService<LocalAssetService>());
        builder.Services.AddHostedService<FilesBackfillHostedService>();
        return builder;
    }

    /// <summary>Tells whether this run stores assets and draft exports in WoW2 Files.</summary>
    internal static bool UsesFiles(IServiceProvider provider) =>
        provider.GetRequiredService<IConfiguration>()[FilesBaseUrlKey] is { Length: > 0 };

    /// <summary>Registers card-rendering settings, the browser adapter and rendering services.</summary>
    private static WebApplicationBuilder AddCards(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(new CardRendererSettings(builder.Configuration["Cards:ExecutablePath"]));
        builder.Services.AddSingleton<IDocumentRenderingBroker, PlaywrightDocumentRenderingBroker>();
        builder.Services.AddSingleton<ICardRenderingService, HtmlCardRenderingService>();
        return builder;
    }

    /// <summary>Registers marketing records, orchestration, seed import and generation work.</summary>
    private static WebApplicationBuilder AddMarketing(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMarketingRepository, MarketingRepository>();
        builder.Services.AddScoped<MarketingService>();
        builder.Services.AddHostedService<SeedImportHostedService>();
        // A job someone asked for stops when they lose access; signing out leaves it running. Signals reach it through the BFF's route.
        builder.Services.AddPersonOperations();
        builder.Services.AddHostedService<GenerationWorker>();
        return builder;
    }

    /// <summary>Registers public inquiries, product verification and their request budget.</summary>
    private static WebApplicationBuilder AddInquiries(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IInquiryRepository, InquiryRepository>();
        builder.Services.AddScoped<InquiryService>();
        builder.Services.AddHttpClient<IPublishedProductVerifier, PublishedProductVerifier>(
            client => client.Timeout = TimeSpan.FromSeconds(5));
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("public-inquiries", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });
        return builder;
    }

    /// <summary>Registers operational records, reporting and worker health.</summary>
    private static WebApplicationBuilder AddOperations(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IOperationsRepository, OperationsRepository>();
        builder.Services.AddScoped<OperationsService>();
        builder.Services.AddSingleton<WorkerHealthService>();
        return builder;
    }

    /// <summary>Registers delivery records, publishing policy, observations and publication work.</summary>
    private static WebApplicationBuilder AddDelivery(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        builder.Services.AddScoped<DeliveryPolicy>();
        builder.Services.AddScoped<PublicationService>();
        builder.Services.AddScoped<MetricObservationService>();
        builder.Services.AddScoped<ReceiptReconciliationService>();
        builder.Services.AddHostedService<PublicationWorker>();
        return builder;
    }

    /// <summary>Registers automation orchestration, catalogue and story clients, and the unattended pass every
    /// <c>Marketing:AutomationInterval</c>.</summary>
    private static WebApplicationBuilder AddAutomation(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<AutomationService>();
        builder.Services.AddScoped<AutomationRunner>();
        builder.Services.AddHttpClient<CatalogueSyncService>(client => client.Timeout = TimeSpan.FromSeconds(60));
        builder.Services.AddHttpClient<BlogStoryService>(client => client.Timeout = TimeSpan.FromSeconds(15));
        // A pass syncs the catalogue over HTTP and renders drafts; the worker health marks it stale after 11 minutes.
        builder.Services.AddPeriodicJob<AutomationRunner>(
            "marketing-automation", (runner, ct) => runner.RunScheduledAsync(ct),
            (options, root) =>
            {
                options.Interval = root.GetRequiredService<MarketingOptions>().AutomationInterval;
                options.PassTimeout = TimeSpan.FromMinutes(10);
            });
        return builder;
    }

    /// <summary>Registers source-analysis records and external marketing analysis.</summary>
    private static WebApplicationBuilder AddSourceAnalysis(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ISourceAnalysisRepository, SourceAnalysisRepository>();
        builder.Services.AddScoped<ISourceAnalysisService, SourceAnalysisService>();
        builder.Services.AddScoped<IExternalMarketingService, ExternalMarketingService>();
        return builder;
    }

    /// <summary>Registers OpenAI settings, image generation and content assistance.</summary>
    private static WebApplicationBuilder AddOpenAi(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<OpenAiProviderOptions>(
            builder.Configuration, "Providers:OpenAi", _ => { });
        builder.AddProvider<OpenAiImageGenerationBroker>();
        builder.AddProvider<OpenAiContentAssistanceBroker>();
        builder.Services.AddTransient<IImageGenerationBroker>(
            provider => provider.GetRequiredService<OpenAiImageGenerationBroker>());
        builder.Services.AddTransient<IContentAssistanceBroker>(
            provider => provider.GetRequiredService<OpenAiContentAssistanceBroker>());
        return builder;
    }

    /// <summary>Registers Gemini settings and image generation.</summary>
    private static WebApplicationBuilder AddGemini(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<GeminiProviderOptions>(
            builder.Configuration, "Providers:Gemini", _ => { });
        builder.AddProvider<GeminiImageGenerationBroker>();
        builder.Services.AddTransient<IImageGenerationBroker>(
            provider => provider.GetRequiredService<GeminiImageGenerationBroker>());
        return builder;
    }

    /// <summary>Registers Telegram settings, account verification, publishing and receipt reconciliation.</summary>
    private static WebApplicationBuilder AddTelegram(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<TelegramProviderOptions>(
            builder.Configuration, "Providers:Telegram", _ => { });
        builder.AddProvider<TelegramAccountVerificationBroker>();
        builder.AddProvider<TelegramSocialPublishingBroker>();
        builder.Services.AddSingleton<IReceiptReconciliationBroker, TelegramReceiptReconciliationBroker>();
        builder.Services.AddTransient<IProviderVerificationBroker>(
            provider => provider.GetRequiredService<TelegramAccountVerificationBroker>());
        builder.Services.AddTransient<ISocialPublishingBroker>(
            provider => provider.GetRequiredService<TelegramSocialPublishingBroker>());
        return builder;
    }

    /// <summary>Registers X settings, account verification, publishing, metrics and receipt reconciliation.</summary>
    private static WebApplicationBuilder AddX(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<XProviderOptions>(
            builder.Configuration, "Providers:X", _ => { });
        builder.AddProvider<XAccountVerificationBroker>();
        builder.AddProvider<XSocialMetricsBroker>();
        builder.AddProvider<XReceiptReconciliationBroker>();
        builder.AddProvider<XSocialPublishingBroker>();
        builder.Services.AddTransient<IReceiptReconciliationBroker>(
            provider => provider.GetRequiredService<XReceiptReconciliationBroker>());
        builder.Services.AddTransient<IProviderVerificationBroker>(
            provider => provider.GetRequiredService<XAccountVerificationBroker>());
        builder.Services.AddTransient<ISocialMetricsBroker>(
            provider => provider.GetRequiredService<XSocialMetricsBroker>());
        builder.Services.AddTransient<ISocialPublishingBroker>(
            provider => provider.GetRequiredService<XSocialPublishingBroker>());
        return builder;
    }

    /// <summary>Registers Instagram settings and publishing.</summary>
    private static WebApplicationBuilder AddInstagram(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<InstagramProviderOptions>(
            builder.Configuration, "Providers:Instagram", _ => { });
        builder.AddProvider<InstagramSocialPublishingBroker>();
        builder.Services.AddTransient<ISocialPublishingBroker>(
            provider => provider.GetRequiredService<InstagramSocialPublishingBroker>());
        return builder;
    }

    /// <summary>Registers WhatsApp settings and publishing.</summary>
    private static WebApplicationBuilder AddWhatsApp(this WebApplicationBuilder builder)
    {
        builder.Services.AddValidatedSettings<WhatsAppProviderOptions>(
            builder.Configuration, "Providers:WhatsApp", _ => { });
        builder.AddProvider<WhatsAppSocialPublishingBroker>();
        builder.Services.AddTransient<ISocialPublishingBroker>(
            provider => provider.GetRequiredService<WhatsAppSocialPublishingBroker>());
        return builder;
    }

    /// <summary>Registers a provider HTTP transport with its existing request budget and redirect policy.</summary>
    private static WebApplicationBuilder AddProvider<T>(this WebApplicationBuilder builder) where T : class
    {
        builder.Services.AddHttpClient<T>(client => client.Timeout = TimeSpan.FromMinutes(5))
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        return builder;
    }

    /// <summary>Registers the mediator against the application assembly.</summary>
    private static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediator(typeof(ApplicationAssembly).Assembly);
        return builder;
    }

    /// <summary>Registers the public runtime settings the SPA reads at startup: the Ocharo app and site addresses.</summary>
    private static WebApplicationBuilder AddRuntimeConfig(this WebApplicationBuilder builder)
    {
        // Every address is optional, and a malformed one is left out rather than failing the host over a menu link.
        builder.Services.AddValidatedSettings<OcharoAppsSettings>(builder.Configuration, OcharoAppsSettings.SectionName, _ => { });
        return builder;
    }

    /// <summary>Warns once about each configured app address the runtime config leaves out.</summary>
    private static void WarnAboutRejectedRuntimeConfig(this WebApplication app)
    {
        var rejected = app.Services.GetRequiredService<OcharoAppsSettings>().RejectedSettings();
        if (rejected.Count > 0)
            app.Logger.LogWarning("Runtime config leaves out {Settings}: each must be a plain HTTPS address.", string.Join(", ", rejected));
    }

    /// <summary>Registers HTTP problem details and controllers on the SDK wire contract.</summary>
    private static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpProblemDetails();
        builder.Services.AddControllersWithSdkJson();
        return builder;
    }

    /// <summary>Configures Marketing's request boundary: rejected commands as ProblemDetails, then the response headers
    /// and the same-origin write check, ahead of sign-in.</summary>
    private static IApplicationBuilder UseMarketingBoundary(this IApplicationBuilder app)
    {
        app.UseMiddleware<MarketingExceptionMiddleware>();
        app.UseMiddleware<MarketingHttpBoundaryMiddleware>();
        return app;
    }

    /// <summary>Configures the anonymous service status the desk reads its version from.</summary>
    private static WebApplication MapSystemStatus(this WebApplication app)
    {
        app.MapGet("/api/system/status", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoMarketing",
            version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev",
        }));
        return app;
    }
}
