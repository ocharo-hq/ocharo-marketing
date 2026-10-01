using Microsoft.Extensions.DependencyInjection;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Site;
using WoW.Two.Sdk.Backend.Beta.Mediator;

namespace OcharoBlog.Application;

/// <summary>Registers application-layer services — story, topic, settings and site services plus the kit mediator.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(typeof(DependencyInjection).Assembly);
        services.AddScoped<ArticleService>();
        services.AddScoped<TopicService>();
        services.AddScoped<SiteSettingsService>();
        services.AddScoped<SiteService>();
        services.AddScoped<PublicationSiteService>();
        return services;
    }
}
