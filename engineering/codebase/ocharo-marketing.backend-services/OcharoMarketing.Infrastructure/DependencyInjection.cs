using Microsoft.Extensions.Configuration;
using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Infrastructure.Scanning;
using Microsoft.Extensions.DependencyInjection;
using OcharoMarketing.Application.Cards.Brokers;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Application.Providers;
using OcharoMarketing.Infrastructure.Cards.Brokers;
using OcharoMarketing.Infrastructure.Cards.Services;
using OcharoMarketing.Infrastructure.Cards.Settings;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.BackgroundServices;
using OcharoMarketing.Infrastructure.Marketing.Services;
using OcharoMarketing.Infrastructure.Providers;
using OcharoMarketing.Infrastructure.Providers.Brokers;
using WoW.Two.Sdk.Backend.Beta.Storage.FileSystem;

namespace OcharoMarketing.Infrastructure;

/// <summary>Registers product orchestration and explicitly configured provider adapters.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketingOptions>(configuration.GetSection("Marketing"));
        var dataRoot = Path.GetFullPath(configuration["Marketing:DataDirectory"] ?? "App_Data");
        services.AddLocalBlobStorage(Path.Combine(dataRoot, "blobs"));
        services.AddSingleton(new CardRendererSettings(configuration["Cards:ExecutablePath"]));
        services.AddSingleton<IDocumentRenderingBroker, PlaywrightDocumentRenderingBroker>();
        services.AddSingleton<ICardRenderingService, HtmlCardRenderingService>();
        services.AddScoped<IAssetService, LocalAssetService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<MarketingService>();
        services.AddScoped<Automation.AutomationService>();
        services.AddScoped<Automation.AutomationRunner>();
        services.AddHttpClient<Automation.CatalogueSyncService>(client => client.Timeout = TimeSpan.FromSeconds(60));
        services.AddHttpClient<Automation.BlogStoryService>(client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<DeliveryPolicy>();
        services.AddScoped<PublicationService>();
        services.AddScoped<MetricObservationService>();
        services.AddScoped<ReceiptReconciliationService>();
        services.AddScoped<ISourceAnalysisService, SourceAnalysisService>();
        services.AddScoped<IExternalMarketingService, ExternalMarketingService>();
        services.AddSingleton(configuration.GetSection("Providers:OpenAi").Get<OpenAiProviderOptions>() ?? new());
        services.AddSingleton(configuration.GetSection("Providers:Gemini").Get<GeminiProviderOptions>() ?? new());
        services.AddSingleton(configuration.GetSection("Providers:Telegram").Get<TelegramProviderOptions>() ?? new());
        services.AddSingleton(configuration.GetSection("Providers:X").Get<XProviderOptions>() ?? new());
        services.AddSingleton(configuration.GetSection("Providers:Instagram").Get<InstagramProviderOptions>() ?? new());
        services.AddSingleton(configuration.GetSection("Providers:WhatsApp").Get<WhatsAppProviderOptions>() ?? new());
        AddProvider<TelegramAccountVerificationBroker>(services);
        AddProvider<XAccountVerificationBroker>(services);
        AddProvider<XSocialMetricsBroker>(services);
        AddProvider<XReceiptReconciliationBroker>(services);
        services.AddSingleton<IReceiptReconciliationBroker, TelegramReceiptReconciliationBroker>();
        services.AddTransient<IReceiptReconciliationBroker>(provider => provider.GetRequiredService<XReceiptReconciliationBroker>());
        services.AddTransient<IProviderVerificationBroker>(provider => provider.GetRequiredService<TelegramAccountVerificationBroker>());
        services.AddTransient<IProviderVerificationBroker>(provider => provider.GetRequiredService<XAccountVerificationBroker>());
        services.AddTransient<ISocialMetricsBroker>(provider => provider.GetRequiredService<XSocialMetricsBroker>());
        AddProvider<OpenAiImageGenerationBroker>(services);
        AddProvider<GeminiImageGenerationBroker>(services);
        AddProvider<OpenAiContentAssistanceBroker>(services);
        AddProvider<TelegramSocialPublishingBroker>(services);
        AddProvider<XSocialPublishingBroker>(services);
        AddProvider<InstagramSocialPublishingBroker>(services);
        AddProvider<WhatsAppSocialPublishingBroker>(services);
        services.AddTransient<IImageGenerationBroker>(provider => provider.GetRequiredService<OpenAiImageGenerationBroker>());
        services.AddTransient<IImageGenerationBroker>(provider => provider.GetRequiredService<GeminiImageGenerationBroker>());
        services.AddTransient<IContentAssistanceBroker>(provider => provider.GetRequiredService<OpenAiContentAssistanceBroker>());
        services.AddTransient<ISocialPublishingBroker>(provider => provider.GetRequiredService<TelegramSocialPublishingBroker>());
        services.AddTransient<ISocialPublishingBroker>(provider => provider.GetRequiredService<XSocialPublishingBroker>());
        services.AddTransient<ISocialPublishingBroker>(provider => provider.GetRequiredService<InstagramSocialPublishingBroker>());
        services.AddTransient<ISocialPublishingBroker>(provider => provider.GetRequiredService<WhatsAppSocialPublishingBroker>());
        return services;
    }

    private static void AddProvider<T>(IServiceCollection services) where T : class =>
        services.AddHttpClient<T>(client => client.Timeout = TimeSpan.FromMinutes(5))
            .RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
}
