using WoW.Two.Sdk.Backend.Beta.Media.Images;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Settings;
using OcharoBlog.Infrastructure.Blog;
using OcharoBlog.Infrastructure.Time;
using WoW.Two.Sdk.Backend.Beta.Storage.Core;
using WoW.Two.Sdk.Backend.Beta.Storage.FileSystem;

namespace OcharoBlog.Infrastructure;

/// <summary>Registers infrastructure: clock, blob-backed media, the catalogue feed and brand kit clients, seeding and the scheduler.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRoot, bool runScheduler = true)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddOptions<BlogOptions>().Bind(configuration.GetSection(BlogOptions.Section));
        services.AddSingleton(provider => new PublicationSiteOptions
        {
            CatalogueAdminUrl = provider.GetRequiredService<IOptions<BlogOptions>>().Value.CatalogueAdminUrl,
        });
        // Resolved lazily so configuration overrides applied after registration (tests, hosts) still win.
        services.AddSingleton<IBlobRepository>(provider =>
        {
            var dataRoot = provider.GetRequiredService<IOptions<BlogOptions>>().Value.DataRoot;
            var root = Path.IsPathRooted(dataRoot) ? dataRoot : Path.Combine(contentRoot, dataRoot);
            return new LocalFileBlobRepository(Path.Combine(root, "blobs"));
        });
        services.AddImageProcessing(o => { o.MaxInputBytes = MediaEditingService.MaximumBytes; o.MaxPixels = MediaEditingService.MaximumPixels; o.MaxOutputDimension = MediaEditingService.MaximumDimension; });
        services.AddHttpContextAccessor();
        services.AddHttpClient("ocharo-image-processing", c => c.Timeout = TimeSpan.FromSeconds(95))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddScoped<IEditorialImageWorker, EditorialImageWorker>();
        services.AddScoped<IMediaEditingService, MediaEditingService>();
        services.AddScoped<IMediaStore, MediaStore>();
        services.AddHttpClient<ICatalogueFeed, CatalogueFeed>(client => client.Timeout = TimeSpan.FromSeconds(8));
        services.AddHttpClient<IBrandKit, BrandKit>(client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddScoped<BlogSeeder>();
        services.AddScoped<EditorialStarterImporter>();
        if (runScheduler) services.AddHostedService<ScheduledPublishingWorker>();
        return services;
    }
}
