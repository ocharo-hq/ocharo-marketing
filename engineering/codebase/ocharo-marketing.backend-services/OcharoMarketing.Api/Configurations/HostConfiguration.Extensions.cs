using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Api.Marketing;
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
using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Ef;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Storage.FileSystem;

namespace OcharoMarketing.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Configures the local pilot startup boundary.</summary>
    private static WebApplicationBuilder AddPilotBoundary(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment() || !builder.Configuration.GetValue("Marketing:LocalPilot", true))
        {
            throw new InvalidOperationException(
                "This pilot requires Development and local-only access. " +
                "Remote hosting requires authenticated deployment configuration.");
        }

        return builder;
    }

    /// <summary>Registers the platform access-ticket client.</summary>
    private static WebApplicationBuilder AddIntegrationBoundary(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("ocharo-platform-tickets", client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        return builder;
    }

    /// <summary>Registers the host-wide marketing options and system time provider.</summary>
    private static WebApplicationBuilder AddSettings(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<MarketingOptions>(builder.Configuration.GetSection("Marketing"));
        builder.Services.AddSingleton(TimeProvider.System);
        return builder;
    }

    /// <summary>Registers the SQLite database and its migration runner.</summary>
    private static WebApplicationBuilder AddSqliteDatabase(this WebApplicationBuilder builder)
    {
        var dataRoot = Path.GetFullPath(builder.Configuration["Marketing:DataDirectory"] ?? "App_Data");
        Directory.CreateDirectory(dataRoot);
        var connectionString = builder.Configuration["ConnectionStrings:OcharoMarketing"]
            ?? $"Data Source={Path.Combine(dataRoot, "marketing.db")};Default Timeout=30";
        builder.Services.AddDbContext<OcharoMarketingDbContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddEfMigrationsRunner<OcharoMarketingDbContext>(options => options.MaxConnectAttempts = 1);
        return builder;
    }

    /// <summary>Registers the shared local blob storage.</summary>
    private static WebApplicationBuilder AddBlobStorage(this WebApplicationBuilder builder)
    {
        var dataRoot = Path.GetFullPath(builder.Configuration["Marketing:DataDirectory"] ?? "App_Data");
        builder.Services.AddLocalBlobStorage(Path.Combine(dataRoot, "blobs"));
        return builder;
    }

    /// <summary>Registers card-rendering settings, the browser adapter and rendering services.</summary>
    private static WebApplicationBuilder AddCards(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(new CardRendererSettings(builder.Configuration["Cards:ExecutablePath"]));
        builder.Services.AddSingleton<IDocumentRenderingBroker, PlaywrightDocumentRenderingBroker>();
        builder.Services.AddSingleton<ICardRenderingService, HtmlCardRenderingService>();
        builder.Services.AddScoped<IAssetService, LocalAssetService>();
        return builder;
    }

    /// <summary>Registers marketing records, orchestration, seed import and generation work.</summary>
    private static WebApplicationBuilder AddMarketing(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMarketingRepository, MarketingRepository>();
        builder.Services.AddScoped<MarketingService>();
        builder.Services.AddHostedService<SeedImportHostedService>();
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

    /// <summary>Registers automation orchestration, catalogue and story clients, and scheduled work.</summary>
    private static WebApplicationBuilder AddAutomation(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<AutomationService>();
        builder.Services.AddScoped<AutomationRunner>();
        builder.Services.AddHttpClient<CatalogueSyncService>(client => client.Timeout = TimeSpan.FromSeconds(60));
        builder.Services.AddHttpClient<BlogStoryService>(client => client.Timeout = TimeSpan.FromSeconds(15));
        builder.Services.AddHostedService<AutomationWorker>();
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
        builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:OpenAi")
            .Get<OpenAiProviderOptions>() ?? new());
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
        builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:Gemini")
            .Get<GeminiProviderOptions>() ?? new());
        builder.AddProvider<GeminiImageGenerationBroker>();
        builder.Services.AddTransient<IImageGenerationBroker>(
            provider => provider.GetRequiredService<GeminiImageGenerationBroker>());
        return builder;
    }

    /// <summary>Registers Telegram settings, account verification, publishing and receipt reconciliation.</summary>
    private static WebApplicationBuilder AddTelegram(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:Telegram")
            .Get<TelegramProviderOptions>() ?? new());
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
        builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:X").Get<XProviderOptions>() ?? new());
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
        builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:Instagram")
            .Get<InstagramProviderOptions>() ?? new());
        builder.AddProvider<InstagramSocialPublishingBroker>();
        builder.Services.AddTransient<ISocialPublishingBroker>(
            provider => provider.GetRequiredService<InstagramSocialPublishingBroker>());
        return builder;
    }

    /// <summary>Registers WhatsApp settings and publishing.</summary>
    private static WebApplicationBuilder AddWhatsApp(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(builder.Configuration.GetSection("Providers:WhatsApp")
            .Get<WhatsAppProviderOptions>() ?? new());
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

    /// <summary>Registers HTTP problem details, controllers and OpenAPI.</summary>
    private static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        return builder;
    }

    /// <summary>Configures the existing request boundaries, static shell and public inquiry budget.</summary>
    private static WebApplication UsePipeline(this WebApplication app)
    {
        app.UseMiddleware<MarketingExceptionMiddleware>();
        app.UseMiddleware<LocalPilotBoundaryMiddleware>();
        app.UseMiddleware<IntegrationBoundaryMiddleware>();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRateLimiter();
        return app;
    }

    /// <summary>Configures controller, status, API error and SPA endpoints.</summary>
    private static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoMarketing",
            mode = "local-pilot",
            version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev",
        }));
        app.MapGet("/api/system/status", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoMarketing",
            version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev",
        }));
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: 404, title: "API route not found"));
        app.MapFallbackToFile("index.html");
        return app;
    }
}
